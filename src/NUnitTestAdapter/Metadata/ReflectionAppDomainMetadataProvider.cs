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

#if !NETCOREAPP1_0
using System;
using System.IO;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Metadata
{
    internal sealed partial class ReflectionAppDomainMetadataProvider : IMetadataProvider
    {
        private AppDomain _appDomain;
        private AppDomainHelper _helper;

        private AppDomainHelper GetHelper()
        {
            if (_helper == null)
            {
                var adapterAssembly = typeof(ReflectionAppDomainMetadataProvider).Assembly;
                var adapterAssemblyPath = adapterAssembly.ManifestModule.FullyQualifiedName;

                _appDomain = AppDomain.CreateDomain(
                    friendlyName: typeof(ReflectionAppDomainMetadataProvider).FullName,
                    securityInfo: null,
                    info: new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(adapterAssemblyPath) });

                _appDomain.ReflectionOnlyAssemblyResolve += AppDomainReflectionOnlyAssemblyResolve;

                _helper = (AppDomainHelper)_appDomain.CreateInstanceAndUnwrap(adapterAssembly.FullName, typeof(AppDomainHelper).FullName);
            }

            return _helper;
        }

        private static Assembly AppDomainReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        public void Dispose()
        {
            if (_appDomain != null)
                AppDomain.Unload(_appDomain);
        }

        public TypeInfo? GetDeclaringType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            return GetHelper().GetDeclaringType(assemblyPath, reflectedTypeName, methodName);
        }

        public TypeInfo? GetStateMachineType(string assemblyPath, string reflectedTypeName, string methodName)
        {
            return GetHelper().GetStateMachineType(assemblyPath, reflectedTypeName, methodName);
        }

        private sealed class AppDomainHelper : MarshalByRefObject
        {
            private readonly DirectReflectionMetadataProvider provider = new DirectReflectionMetadataProvider();

            public TypeInfo? GetDeclaringType(string assemblyPath, string reflectedTypeName, string methodName)
            {
                return provider.GetDeclaringType(assemblyPath, reflectedTypeName, methodName);
            }

            public TypeInfo? GetStateMachineType(string assemblyPath, string reflectedTypeName, string methodName)
            {
                return provider.GetStateMachineType(assemblyPath, reflectedTypeName, methodName);
            }
        }
    }
}
#endif
