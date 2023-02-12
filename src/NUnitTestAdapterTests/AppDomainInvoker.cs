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

#if NET462
using System;
using System.IO;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    internal sealed class AppDomainInvoker : IDisposable
    {
        private AppDomain appDomain;
        private RemoteHelper helper;

        private RemoteHelper GetHelper()
        {
            if (helper == null)
            {
                var assembly = typeof(AppDomainInvoker).GetTypeInfo().Assembly;

                appDomain = AppDomain.CreateDomain(
                    typeof(AppDomainInvoker).FullName,
                    null,
                    new AppDomainSetup
                    {
                        ApplicationBase = Path.GetDirectoryName(assembly.Location),
                        ConfigurationFile = assembly.Location + ".config"
                    });

                helper = (RemoteHelper)appDomain.CreateInstanceAndUnwrap(assembly.FullName, typeof(RemoteHelper).FullName);
            }
            return helper;
        }

        public void Dispose()
        {
            if (appDomain != null) AppDomain.Unload(appDomain);
        }

        public void Invoke<T>(Action<T> action, T obj) => GetHelper().Invoke(action, obj);

        private sealed class RemoteHelper : MarshalByRefObject
        {
            public void Invoke<T>(Action<T> action, T obj) => action.Invoke(obj);
        }
    }
}
#endif
