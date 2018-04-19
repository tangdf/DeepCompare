using System;
using Xunit;

namespace DeepCompare.UnitTest
{
    public class DeepCompareTest
    {
        [Fact]
        public void Complex_Object_Test()
        {
            Foo foo1 = new Foo {
                A = "A",
                B = "B",
                C = 1
            };
            foo1.FooField = foo1;

            Foo foo2 = new Foo {
                A = "A",
                B = "B",
                C = 1
            };

            foo2.FooField = foo2;
            var result = DeepComparer.Compare((object)foo1, (object)foo2);

            Assert.True(result);

            foo1.A = "AA";

           result = DeepComparer.Compare(foo1, foo2);

            Assert.False(result);
        }

        [Fact]
        public void Array_Test()
        {
            var array1 = new string[] { "A", "B" };

            var array2 = new string[] { "A", "B" };

            var result = DeepComparer.Compare((object) array1, (object) array2);

            Assert.True(result);


            array1[1] = "C";

            result = DeepComparer.Compare(array1, array2);

            Assert.False(result);

            var obj1 = new int[2,1,3] { { { 1, 2, 3 } }, { { 4, 5, 6 } } };

            var obj2 = new int[2, 1, 3] { { { 1, 2, 3 } }, { { 4, 5, 6 } } };

            result = DeepComparer.Compare((object) obj1, (object) obj2);

            Assert.True(result);
        }


        class Foo
        {
            public String A;
            public String B;

            public int C;

            public object D;

            public Foo FooField;
        }
    }
}