// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// ------------------ IMPORTANT !! -----------------------
    /// This type needs to be serializable as it will be passed over to different Appdomain.
    /// So please make sure all the types you use inside this class serialiable, eg. you 
    /// can't use the type ObjectModel.ValidateArg which is not serializable.
    /// </summary>
    [Serializable]
    class TestRunFilter : TestFilter
    {
        private readonly List<string> map;

        public TestRunFilter(List<string> map)
        {
            this.map = map;
        }

        public override bool Match(ITest test)
        {
            if (test != null)
            {
                return test.TestType == "TestMethod" && map.Contains(test.TestName.FullName);
            }

            return false;
        }
    }
}
