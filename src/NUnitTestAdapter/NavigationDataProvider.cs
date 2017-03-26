// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NavigationDataProvider
    {
        readonly string _assemblyPath;
        IDictionary<string, TypeDefinition> _typeDefs;

        public NavigationDataProvider(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public NavigationData GetNavigationData(string className, string methodName)
        {
            if (_typeDefs == null)
                _typeDefs = CacheTypes(_assemblyPath);

#if true
            TypeDefinition typeDef;
            if (!TryGetTypeDefinition(className, out typeDef))
                return NavigationData.Invalid;

            MethodDefinition methodDef = null;

            // Through NUnit 3.2.1, the class name provided by NUnit is
            // the reflected class - that is, the class of the fixture
            // that is being run. To use for location data, we actually
            // need the class where the method is defined.
            // TODO: Once NUnit is changed, try to simplify this.
            while (true)
            {
                methodDef = typeDef.Methods.FirstOrDefault(o => o.Name == methodName);

                if (methodDef != null)
                    break;

                var baseType = typeDef.BaseType;
                if (baseType == null || baseType.FullName == "System.Object")
                    return NavigationData.Invalid;

                typeDef = typeDef.BaseType.Resolve();
            }

            var sequencePoint = FirstOrDefaultSequencePoint(methodDef);
            if (sequencePoint != null)
                return new NavigationData(sequencePoint.Document.Url, sequencePoint.StartLine);

            return NavigationData.Invalid;
#else
            var navigationData = GetMethods(className)
                .Where(m => m.Name == methodName)
                .Select(FirstOrDefaultSequencePoint)
                .Where(x => x != null)
                .OrderBy(x => x.StartLine)
                .Select(x => new NavigationData(x.Document.Url, x.StartLine))
                .FirstOrDefault();

            return navigationData ?? NavigationData.Invalid;
#endif
        }

        static bool DoesPdbFileExist(string filepath) => File.Exists(Path.ChangeExtension(filepath, ".pdb"));


        static IDictionary<string, TypeDefinition> CacheTypes(string assemblyPath)
        {
            var readsymbols = DoesPdbFileExist(assemblyPath);
            var readerParameters = new ReaderParameters { ReadSymbols = readsymbols};
            var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);

            var types = new Dictionary<string, TypeDefinition>();

            foreach (var type in module.GetTypes())
                types[type.FullName] = type;

            return types;
        }

        IEnumerable<MethodDefinition> GetMethods(string className)
        {
            TypeDefinition type;

            if (TryGetTypeDefinition(className, out type))
                return type.Methods;

            return Enumerable.Empty<MethodDefinition>();
        }

        bool TryGetTypeDefinition(string className, out TypeDefinition typeDef)
        {
            return _typeDefs.TryGetValue(StandardizeTypeName(className), out typeDef);
        }

        static SequencePoint FirstOrDefaultSequencePoint(MethodDefinition testMethod)
        {
            CustomAttribute asyncStateMachineAttribute;

            if (TryGetAsyncStateMachineAttribute(testMethod, out asyncStateMachineAttribute))
                testMethod = GetStateMachineMoveNextMethod(asyncStateMachineAttribute);

#if NETCOREAPP1_0
            return FirstOrDefaultUnhiddenSequencePoint(testMethod.DebugInformation);
#else
            return FirstOrDefaultUnhiddenSequencePoint(testMethod.Body);
#endif
        }

        static bool TryGetAsyncStateMachineAttribute(MethodDefinition method, out CustomAttribute attribute)
        {
            attribute = method.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == "AsyncStateMachineAttribute");
            return attribute != null;
        }

        static MethodDefinition GetStateMachineMoveNextMethod(CustomAttribute asyncStateMachineAttribute)
        {
            var stateMachineType = (TypeDefinition)asyncStateMachineAttribute.ConstructorArguments[0].Value;
            var stateMachineMoveNextMethod = stateMachineType.Methods.First(m => m.Name == "MoveNext");
            return stateMachineMoveNextMethod;
        }

        const int lineNumberIndicatingHiddenLine = 16707566; //0xfeefee

#if NETCOREAPP1_0
        static SequencePoint FirstOrDefaultUnhiddenSequencePoint(MethodDebugInformation body) =>
            body.SequencePoints.FirstOrDefault(sp => sp != null && sp.StartLine != lineNumberIndicatingHiddenLine);
#else
        static SequencePoint FirstOrDefaultUnhiddenSequencePoint(MethodBody body) =>
            body.Instructions
                .Where(i => i.SequencePoint != null && i.SequencePoint.StartLine != lineNumberIndicatingHiddenLine)
                .Select(i => i.SequencePoint)
                .FirstOrDefault();
#endif

        static string StandardizeTypeName(string className)
        {
            //Mono.Cecil respects ECMA-335 for the FullName of a type, which can differ from Type.FullName.
            //In order to make reliable comparisons between the class part of a MethodGroup, the class part
            //must be standardized to the ECMA-335 format.
            //
            //ECMA-335 specifies "/" instead of "+" to indicate a nested type.

            return className.Replace("+", "/");
        }
    }
}
