// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System.IO;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitTestAdapterTests
    {
        private static readonly string adapterPath = Path.GetFullPath("NUnit.VisualStudio.TestAdapter.dll");
        private static readonly string adapterTestsPath = Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string mockAssemblyPath = Path.GetFullPath("mock-assembly.dll");

        [Test]
        public void CanFilterAssembliesWithNUnitTests()
        {
            var filePaths = new[] {
                Path.GetFullPath(adapterPath),
                Path.GetFullPath(adapterTestsPath),
                Path.GetFullPath(mockAssemblyPath)
            };

            Assert.That(NUnitTestAdapter.SanitizeSources(filePaths), Is.EqualTo(new object[] {
                Path.GetFullPath(adapterTestsPath),
                Path.GetFullPath(mockAssemblyPath)
            }));
        }

        [Test]
        public void CanRecognizeAssemblyReferencesNUnitFramework()
        {
            Assert.True(NUnitTestAdapter.CanHaveNUnitFrameworkReference(mockAssemblyPath));
        }

        [Test]
        public void CanRecognizeAssemblyDoesNotReferenceNUnitFramework()
        {
            Assert.False(NUnitTestAdapter.CanHaveNUnitFrameworkReference(adapterPath));
        }
    }
}
