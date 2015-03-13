// ****************************************************************
// Copyright (c) 2012 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    class FakeRunContext : IRunContext
    {
        #region IRunContext Members

        bool IRunContext.InIsolation
        {
            get { throw new NotImplementedException(); }
        }

        bool IRunContext.IsBeingDebugged
        {
            get { throw new NotImplementedException(); }
        }

        bool IRunContext.IsDataCollectionEnabled
        {
            get { throw new NotImplementedException(); }
        }

        bool IRunContext.KeepAlive
        {
            get
            {
                return true;
            }
        }

        string IRunContext.TestRunDirectory
        {
            get { throw new NotImplementedException(); }
        }

        ITestCaseFilterExpression IRunContext.GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            return null;  // as if we don't have a TFS Build, equal to testing from VS 
        }

        #endregion

        #region IDiscoveryContextMembers

        IRunSettings IDiscoveryContext.RunSettings
        {
            get { throw new NotImplementedException(); }
        }

        #endregion


        public string SolutionDirectory
        {
            get { throw new NotImplementedException(); }
        }
    }
}
