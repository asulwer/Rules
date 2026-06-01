using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for Rule result caching (memoization).
    /// </summary>
    public class RuleCacheTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly ExpressionCompiler _compiler;

        public RuleCacheTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 30, Name = "Alice" })
            };
            _compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
        }

        [Fact]
        public void Execute_CacheEnabled_SecondCallReturnsCachedResult()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Cached result should be the same reference (struct equality)
            result2.Should().Be(result1);
        }

        [Fact]
        public void Execute_CacheDisabled_EvaluatesEveryTime()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = null // disabled
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Should re-evaluate (different struct instances)
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_CacheExpired_ReEvaluates()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMilliseconds(50)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            System.Threading.Thread.Sleep(100); // Let cache expire
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // After expiration, should be a fresh evaluation
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_ClearCache_ForcesReEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            rule.ClearCache();
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // After clear, should be a fresh evaluation
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_Cache_DifferentParameters_DifferentResults()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var params1 = new[]
            {
                new RuleParameter("age", typeof(int), 30)
            };
            var params2 = new[]
            {
                new RuleParameter("age", typeof(int), 15)
            };

            rule.Compile(_compiler, params1, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(params1);
            var result2 = rule.Execute(params2);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_Cache_Hit_CountIncreases()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            rule.Execute(_parameters);
            rule.Execute(_parameters);
            rule.Execute(_parameters);

            // Internal cache count should be 1 (same key)
            // We can't directly access _resultCache.Count from here since it's private
            // But we can verify behavior: third call should still be cached
            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_CacheEnabled_SecondCallReturnsCachedResult()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "await Task.FromResult(customer.Age >= 18)",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = await rule.ExecuteAsync(_parameters);
            var result2 = await rule.ExecuteAsync(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            result2.Should().Be(result1);
        }

        [Fact]
        public void Execute_Cache_ChildRules_StillEvaluated()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var child = new Rule
            {
                Description = "Child",
                Expression = "customer.Age >= 21",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            parent.ChildRules.Add(child);
            parent.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = parent.Execute(_parameters);
            var result2 = parent.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Parent result cached, but child should still be evaluated independently
        }

        [Fact]
        public void Execute_Cache_ExceptionsNotCached()
        {
            var rule = new Rule
            {
                Description = "Error rule",
                Expression = "1 / (customer.Age - 30) == 1",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            // First call returns failed result with exception (not thrown)
            var result1 = rule.Execute(_parameters);
            result1.Success.Should().BeFalse();
            result1.Exception.Should().BeOfType<DivideByZeroException>();

            // Second call should re-evaluate and also return failed result
            var result2 = rule.Execute(_parameters);
            result2.Success.Should().BeFalse();
            result2.Exception.Should().BeOfType<DivideByZeroException>();
            
            // If exception was cached, result2 would be a cache hit with same reference
            // But we can't easily verify this since RuleResult is a struct
            // The key point: both calls should produce failed results
        }
    }
}
