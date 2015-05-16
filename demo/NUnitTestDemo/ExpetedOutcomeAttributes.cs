using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class ExpectPassAttribute : PropertyAttribute
    {
        public ExpectPassAttribute() : base("Expect", "Pass") { }
    }


    public class ExpectFailureAttribute : PropertyAttribute
    {
        public ExpectFailureAttribute() : base("Expect", "Failure") { }
    }

    public class ExpectIgnoreAttribute : PropertyAttribute
    {
        public ExpectIgnoreAttribute() : base("Expect", "Ignore") { }
    }

    public class ExpectErrorAttribute : PropertyAttribute
    {
        public ExpectErrorAttribute() : base("Expect", "Error") { }
    }

    public class ExpectInconclusiveAttribute : PropertyAttribute
    {
        public ExpectInconclusiveAttribute() : base("Expect", "Inconclusive") { }
    }

    public class ExpectMixedAttribute : PropertyAttribute
    {
        public ExpectMixedAttribute() : base("Expect", "Mixed") { }
    }
}
