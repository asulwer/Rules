using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for Rule logging edge cases (inactive rules, formatting, etc.)
    /// </summary>
    public class RuleLoggingTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleLoggingTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public void Execute_InactiveRule_LogsSkipped()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Inactive",
                Expression = "customer.Age > 0",
                IsActive = false,
                Logger = logger
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });
            rule.Execute(_parameters);

            logger.EventIds.Should().ContainSingle();
            logger.EventIds[0].Id.Should().Be(1001); // RuleSkipped
            logger.LogMessages[0].Should().Contain("[SKIP]");
        }

        [Fact]
        public void Execute_FailingRule_LogsFailed()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Fails",
                Expression = "customer.Age > 100",
                IsActive = true,
                Logger = logger
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });
            rule.Execute(_parameters);

            logger.EventIds[0].Id.Should().Be(1003); // RuleFailed
            logger.LogMessages[0].Should().Contain("[FAIL]");
        }

        [Fact]
        public void Execute_RuntimeError_LogsError()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Error",
                Expression = "1 / (customer.Age - 25) == 1",
                IsActive = true,
                Logger = logger
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });
            rule.Execute(_parameters);

            logger.EventIds[0].Id.Should().Be(1004); // RuleError
            logger.LogMessages[0].Should().Contain("[ERROR]");
        }

        [Fact]
        public void Execute_PassingRule_LogsPassed()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Passes",
                Expression = "customer.Age > 0",
                IsActive = true,
                Logger = logger
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });
            rule.Execute(_parameters);

            logger.EventIds[0].Id.Should().Be(1002); // RulePassed
            logger.LogMessages[0].Should().Contain("[PASS]");
        }

        [Fact]
        public void Execute_WithNullLogger_DoesNotThrow()
        {
            var rule = new Rule
            {
                Description = "No logger",
                Expression = "customer.Age > 0",
                IsActive = true,
                Logger = null
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });
            
            var act = () => rule.Execute(_parameters);
            act.Should().NotThrow();
        }
    }
}
