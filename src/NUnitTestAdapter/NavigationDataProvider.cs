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
using Mono.Cecil.Rocks;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NavigationDataProvider
    {
        readonly string _assemblyPath;
        ICollection<string> _knownReferencedAssemblies;
        IDictionary<string, TypeDefinition> _typeDefs;

        public NavigationDataProvider(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public NavigationData GetNavigationData(string className, string methodName)
        {
            if (_typeDefs == null)
            {
                _typeDefs = CacheTypes(_assemblyPath);
                _knownReferencedAssemblies = new HashSet<string> { _assemblyPath };
            }

            // Through NUnit 3.2.1, the class name provided by NUnit is
            // the reflected class - that is, the class of the fixture
            // that is being run. To use for location data, we actually
            // need the class where the method is defined (for example,
            // a base class being used as an abstract test fixture).
            // TODO: Once NUnit is changed, try to simplify this.
            var sequencePoint = GetTypeLineage(className)
                .Select(typeDef => typeDef.GetMethods().FirstOrDefault(o => o.Name == methodName))
                .Where(x => x != null)
                .Select(FirstOrDefaultSequencePoint)
                .FirstOrDefault();

            if (sequencePoint == null) { return NavigationData.Invalid; }
            return new NavigationData(sequencePoint.Document.Url, sequencePoint.StartLine);
        }

        static bool DoesPdbFileExist(string filepath) => File.Exists(Path.ChangeExtension(filepath, ".pdb"));

        static IDictionary<string, TypeDefinition> CacheTypes(string assemblyPath)
        {
            var types = new Dictionary<string, TypeDefinition>();
            CacheNewTypes(assemblyPath, types);
            return types;
        }

        private static void CacheNewTypes(string assemblyPath, IDictionary<string, TypeDefinition> types)
        {
            var resolver = new DefaultAssemblyResolver();
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = DoesPdbFileExist(assemblyPath),
                AssemblyResolver = resolver
            };

            var directory = Path.GetDirectoryName(assemblyPath);
            resolver.AddSearchDirectory(directory);
            var knownSearchDirectories = new HashSet<string> { directory };

            var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);

            foreach (var type in module.GetTypes())
            {
                directory = Path.GetDirectoryName(type.Module.FullyQualifiedName);
                if (!knownSearchDirectories.Contains(directory))
                {
                    resolver.AddSearchDirectory(directory);
                    knownSearchDirectories.Add(directory);
                }

                types[type.FullName] = type;
            }
        }

        IEnumerable<TypeDefinition> GetTypeLineage(string className)
        {
            var typeDef = GetTypeDefinitionOrDefault(className);
            while (typeDef != default(TypeDefinition) && typeDef.FullName != "System.Object")
            {
                if (!_typeDefs.ContainsKey(typeDef.FullName))
                {
                    _typeDefs[typeDef.FullName] = typeDef;
                }

                yield return typeDef;

                // .Resolve() will resolve the type within all configured
                // search-directories, but it will _not_ try to read any
                // symbols, which we need for navigation data.
                //
                // So, first resolve the type to get the full-path, and
                // then re-read and cache the module with our reader-settings.
                //
                // This feels like it should be handled by Mono.Cecil.  A
                // follow-up may be in order.
                try
                {
                    var newTypeDef = typeDef.BaseType.Resolve();
                    var newAssemblyPath = newTypeDef.Module.FullyQualifiedName;
                    if (!_knownReferencedAssemblies.Contains(newAssemblyPath))
                    {
                        CacheNewTypes(newAssemblyPath, _typeDefs);
                        _knownReferencedAssemblies.Add(newAssemblyPath);
                        if (_typeDefs.ContainsKey(newTypeDef.FullName))
                        {
                            newTypeDef = _typeDefs[newTypeDef.FullName];
                        }
                    }

                    typeDef = newTypeDef;
                }
                catch (AssemblyResolutionException)
                {
                    // when resolving the assembly for the base-type fails
                    // (assembly not found in the known search-directories)
                    // stop the type-lineage here ("best-effort").
                    typeDef = null;
                }
            }
        }

        TypeDefinition GetTypeDefinitionOrDefault(string className)
        {
            TypeDefinition typeDef;
            return _typeDefs.TryGetValue(StandardizeTypeName(className), out typeDef) ? typeDef : default(TypeDefinition);
        }

        static SequencePoint FirstOrDefaultSequencePoint(MethodDefinition testMethod)
        {
            CustomAttribute asyncStateMachineAttribute;

            if (TryGetAsyncStateMachineAttribute(testMethod, out asyncStateMachineAttribute))
                testMethod = GetStateMachineMoveNextMethod(asyncStateMachineAttribute);

            return FirstOrDefaultUnhiddenSequencePoint(testMethod.Body);
        }

        static bool TryGetAsyncStateMachineAttribute(MethodDefinition method, out CustomAttribute attribute)
        {
            attribute = method.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == "AsyncStateMachineAttribute");
            return attribute != null;
        }

        static MethodDefinition GetStateMachineMoveNextMethod(CustomAttribute asyncStateMachineAttribute)
        {
            var stateMachineType = (TypeDefinition)asyncStateMachineAttribute.ConstructorArguments[0].Value;
            var stateMachineMoveNextMethod = stateMachineType.GetMethods().First(m => m.Name == "MoveNext");
            return stateMachineMoveNextMethod;
        }

        static SequencePoint FirstOrDefaultUnhiddenSequencePoint(MethodBody body)
        {
            const int lineNumberIndicatingHiddenLine = 16707566; //0xfeefee

            foreach (var instruction in body.Instructions)
                if (instruction.SequencePoint != null && instruction.SequencePoint.StartLine != lineNumberIndicatingHiddenLine)
                    return instruction.SequencePoint;

            return null;
        }

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
