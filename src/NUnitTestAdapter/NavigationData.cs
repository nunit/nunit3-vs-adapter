using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// The NavigationData class manages the location of navigation data
    /// for tests. It contains special code for handling async methods.
    /// </summary>
    public class NavigationData : IDisposable
    {
        private string _sourceAssembly;
        private DiaSession _diaSession;
        private bool _tryToCreateDiaSession = true;
        private Assembly _loadedAssembly;
        private bool _tryToLoadAssembly = true;

        public NavigationData(string sourceAssembly)
        {
            _sourceAssembly = sourceAssembly;
        }


        public DiaNavigationData For(string className, string methodName)
        {
            if (this.DiaSession == null) return null;

            var navData = DiaSession.GetNavigationData(className, methodName);

            if (navData != null && navData.FileName != null) return navData;

            // DiaSession returned null. The rest of this code checks to see 
            // if this test is an async method, which needs special handling.

            if (this.LoadedAssembly == null) return null;

            var definingType = LoadedAssembly.GetType(className);
            if (definingType == null) return null;

            var method = definingType.GetMethod(methodName);
            if (method == null) return null;

            var asyncAttribute = Reflect.GetAttribute(method, "System.Runtime.CompilerServices.AsyncStateMachineAttribute", false);
            if (asyncAttribute == null) return null;

            PropertyInfo stateMachineTypeProperty = asyncAttribute.GetType().GetProperty("StateMachineType");
            if (stateMachineTypeProperty == null) return null;

            Type stateMachineType = stateMachineTypeProperty.GetValue(asyncAttribute, new object[0]) as Type;
            if (stateMachineType == null) return null;

            navData = DiaSession.GetNavigationData(stateMachineType.FullName, "MoveNext");

            return navData;
        }

        // NOTE: There is some sort of timing issue involved
        // in creating the DiaSession. When it is created
        // in the constructor, an exception is thrown on the
        // call to GetNavigationData. We don't understand
        // this, we're just dealing with it.
        private DiaSession DiaSession
        {
            get
            {
                if (_tryToCreateDiaSession)
                {
                    try
                    {
                        _diaSession = new DiaSession(_sourceAssembly);
                    }
                    catch (Exception)
                    {
                        // If this isn't a project type supporting DiaSession,
                        // we just ignore the error. We won't try this again 
                        // for the project.
                    }

                    _tryToCreateDiaSession = false;
                }

                return _diaSession;
            }
        }

        // The assembly is only needed here if there async tests
        // are used. Therefore, we delay loading of the assembly
        // until it is actually needed.
        private Assembly LoadedAssembly
        {
            get
            {
                if (_tryToLoadAssembly)
                {
                    try
                    {
                        _loadedAssembly = Assembly.LoadFrom(_sourceAssembly);
                    }
                    catch
                    {
                        // If we can't load it for some reason, we just ignore
                        // the error and won't try to do it again.
                    }

                    _tryToLoadAssembly = false;
                }

                return _loadedAssembly;
            }
        }

        public void Dispose()
        {
            if (_diaSession != null)
                _diaSession.Dispose();
        }
    }
}
