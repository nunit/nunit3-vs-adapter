using NUnitLite;
using System;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            return new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
        }
    }
}
