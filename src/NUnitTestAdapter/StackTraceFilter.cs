// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Terje Sandstrom
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

namespace NUnit.VisualStudio.TestAdapter
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Summary description for StackTraceFilter.
    /// </summary>
    public static class StackTraceFilter
    {
        public static string Filter(string stack)
        {
            if(stack == null) return null;
            var sw = new StringWriter();
            var sr = new StringReader(stack);

            try
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!FilterLine(line))
                        sw.WriteLine(line.Trim());
                }
            }
            catch (Exception)
            {
                return stack;
            }
            return sw.ToString();
        }

        static bool FilterLine(string line)
        {
            var patterns = new[]
            {
                "NUnit.Framework.Assertion",
                "NUnit.Framework.Assert",
                "System.Reflection.MonoMethod"
            };

            return patterns.Any(t => line.IndexOf(t,StringComparison.OrdinalIgnoreCase) > 0);
        }

    }
}
