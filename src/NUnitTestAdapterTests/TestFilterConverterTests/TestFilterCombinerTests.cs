using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests
{
    public class TestFilterCombinerTests
    {
        [Test]
        public void TestCombiningCategories()
        {
            var builder = new TestFilterBuilder();
            builder.SelectWhere("cat==FOO");

            var t1 = builder.GetFilter();
            builder = new TestFilterBuilder();
            builder.SelectWhere("cat!=BOO");
            var t2 = builder.GetFilter();
            var combiner = new TestFilterCombiner(t1, t2);
            var tRes = combiner.GetFilter();
            Assert.Multiple(() =>
            {
                Assert.That(t1.Text, Does.StartWith("<filter>"));
                Assert.That(t1.Text, Is.EqualTo("<filter><cat>FOO</cat></filter>"));
                Assert.That(t2.Text, Does.StartWith("<filter>"));
                Assert.That(t2.Text, Is.EqualTo("<filter><not><cat>BOO</cat></not></filter>"));
                Assert.That(tRes.Text, Does.StartWith("<filter>"));
                Assert.That(tRes.Text, Does.Not.StartWith("<filter><filter>"));
                Assert.That(tRes.Text, Is.EqualTo("<filter><cat>FOO</cat><not><cat>BOO</cat></not></filter>"));
            });
            TestContext.Out.WriteLine(" ");
            TestContext.Out.WriteLine(tRes.Text);
        }
    }
}
