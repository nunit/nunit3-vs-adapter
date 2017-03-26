#if NETCOREAPP1_0
using NUnitLite;
using System;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
            return result;
        }
    }
}
#endif