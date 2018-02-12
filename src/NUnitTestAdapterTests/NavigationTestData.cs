using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // Contains methods used in testing NavigationDataProvider.
    class NavigationTestData
    {
        public void EmptyMethod_OneLine() { } // expectedLineDebug, expectedLineRelease

        public void EmptyMethod_TwoLines()
        { // expectedLineDebug
        } // expectedLineRelease

        public void EmptyMethod_ThreeLines()
        { // expectedLineDebug
        } // expectedLineRelease

        public void EmptyMethod_LotsOfLines()
        { // expectedLineDebug


        } // expectedLineRelease

        public void SimpleMethod_Void_NoArgs()
        {// expectedLineDebug
            const int answer = 42;
            Console.Write(answer); // expectedLineRelease
        }

        public void SimpleMethod_Void_OneArg(int x)
        { // expectedLineDebug
            var answer = x; // expectedLineRelease
            Console.Write(answer);
        }

        public void SimpleMethod_Void_TwoArgs(int x, int y)
        { // expectedLineDebug
            var answer = x + y; // expectedLineRelease
            Console.Write(answer);
        }

        public int SimpleMethod_ReturnsInt_NoArgs()
        {// expectedLineDebug
            const int answer = 42;
            return answer; // expectedLineRelease
        }

        public string SimpleMethod_ReturnsString_OneArg(int x)
        { // expectedLineDebug
            return x.ToString(CultureInfo.InvariantCulture); // expectedLineRelease
        }

        public string GenericMethod_ReturnsString_OneArg<T>(T x)
        { // expectedLineDebug
            return x.ToString(); // expectedLineRelease
        }

        public async void AsyncMethod_Void()
        { // expectedLineDebug
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease
            Console.Write(answer);
        }

        public async Task AsyncMethod_Task()
        { // expectedLineDebug
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease
            Console.Write(answer);
        }

        public async Task<int> AsyncMethod_ReturnsInt()
        { // expectedLineDebug
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease
            return answer;
        }

        public IEnumerable<int> IteratorMethod_ReturnsEnumerable()
        { // expectedLineDebug
            const int answer = 42;
            yield return answer; // expectedLineRelease
        }

        public IEnumerator<int> IteratorMethod_ReturnsEnumerator()
        { // expectedLineDebug
            const int answer = 42;
            yield return answer; // expectedLineRelease
        }

        public class NestedClass
        {
            public void SimpleMethod_Void_NoArgs()
            {// expectedLineDebug
                const int answer = 42;
                Console.Write(answer); // expectedLineRelease
            }
        }

        public class ParameterizedFixture
        {
            private readonly double x;
            private readonly string s;

            public ParameterizedFixture(double x, string s)
            {
                this.x = x;
                this.s = s;
            }

            public string SimpleMethod_ReturnsString_OneArg(int i)
            { // expectedLineDebug
                return s + x * i; // expectedLineRelease
            }
        }

        public class GenericFixture<T1, T2>
        {
            private readonly T1 x;

            public GenericFixture(T1 x)
            {
                this.x = x;
            }

            public bool Matches(T2 y)
            { // expectedLineDebug
                return x.Equals(y); // expectedLineRelease
            }

            public class DoublyNested
            {
                public T1 X;
                public T2 Y;

                public DoublyNested(T1 x, T2 y)
                {
                    X = x;
                    Y = y;
                }

                public void WriteBoth()
                { // expectedLineDebug
                    Console.Write(X + Y.ToString()); // expectedLineRelease
                }
            }

            public class DoublyNested<T3>
            {
                public T1 X;
                public T2 Y;
                public T3 Z;

                public DoublyNested(T1 x, T2 y, T3 z)
                {
                    X = x;
                    Y = y;
                    Z = z;
                }

                public void WriteAllThree()
                { // expectedLineDebug
                    Console.Write(X + Y.ToString() + Z); // expectedLineRelease
                }
            }
        }

        public abstract class BaseClass
        {
            public void EmptyMethod_ThreeLines()
            { // expectedLineDebug
            } // expectedLineRelease
        }

        public class DerivedClass : BaseClass
        {
        }

        public class DerivedFromExternalAbstractClass : AbstractBaseClass
        {
        }

        public class DerivedFromExternalConcreteClass : ConcreteBaseClass
        {
        }
    }
}
