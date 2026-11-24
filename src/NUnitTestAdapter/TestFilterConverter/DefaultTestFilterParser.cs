// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
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
using System.Collections.Generic;
using System.Linq;

using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Tries each parser in turn and returns the first filter one of them accepts. By default that is
/// the VSTest parser, then the legacy parser, so a filter that is not valid VSTest syntax gets the
/// filter it got before. When every parser rejects the filter, the last parser's error is thrown.
/// </summary>
public sealed class DefaultTestFilterParser : ITestFilterParser
{
    private readonly IReadOnlyList<ITestFilterParser> _parsers;
    private readonly ITestLogger _log;

    public DefaultTestFilterParser(ITestLogger log = null)
        : this([new VSTestFilterParser(), new LegacyTestFilterParser()], log)
    {
    }

    public DefaultTestFilterParser(IReadOnlyList<ITestFilterParser> parsers, ITestLogger log = null)
    {
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        if (_parsers.Count == 0)
        {
            throw new ArgumentException("At least one parser is required.", nameof(parsers));
        }

        _log = log;
    }

    /// <inheritdoc/>
    public ITestFilter Parse(string filter)
    {
        for (int i = 0; ; i++)
        {
            try
            {
                return _parsers[i].Parse(filter);
            }
            catch (TestFilterParserException ex) when (i < _parsers.Count - 1)
            {
                _log?.Info($"{_parsers[i].GetType().Name} did not accept the filter '{filter}' ({ex.Message}), trying {_parsers[i + 1].GetType().Name}.");
            }
        }
    }
}
