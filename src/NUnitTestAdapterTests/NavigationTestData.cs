using System;
using System.Globalization;
using System.Threading.Tasks;
namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // Contains methods used in testing NavigationDataProvider.
    class NavigationTestData
    {
        public void EmptyMethod_OneLine() { } // expectedLineDebug = expectedLineRelease = 8

        public void EmptyMethod_TwoLines()
        { // expectedLineDebug = 12
        } // expectedLineRelease = 13

        public void EmptyMethod_ThreeLines()
        { // expectedLineDebug = 16
        } // expectedLineRelease = 17

        public void EmptyMethod_LotsOfLines()
        { // expectedLineDebug = 20


        } // expectedLineRelease = 23

        public void SimpleMethod_Void_NoArgs()
        {// expectedLineDebug = 26
            const int answer = 42;
            Console.Write(answer); // expectedLineRelease = 28
        }

        public void SimpleMethod_Void_OneArg(int x)
        { // expectedLineDebug = 32
            var answer = x; // expectedLineRelease = 33
            Console.Write(answer);
        }

        public void SimpleMethod_Void_TwoArgs(int x, int y)
        { // expectedLineDebug = 38
            var answer = x + y; // expectedLineRelease = 39
            Console.Write(answer);
        }

        public int SimpleMethod_ReturnsInt_NoArgs()
        {// expectedLineDebug = 44
            const int answer = 42;
            return answer; // expectedLineRelease = 46
        }

        public string SimpleMethod_ReturnsString_OneArg(int x)
        { // expectedLineDebug = 50
            return x.ToString(CultureInfo.InvariantCulture); // expectedLineRelease = 51
        }

        public string GenericMethod_ReturnsString_OneArg<T>(T x)
        { // expectedLineDebug = 55
            return x.ToString(); // expectedLineRelease = 56
        }

        public async void AsyncMethod_Void()
        { // expectedLineDebug = 60
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease = 62
            Console.Write(answer);
        }

        public async Task AsyncMethod_Task()
        { // expectedLineDebug = 67
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease = 69
            Console.Write(answer);
        }

        public async Task<int> AsyncMethod_ReturnsInt()
        { // expectedLineDebug = 74
            const int answer = 42;
            await Task.Delay(0); // expectedLineRelease = 76
            return answer;
        }

        public class NestedClass
        {
            public void SimpleMethod_Void_NoArgs()
            {// expectedLineDebug = 83
                const int answer = 42;
                Console.Write(answer); // expectedLineRelease = 85
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
            { // expectedLineDebug = 101
                return s + x * i; // expectedLineRelease = 102
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
            { // expectedLineDebug = 116
                return x.Equals(y); // expectedLineRelease = 117
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
                { // expectedLineDebug = 132
                    Console.Write(X + Y.ToString()); // expectedLineRelease = 133
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
                { // expectedLineDebug = 151
                    Console.Write(X + Y.ToString() + Z); // expectedLineRelease = 152
                }
            }
        }

        public abstract class BaseClass
        {
            public void EmptyMethod_ThreeLines()
            { // expectedLineDebug = 160
            } // expectedLineRelease = 161
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
