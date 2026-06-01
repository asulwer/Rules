using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for Rule lifecycle events (OnRuleExecuting, OnRuleExecuted).
    /// </summary>
    public class RuleLifecycleEventTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleLifecycleEventTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 30, Name = "Alice" })
            };
        }

        [Fact]
        public void Execute_OnRuleExecuting_FiresBeforeEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executingFired = false;
            rule.OnRuleExecuting += (sender, args) =>
            {
                executingFired = true;
                args.Rule.Should().Be(rule);
                args.Parameters.Should().BeEquivalentTo(_parameters);
                args.Cancel.Should().BeFalse();
            };

            rule.Execute(_parameters);

            executingFired.Should().BeTrue();
        }

        [Fact]
        public void Execute_OnRuleExecuted_FiresAfterEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executedFired = false;
            rule.OnRuleExecuted += (sender, args) =>
            {
                executedFired = true;
                args.Rule.Should().Be(rule);
                args.Result.Success.Should().BeTrue();
                args.Result.RuleId.Should().Be(rule.Id);
                args.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
                args.Exception.Should().BeNull();
            };

            rule.Execute(_parameters);

            executedFired.Should().BeTrue();
        }

        [Fact]
        public void Execute_OnRuleExecuting_Cancel_SkipsEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executedFired = false;
            rule.OnRuleExecuting += (sender, args) =>
            {
                args.Cancel = true;
                args.CancelReason = "Skipped by test";
            };

            rule.OnRuleExecuted += (sender, args) =>
            {
                executedFired = true;
                args.Result.Success.Should().BeTrue(); // Cancelled = success
                args.Result.Exception.Should().NotBeNull();
                args.Result.Exception.Should().BeOfType<OperationCanceledException>();
            };

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue();
            executedFired.Should().BeTrue();
        }

        [Fact]
        public void Execute_OnRuleExecuted_CapturesFailedResult()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 100",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executedFired = false;
            rule.OnRuleExecuted += (sender, args) =>
            {
                executedFired = true;
                args.Result.Success.Should().BeFalse();
                args.Exception.Should().BeNull();
            };

            rule.Execute(_parameters);

            executedFired.Should().BeTrue();
        }

        [Fact]
        public void Execute_OnRuleExecuted_CapturesException()
        {
            var rule = new Rule
            {
                Description = "Error rule",
                Expression = "1 / (customer.Age - 30) == 1",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executedFired = false;
            rule.OnRuleExecuted += (sender, args) =>
            {
                executedFired = true;
                // OnRuleExecuted fires for successful completion; exceptions bypass it
            };

            rule.Execute(_parameters);

            // Exception causes OnRuleExecuted to NOT fire (exception propagates to caller)
            executedFired.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_OnRuleExecuting_FiresBeforeEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "await Task.FromResult(customer.Age >= 18)",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executingFired = false;
            rule.OnRuleExecuting += (sender, args) =>
            {
                executingFired = true;
            };

            await rule.ExecuteAsync(_parameters);

            executingFired.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_OnRuleExecuted_FiresAfterEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "await Task.FromResult(customer.Age >= 18)",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            bool executedFired = false;
            rule.OnRuleExecuted += (sender, args) =>
            {
                executedFired = true;
                args.Result.Success.Should().BeTrue();
            };

            await rule.ExecuteAsync(_parameters);

            executedFired.Should().BeTrue();
        }

        [Fact]
        public void Execute_NoSubscribers_DoesNotThrow()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            var act = () => rule.Execute(_parameters);
            act.Should().NotThrow();
        }

        [Fact]
        public void Execute_ChildRule_FiresEventsForParentAndChild()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child",
                Expression = "customer.Age >= 21",
                IsActive = true
            };

            parent.ChildRules.Add(child);

            var compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
            parent.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            int childExecutingCount = 0;
            int childExecutedCount = 0;
            int parentExecutingCount = 0;
            int parentExecutedCount = 0;

            child.OnRuleExecuting += (s, e) => childExecutingCount++;
            child.OnRuleExecuted += (s, e) => childExecutedCount++;
            parent.OnRuleExecuting += (s, e) => parentExecutingCount++;
            parent.OnRuleExecuted += (s, e) => parentExecutedCount++;

            parent.Execute(_parameters);

            childExecutingCount.Should().Be(1);
            childExecutedCount.Should().Be(1);
            parentExecutingCount.Should().Be(1);
            parentExecutedCount.Should().Be(1);
        }
    }
}