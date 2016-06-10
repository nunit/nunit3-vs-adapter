// ****************************************************************
// Copyright (c) 2016 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NavigationData
    {
        public static readonly NavigationData Invalid = new NavigationData(null, 0);

        public NavigationData(string filePath, int lineNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        public string FilePath { get; private set; }

        public int LineNumber { get; private set; }

        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(FilePath); }
        }
    }
}
