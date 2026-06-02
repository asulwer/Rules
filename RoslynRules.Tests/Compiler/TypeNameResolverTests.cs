using FluentAssertions;
using RoslynRules.Compiler;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Tests for TypeNameResolver.
    /// </summary>
    public class TypeNameResolverTests
    {
        [Fact]
        public void GetTypeName_GenericType_ReturnsCSharpName()
        {
            TypeNameResolver.GetTypeName(typeof(System.Collections.Generic.List<int>))
                .Should().Be("List<int>");
        }

        [Fact]
        public void GetTypeName_ArrayType_ReturnsCSharpName()
        {
            TypeNameResolver.GetTypeName(typeof(int[]))
                .Should().Be("int[]");
        }

        [Fact]
        public void GetTypeName_NestedType_ReturnsDotSeparator()
        {
            TypeNameResolver.GetTypeName(typeof(System.Environment.SpecialFolder))
                .Should().Be("System.Environment.SpecialFolder");
        }

        [Fact]
        public void GetTypeName_NullableType_ReturnsCSharpName()
        {
            TypeNameResolver.GetTypeName(typeof(int?))
                .Should().Be("int?");
        }

        [Fact]
        public void GetTypeName_DictionaryType_ReturnsCSharpName()
        {
            TypeNameResolver.GetTypeName(typeof(System.Collections.Generic.Dictionary<string, int>))
                .Should().Be("Dictionary<string, int>");
        }

        [Fact]
        public void GetTypeName_Primitives_ReturnCSharpAliases()
        {
            TypeNameResolver.GetTypeName(typeof(void)).Should().Be("void");
            TypeNameResolver.GetTypeName(typeof(string)).Should().Be("string");
            TypeNameResolver.GetTypeName(typeof(int)).Should().Be("int");
            TypeNameResolver.GetTypeName(typeof(bool)).Should().Be("bool");
            TypeNameResolver.GetTypeName(typeof(object)).Should().Be("object");
            TypeNameResolver.GetTypeName(typeof(long)).Should().Be("long");
            TypeNameResolver.GetTypeName(typeof(double)).Should().Be("double");
            TypeNameResolver.GetTypeName(typeof(float)).Should().Be("float");
            TypeNameResolver.GetTypeName(typeof(decimal)).Should().Be("decimal");
            TypeNameResolver.GetTypeName(typeof(char)).Should().Be("char");
            TypeNameResolver.GetTypeName(typeof(byte)).Should().Be("byte");
        }

        [Fact]
        public void GetTypeName_DeeplyNestedType_ReturnsDotSeparatedPath()
        {
            // Use a real deeply nested type from the framework
            TypeNameResolver.GetTypeName(typeof(System.IO.FileAccess))
                .Should().Be("System.IO.FileAccess");
        }
    }
}
