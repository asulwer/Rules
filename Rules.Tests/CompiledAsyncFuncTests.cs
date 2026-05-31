using FluentAssertions;
using Rules.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Rules.Tests
{
    /// <summary>
    /// Tests for CompiledAsyncFunc to cover Invoke, ToString, and other paths.
    /// </summary>
    public class CompiledAsyncFuncTests
    {
        [Fact]
        public async Task InvokeAsync_AsyncFunc_ReturnsValue()
        {
            Func<object, Task<bool>> asyncFunc = async (obj) =>
            {
                await Task.Delay(1);
                return true;
            };
            var compiled = new CompiledAsyncFunc<object, bool>(asyncFunc);

            var result = await compiled.InvokeAsync("test");

            ((bool)result!).Should().BeTrue();
        }

        [Fact]
        public void Invoke_SyncFallback_ReturnsValue()
        {
            Func<object, Task<bool>> asyncFunc = (obj) => Task.FromResult(true);
            var compiled = new CompiledAsyncFunc<object, bool>(asyncFunc);

            var result = compiled.Invoke("test");

            ((bool)result!).Should().BeTrue();
        }

        [Fact]
        public void ToString_ReturnsReadableString()
        {
            Func<object, Task<bool>> asyncFunc = (obj) => Task.FromResult(true);
            var compiled = new CompiledAsyncFunc<object, bool>(asyncFunc);

            var str = compiled.ToString();

            str.Should().Contain("CompiledAsyncFunc");
        }

        [Fact]
        public void BaseType_IsCompiledDelegate()
        {
            Func<object, Task<bool>> asyncFunc = (obj) => Task.FromResult(true);
            var compiled = new CompiledAsyncFunc<object, bool>(asyncFunc);

            compiled.GetType().BaseType.Should().Be(typeof(CompiledDelegate));
        }
    }
}
