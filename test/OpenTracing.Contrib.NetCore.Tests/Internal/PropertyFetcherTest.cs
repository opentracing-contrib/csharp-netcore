using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing.Contrib.NetCore.Internal;
using Xunit;

namespace OpenTracing.Contrib.NetCore.Tests.Internal
{
    public class PropertyFetcherTest
    {
        public class TestClass
        {
            public string TestProperty { get; set; }
        }

        [Fact]
        public void Fetch_NameNotFound_NullReturned()
        {
            var obj = new TestClass { TestProperty = "TestValue" };

            var sut = new PropertyFetcher("DifferentProperty");

            var result = sut.Fetch(obj);

            Assert.Null(result);
        }

        [Fact]
        public void Fetch_NameFound_ValueReturned()
        {
            var obj = new TestClass { TestProperty = "TestValue" };

            var sut = new PropertyFetcher("TestProperty");

            var result = sut.Fetch(obj);

            Assert.Equal("TestValue", result);
        }

        [Fact]
        public void Fetch_NameFoundDifferentCasing_ValueReturned()
        {
            var obj = new TestClass { TestProperty = "TestValue" };

            var sut = new PropertyFetcher("testproperty");

            var result = sut.Fetch(obj);

            Assert.Equal("TestValue", result);
        }
    }
}
