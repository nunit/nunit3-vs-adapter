using System;
using System.Threading.Tasks;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // Contains methods used in testing use of DiaSession
    class DiaSessionTestData
    {
        public void EmptyMethod_OneLine() { } // Line 8

        public void EmptyMethod_TwoLines()
        { // Line 12
        } // Line 13

        public void EmptyMethod_ThreeLines()
        { // Line 16
        } // Line 17

        public void EmptyMethod_LotsOfLines()
        { // Line 20


        } // Line 23

        public void SimpleMethod_Void_NoArgs()
        { // Line 26
            int answer = 42;
            Console.Write(answer);
        } // Line 29

        public void SimpleMethod_Void_OneArg(int x)
        { // Line 32
            int answer = x;
            Console.Write(answer);
        } // Line 35

        public void SimpleMethod_Void_TwoArgs(int x, int y)
        { // Line 38
            int answer = x + y;
            Console.Write(answer);
        } // Line 41

        public int SimpleMethod_ReturnsInt_NoArgs()
        { // Line 44
            int answer = 42;
            return answer;
        } // Line 47

        public string SimpleMethod_ReturnsString_OneArg(int x)
        { // Line 50
            return x.ToString();
        } // Line 52

        public string GenericMethod_ReturnsString_OneArg<T>(T x)
        { // Line 55
            return x.ToString();
        } // Line 57

        public async void AsyncMethod_Void()
        { // Line 60
            int answer = 42;
            await Task.Delay(0);
            Console.Write(answer);
        } // Line 64

        public async Task AsyncMethod_Task()
        { // Line 67
            int answer = 42;
            await Task.Delay(0);
            Console.Write(answer);
        } // Line 71

        public async Task<int> AsyncMethod_ReturnsInt()
        { // Line 74
            int answer = 42;
            await Task.Delay(0);
            return answer;
        } // Line 78

        public class NestedClass
        {
            public void SimpleMethod_Void_NoArgs()
            { // Line 83
                int answer = 42;
                Console.Write(answer);
            } // Line 86
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
            { // Line 101
                return this.s + this.x * i;
            } // Line 103
        }

        public class GenericFixture<T1, T2>
        {
            private T1 x;

            public GenericFixture(T1 x) 
            {
                this.x = x;
            }

            public bool Matches(T2 y)
            { // Line 116
                return x.Equals(y);
            } // Line 118

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
                { // Line 132
                    Console.Write(x.ToString() + y.ToString());
                } // Line 134
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
                { // Line 151
                    Console.Write(x.ToString() + y.ToString() + z.ToString());
                } // Line 153
            }
        }
    }
}
