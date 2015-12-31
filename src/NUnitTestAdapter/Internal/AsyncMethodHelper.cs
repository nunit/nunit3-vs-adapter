// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Linq;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    public class AsyncMethodHelper : MarshalByRefObject
    {
        Assembly targetAssembly;

        public void LoadAssembly(string assemblyName)
        {
            targetAssembly = Assembly.LoadFrom(assemblyName);
        }

        public string GetClassNameForAsyncMethod(string className, string methodName)
        {
            if (targetAssembly == null) return null;

            var definingType = targetAssembly.GetType(className);
            if (definingType == null) return null;

            var method = definingType.GetMethods().Where(o => o.Name == methodName).OrderBy(o => o.GetParameters().Length).FirstOrDefault();
            if (method == null) return null;

            var asyncAttribute = GetAsyncStateMachineAttribute(method);
            if (asyncAttribute == null) return null;

            PropertyInfo stateMachineTypeProperty = asyncAttribute.GetType().GetProperty("StateMachineType");
            if (stateMachineTypeProperty == null) return null;

            var asyncStateMachineType = stateMachineTypeProperty.GetValue(asyncAttribute, new object[0]) as Type;
            return asyncStateMachineType == null ? null : asyncStateMachineType.FullName;
        }

        private Attribute GetAsyncStateMachineAttribute(MethodInfo method)
        {
            return method.GetCustomAttributes(false).Cast<Attribute>().FirstOrDefault(attribute => attribute.GetType().FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
