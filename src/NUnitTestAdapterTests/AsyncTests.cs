﻿// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole, Terje Sandstrom
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
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests;

public class AsyncTests
{
    [Test]
    public async Task TaskTestSuccess()
    {
            var result = await ReturnOne();

            Assert.That(result, Is.EqualTo(1));
        }

    [TestCase(ExpectedResult = 1)]
    public async Task<int> TaskTTestCaseWithResultCheckSuccess()
    {
            return await ReturnOne();
        }

    private static Task<int> ReturnOne()
    {
            return Task.Run(() => 1);
        }

    private static Task ThrowException()
    {
            return Task.Run(() =>
            {
                throw new InvalidOperationException();
            });
        }
}