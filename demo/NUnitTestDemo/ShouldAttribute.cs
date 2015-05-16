using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class ShouldAttribute : PropertyAttribute
    {
        public ShouldAttribute(string result) : base(result) { }
    }
}
