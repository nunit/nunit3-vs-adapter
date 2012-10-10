using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

namespace NUnitTestDemo
{
    public class TextOutputTests
    {
        [Test]
        public void WriteToConsole()
        {
            Console.WriteLine("This is Console line 1");
            Console.WriteLine("This is Console line 2\nThis is Console line 3");
        }

        [Test]
        public void WriteToError()
        {
            Console.Error.Write("This is Error line 1");
            Console.Error.Write("This is Error line 2\nThis is Error line 3");
        }

        [Test]
        public void WriteToTrace()
        {
            Trace.Write("This is Trace line 1");
            Trace.Write("This is Trace line 2");
            Trace.Write("This is Trace line 3");
        }

    }
}
