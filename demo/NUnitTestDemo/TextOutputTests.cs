using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

namespace NUnitTestDemo
{
    [ExpectPass]
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

        [Test, Description("Displays various settings for verification")]
        public void DisplayTestSettings()
        {
            Console.WriteLine("CurrentDirectory={0}", Environment.CurrentDirectory);
            Console.WriteLine("BasePath={0}", AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine("PrivateBinPath={0}", AppDomain.CurrentDomain.SetupInformation.PrivateBinPath);
            Console.WriteLine("WorkDirectory={0}", TestContext.CurrentContext.WorkDirectory);
            Console.WriteLine("DefaultTimeout={0}", NUnit.Framework.Internal.TestExecutionContext.CurrentContext.TestCaseTimeout);
        }

        [Test]
        public void DisplayTestParameters()
        {
            if (TestContext.Parameters.Count == 0)
                Console.WriteLine("No TestParameters were passed");
            else
            foreach (var name in TestContext.Parameters.Names)
                Console.WriteLine("Parameter: {0} = {1}", name, TestContext.Parameters.Get(name));
        }
    }
}
