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
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.VisualStudio.TestAdapter.Metadata;

namespace NUnit.VisualStudio.TestAdapter
{
    public sealed class NavigationDataProvider : IDisposable
    {
        private readonly string _assemblyPath;
        private readonly Dictionary<string, DiaSession> _sessionsByAssemblyPath = new Dictionary<string, DiaSession>(StringComparer.OrdinalIgnoreCase);
        private readonly IMetadataProvider _metadataProvider;

        public NavigationDataProvider(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("Assembly path must be specified.", nameof(assemblyPath));

            _assemblyPath = assemblyPath;

#if NETCOREAPP1_0
            _metadataProvider = new DirectReflectionMetadataProvider();
#else
            _metadataProvider = new ReflectionAppDomainMetadataProvider();
#endif
        }

        public void Dispose()
        {
            _metadataProvider.Dispose();

            foreach (var session in _sessionsByAssemblyPath.Values)
                session?.Dispose();
        }

        public NavigationData GetNavigationData(string className, string methodName)
        {
            return TryGetSessionData(_assemblyPath, className, methodName)
                ?? TryGetSessionData(DoSafe(_metadataProvider.GetStateMachineType, _assemblyPath, className, methodName), "MoveNext")
                ?? TryGetSessionData(DoSafe(_metadataProvider.GetDeclaringType, _assemblyPath, className, methodName), methodName)
                ?? NavigationData.Invalid;
        }

        private static TypeInfo? DoSafe(Func<string, string, string, TypeInfo?> method, string assemblyPath, string declaringTypeName, string methodName)
        {
            try
            {
                return method.Invoke(assemblyPath, declaringTypeName, methodName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private NavigationData TryGetSessionData(string assemblyPath, string declaringTypeName, string methodName)
        {
            if (!_sessionsByAssemblyPath.TryGetValue(assemblyPath, out var session))
            {
                try
                {
                    session = new DiaSession(assemblyPath);
                }
                // DiaSession crashes for .NET Framework tests run via `dotnet test` or `dotnet vstest`.
                // See https://github.com/Microsoft/vstest/issues/1432.
                catch // TestPlatformException not available in net35.
                {
                    session = null;
                }
                _sessionsByAssemblyPath.Add(assemblyPath, session);
            }

            var data = session?.GetNavigationData(declaringTypeName, methodName);

            return string.IsNullOrEmpty(data?.FileName) ? null :
                new NavigationData(data.FileName, data.MinLineNumber);
        }

        private NavigationData TryGetSessionData(TypeInfo? declaringType, string methodName)
        {
            return declaringType == null ? null :
                TryGetSessionData(declaringType.Value.AssemblyPath, declaringType.Value.FullName, methodName);
        }
    }
}
