using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter;

public class TestFilterCombiner(TestFilter a, TestFilter b)
{
    public TestFilter GetFilter()
    {
        var innerA = StripFilter(a);
        var innerB = StripFilter(b);
        var inner = $"<filter>{innerA}{innerB}</filter>";
        return new TestFilter(inner);
    }

    private string StripFilter(TestFilter x)
    {
        var s = x.Text.Replace("<filter>", "");
        var s2 = s.Replace("</filter>", "");
        return s2;
    }
}