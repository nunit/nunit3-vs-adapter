// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
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
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.VisualStudio.TestAdapter.Internal;

#if !NET462 && !NETSTANDARD
using System.Runtime.Loader;
#endif

namespace NUnit.VisualStudio.TestAdapter.Metadata
{
    internal sealed class DirectReflectionMetadataProvider : IMetadataProvider
    {
        public TypeInfo? GetDeclaringType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            var type = TryGetSingleMethod(assemblyPath, reflectedTypeName, methodName)?.DeclaringType;
            if (type == null) return null;

            if (type.IsConstructedGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            return new TypeInfo(type);
        }

        public TypeInfo? GetStateMachineType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            var method = TryGetSingleMethod(assemblyPath, reflectedTypeName, methodName);
            if (method == null) return null;

            var candidate = (Type)null;

            foreach (var attributeData in CustomAttributeData.GetCustomAttributes(method))
            {
                for (var current = attributeData.Constructor.DeclaringType; current != null; current = current.GetTypeInfo().BaseType)
                {
                    if (current.FullName != "System.Runtime.CompilerServices.StateMachineAttribute") continue;

                    var parameters = attributeData.Constructor.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].Name != "stateMachineType") continue;
                        if (attributeData.ConstructorArguments[i].Value is Type argument)
                        {
                            if (candidate != null)
                                return null;
                            candidate = argument;
                        }
                    }
                }
            }

            if (candidate == null)
                return null;
            return new TypeInfo(candidate);
        }

        private static MethodInfo TryGetSingleMethod(string assemblyPath, string reflectedTypeName, string methodName)
        {
            try
            {
#if !NET462 && !NETSTANDARD
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#else
                var assembly = Assembly.LoadFrom(assemblyPath);
#endif

                var type = assembly.GetType(reflectedTypeName, throwOnError: false);

                var methods = type?.GetMethods().Where(m => m.Name == methodName).Take(2).ToList();
                return methods?.Count == 1 ? methods[0] : null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        void IDisposable.Dispose()
        {
        }
    }
}
