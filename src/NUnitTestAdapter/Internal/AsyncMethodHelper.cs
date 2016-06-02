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
        public string GetClassNameForAsyncMethod(MethodInfo method)
        {
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
