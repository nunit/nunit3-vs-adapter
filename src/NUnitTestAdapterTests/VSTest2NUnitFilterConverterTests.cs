using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class VSTest2NUnitFilterConverterTests
    {
        [TestCase("TestCategory!=Whatever", "Category!=Whatever")]
        [TestCase("TestCategory=Whatever","Category==Whatever")]
        public void ThatWeCanConvertSimpleFilters(string vstestfilter, string expectedNUnitFilter)
        {
            var sut = new VSTest2NUnitFilterConverter(vstestfilter);
            var res = sut.ToString();
            Assert.That(res,Is.EqualTo(expectedNUnitFilter));
        }

        [TestCase("TestCategory=Whatever", "Category==Whatever")]
        public void ThatWeCanConvertComplexFilters(string vstestfilter, string expectedNUnitFilter)
        {
            var sut = new VSTest2NUnitFilterConverter(vstestfilter);
            var res = sut.ToString();
            Assert.That(res, Is.EqualTo(expectedNUnitFilter));
        }
    }
}
