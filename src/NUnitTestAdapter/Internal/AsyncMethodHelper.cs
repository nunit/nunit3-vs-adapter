// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter
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

            var method = definingType.GetMethod(methodName);
            if (method == null) return null;

            var asyncAttribute = GetAsyncStateMachineAttribute(method);
            if (asyncAttribute == null) return null;

            PropertyInfo stateMachineTypeProperty = asyncAttribute.GetType().GetProperty("StateMachineType");
            if (stateMachineTypeProperty == null) return null;

            Type asyncStateMachineType = stateMachineTypeProperty.GetValue(asyncAttribute, new object[0]) as Type;
            if (asyncStateMachineType == null) return null;
            
            return asyncStateMachineType.FullName;
        }

        private Attribute GetAsyncStateMachineAttribute(MethodInfo method)
        {
            foreach (Attribute attribute in method.GetCustomAttributes(false) )
                if (attribute.GetType().FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                    return attribute;
            return null;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
