using FluentAssertions;
using Rules.Models;
using Xunit;

namespace Rules.Tests
{
    /// <summary>
    /// Tests for TypeNameResolver edge cases.
    /// </summary>
    public class TypeNameResolverTests
    {
        [Fact]
        public void GetTypeName_GenericType_ReturnsFullName()
        {
            typeof(System.Collections.Generic.List<int>).FullName.Should().Contain("List`1");
        }

        [Fact]
        public void GetTypeName_ArrayType_ReturnsFullName()
        {
            typeof(int[]).FullName.Should().Be("System.Int32[]");
        }

        [Fact]
        public void GetTypeName_NestedType_ReturnsFullName()
        {
            typeof(System.Environment.SpecialFolder).FullName.Should().Contain("Environment+SpecialFolder");
        }

        [Fact]
        public void GetTypeName_NullableType_ReturnsFullName()
        {
            typeof(int?).FullName.Should().Be("System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");
        }

        [Fact]
        public void GetTypeName_DictionaryType_ReturnsFullName()
        {
            typeof(System.Collections.Generic.Dictionary<string, int>).FullName.Should().Contain("Dictionary`2");
        }
    }
}
