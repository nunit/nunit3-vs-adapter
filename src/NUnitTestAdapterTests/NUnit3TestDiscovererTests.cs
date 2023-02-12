// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

using NSubstitute;

using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class NUnit3TestDiscovererTests
    {
        [Test]
        public void VerifyNUnit3TestDiscovererHasCategoryAttribute()
        {
            var attribute = typeof(NUnit3TestDiscoverer).GetTypeInfo().GetCustomAttribute(typeof(System.ComponentModel.CategoryAttribute));
            Assert.That(attribute, Is.Not.Null);
            Assert.That((attribute as System.ComponentModel.CategoryAttribute)?.Category, Is.EqualTo("managed"));
        }

        [Test]
        public void ThatDiscovererNUnitEngineAdapterIsInitialized()
        {
            var sut = new NUnit3TestDiscoverer();
            Assert.That(sut.NUnitEngineAdapter, Is.Not.Null);
            var dc = Substitute.For<IDiscoveryContext>();
            sut.DiscoverTests(new List<string>(), dc, null, null);
            Assert.That(sut.NUnitEngineAdapter, Is.Not.Null);
        }
    }
}
