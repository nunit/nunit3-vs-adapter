using System;
using System.Threading;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter;
using NUnit.VisualStudio.TestAdapter.Internal;

namespace NUnit.VisualStudio.TestAdapter.Tests;

public class TimingLoggerTests
{
    [Test]
    public void ReStart_ResetsElapsedTime()
    {
        var settings = Substitute.For<IAdapterSettings>();
        settings.Verbosity.Returns(5);
        var logger = Substitute.For<ITestLogger>();

        var sut = new TimingLogger(settings, logger);

        Thread.Sleep(10);
        var before = sut.Stopwatch.Elapsed;
        sut.ReStart();
        Thread.Sleep(1);
        var after = sut.Stopwatch.Elapsed;

        Assert.That(before, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(after, Is.LessThan(before));
    }
}

