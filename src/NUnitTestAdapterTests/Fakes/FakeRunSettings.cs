// ****************************************************************
// Copyright (c) 2012 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    class FakeRunSettings : IRunSettings
    {
        ISettingsProvider IRunSettings.GetSettings(string settingsName)
        {
            throw new NotImplementedException();
        }

        public string SettingsXml
        {
            get { throw new NotImplementedException(); }
        }
    }
}
