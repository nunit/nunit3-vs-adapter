using System;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // Contains methods used in testing use of DiaSession
    class DiaSessionTestData
    {
        public void EmptyMethod_OneLine() { } // Line 8

        public void EmptyMethod_TwoLines()
        { // Line 11
        } // Line 12

        public void EmptyMethod_ThreeLines()
        { // Line 15
        } // Line 16

        public void EmptyMethod_LotsOfLines()
        { // Line 19


        } // Line 22

        public void SimpleMethod_Void_NoArgs()
        { // Line 25
            int answer = 42;
            Console.Write(answer);
        } // Line 28

        public void SimpleMethod_Void_OneArg(int x)
        { // Line 31
            int answer = x;
            Console.Write(answer);
        } // Line 34

        public void SimpleMethod_Void_TwoArgs(int x, int y)
        { // Line 37
            int answer = x + y;
            Console.Write(answer);
        } // Line 40

        public int SimpleMethod_ReturnsInt_NoArgs()
        { // Line 43
            int answer = 42;
            return answer;
        } // Line 46

        public string SimpleMethod_ReturnsString_OneArg(int x)
        { // Line 49
            return x.ToString();
        } // Line 51

        public string GenericMethod_ReturnsString_OneArg<T>(T x)
        { // Line 54
            return x.ToString();
        } // Line 56

        public class NestedClass
        {
            public void SimpleMethod_Void_NoArgs()
            { // Line 61
                int answer = 42;
                Console.Write(answer);
            } // Line 64
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
            { // Line 79
                return this.s + this.x * i;
            } // Line 81
        }

        public class GenericFixture<T1, T2>
        {
            private T1 x;

            public GenericFixture(T1 x) 
            {
                this.x = x;
            }

            public bool Matches(T2 y)
            { // Line 94
                return x.Equals(y);
            } // Line 96

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
                { // Line 110
                    Console.Write(x.ToString() + y.ToString());
                } // Line 112
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
                { // Line 129
                    Console.Write(x.ToString() + y.ToString() + z.ToString());
                } // Line 131
            }
        }
    }
}
