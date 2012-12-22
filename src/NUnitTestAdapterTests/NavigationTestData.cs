using System;
using System.Threading.Tasks;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // Contains methods used in testing use of DiaSession.
    class NavigationTestData
    {
        public void EmptyMethod_OneLine() { } // minLineDebug = minLineRelease = maxLineDebug = maxLineRelease = 8

        public void EmptyMethod_TwoLines()
        { // minLineDebug = 12
        } // minLineRelease = maxLineDebug = maxLineRelease = 13

        public void EmptyMethod_ThreeLines()
        { // minLineDebug = 16
        } // minLineRelease = maxLineDebug = maxLineRelease = 17

        public void EmptyMethod_LotsOfLines()
        { // minLineDebug = 20


        } // minLineRelease = maxLineDebug = maxLineRelease = 23

        public void SimpleMethod_Void_NoArgs()
        { // minLineDebug = 26
            int answer = 42; // minLineRelease = 27
            Console.Write(answer);
        } // maxLineDebug = maxLineRelease = 29

        public void SimpleMethod_Void_OneArg(int x)
        { // minLineDebug = 32
            int answer = x; // minLineRelease = 33
            Console.Write(answer);
        } // maxLineDebug = maxLineRelease = 35

        public void SimpleMethod_Void_TwoArgs(int x, int y)
        { // minLineDebug = 38
            int answer = x + y; // minLineRelease = 39
            Console.Write(answer);
        } // maxLineDebug = maxLineRelease = 41

        public int SimpleMethod_ReturnsInt_NoArgs()
        { // minLineDebug = 44
            int answer = 42; // minLineRelease = 45
            return answer; // maxLineRelease = 46
        } // maxLineDebug = 47

        public string SimpleMethod_ReturnsString_OneArg(int x)
        { // minLineDebug = 50
            return x.ToString(); // minLineRelease = maxLineRelease = 51
        } // maxLineDebug = 52

        public string GenericMethod_ReturnsString_OneArg<T>(T x)
        { // minLineDebug = 55
            return x.ToString(); // minLineRelease = maxLineRelease = 56
        } // maxLineDebug = 57

        public async void AsyncMethod_Void()
        { // minLineDebug = 60
            int answer = 42; // minLineRelease = 61
            await Task.Delay(0);
            Console.Write(answer);
        } // maxLineDebug = maxLineRelease = 64

        public async Task AsyncMethod_Task()
        { // minLineDebug = 67
            int answer = 42; // minLineRelease = 68
            await Task.Delay(0);
            Console.Write(answer);
        } // maxLineDebug = maxLineRelease = 71

        public async Task<int> AsyncMethod_ReturnsInt()
        { // minLineDebug = 74
            int answer = 42; // minLineRelease = 75
            await Task.Delay(0);
            return answer;
        } // maxLineDebug = maxLineRelease = 78

        public class NestedClass
        {
            public void SimpleMethod_Void_NoArgs()
            { // minLineDebug = 83
                int answer = 42; // minLineRelease = 84
                Console.Write(answer);
            } // maxLineDebug = maxLineRelease = 86
        }

        public class ParameterizedFixture
        {
            private double x;
            private string s;

            public ParameterizedFixture(double x, string s) 
            {
                this.x = x;
                this.s = s;
            }

            public string SimpleMethod_ReturnsString_OneArg(int i)
            { // minLineDebug = 101
                return this.s + this.x * i; // minLineRelease = maxLineRelease = 102
            } // maxLineDebug = 103
        }

        public class GenericFixture<T1, T2>
        {
            private T1 x;

            public GenericFixture(T1 x) 
            {
                this.x = x;
            }

            public bool Matches(T2 y)
            { // minLineDebug = 116
                return x.Equals(y); // minLineRelease = maxLineRelease = 117
            } // maxLineDebug = 118

            public class DoublyNested
            {
                public T1 x;
                public T2 y;

                public DoublyNested(T1 x, T2 y)
                {
                    this.x = x;
                    this.y = y;
                }

                public void WriteBoth()
                { // minLineDebug = 132
                    Console.Write(x.ToString() + y.ToString()); // minLineRelease = 133
                } // maxLineDebug = maxLineRelease = 134
            }

            public class DoublyNested<T3>
            {
                public T1 x;
                public T2 y;
                public T3 z;

                public DoublyNested(T1 x, T2 y, T3 z)
                {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                }

                public void WriteAllThree()
                { // minLineDebug = 151
                    Console.Write(x.ToString() + y.ToString() + z.ToString()); // minLineRelease = 152
                } // maxLineDebug = maxLineRelease = 153
            }
        }
    }
}
