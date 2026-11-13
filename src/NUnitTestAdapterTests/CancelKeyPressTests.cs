// ***********************************************************************
// Copyright (c) 2026 Terje Sandstrom
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

using System;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests;

/// <summary>
/// Issue #1549: on Android (and iOS, tvOS, browser) subscribing to Console.CancelKeyPress
/// throws PlatformNotSupportedException, which failed every MTP run before a test executed.
/// </summary>
public class CancelKeyPressTests
{
    [Test]
    public void ThatAPlatformWithoutCancelKeyPressIsSkipped()
    {
        bool subscribed = true;
        Assert.That(
            () => subscribed = NUnit3TestExecutor.TrySubscribeToCancelKeyPress(() => throw new PlatformNotSupportedException()),
            Throws.Nothing);
        Assert.That(subscribed, Is.False);
    }

    [Test]
    public void ThatASupportedPlatformSubscribes()
    {
        bool called = false;
        Assert.That(NUnit3TestExecutor.TrySubscribeToCancelKeyPress(() => called = true), Is.True);
        Assert.That(called);
    }

    [Test]
    public void ThatOtherExceptionsAreNotSwallowed()
    {
        Assert.That(
            () => NUnit3TestExecutor.TrySubscribeToCancelKeyPress(() => throw new InvalidOperationException()),
            Throws.InvalidOperationException);
    }

    [Test]
    public void ThatAnMtpExecutorCanBeCreatedAndDisposed()
    {
        Assert.That(CreateAndDisposeMtpExecutor, Throws.Nothing);
    }

    private static void CreateAndDisposeMtpExecutor()
    {
        using var sut = new NUnit3TestExecutor(isMTP: true);
    }
}
