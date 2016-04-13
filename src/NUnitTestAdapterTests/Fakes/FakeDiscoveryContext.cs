// ****************************************************************
// Copyright (c) 2012 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    class FakeDiscoveryContext : IDiscoveryContext
    {
        #region IDiscoveryContextMembers

        IRunSettings IDiscoveryContext.RunSettings
        {
            get { return new FakeRunSettings(); }
        }

        #endregion
    }
}
