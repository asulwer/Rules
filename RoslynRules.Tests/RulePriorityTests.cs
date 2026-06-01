using FluentAssertions;
using RoslynRules.Models;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests
{
    /// <summary>
    /// Tests for Rule.Priority execution ordering.
    /// </summary>
    public class RulePriorityTests
    {
        private readonly RuleParameter[] _parameters;

        public RulePriorityTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public void Execute_HighPriorityFirst_ExecutesInOrder()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Low", Expression = "true", Priority = 0 },
                    new Rule { Description = "High", Expression = "true", Priority = 10 },
                    new Rule { Description = "Medium", Expression = "true", Priority = 5 }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });
            var results = workflow.Execute(_parameters).ToList();

            results[0].RuleDescription.Should().Be("High");
            results[1].RuleDescription.Should().Be("Medium");
            results[2].RuleDescription.Should().Be("Low");
        }

        [Fact]
        public void Execute_NegativePriority_ExecutesAfterDefault()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Default", Expression = "true", Priority = 0 },
                    new Rule { Description = "Negative", Expression = "true", Priority = -5 }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });
            var results = workflow.Execute(_parameters).ToList();

            results[0].RuleDescription.Should().Be("Default");
            results[1].RuleDescription.Should().Be("Negative");
        }

        [Fact]
        public void ExecuteParallel_RespectsPriority()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Low", Expression = "true", Priority = 0 },
                    new Rule { Description = "High", Expression = "true", Priority = 10 }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });
            var results = workflow.ExecuteParallel(_parameters);

            results[0].RuleDescription.Should().Be("High");
            results[1].RuleDescription.Should().Be("Low");
        }

        [Fact]
        public void Priority_CannotChangeAfterCompile()
        {
            var rule = new Rule
            {
                Description = "Test",
                Expression = "true",
                Priority = 5
            };

            var compiler = new Compiler.ExpressionCompiler();
            rule.Compile(compiler, _parameters, new[] { "RoslynRules.Tests" });

            var act = () => rule.Priority = 10;
            act.Should().Throw<RoslynRules.Exceptions.RuleCompilationException>();
        }

        [Fact]
        public void Batch_RespectsPriority()
        {
            var batch = new Batch.RuleBatch()
                .AddRule(new Rule { Description = "Low", Expression = "true", Priority = 0 })
                .AddRule(new Rule { Description = "High", Expression = "true", Priority = 10 })
                .AddRule(new Rule { Description = "Medium", Expression = "true", Priority = 5 });

            batch.Compile(_parameters, new[] { "RoslynRules.Tests" });
            var results = batch.Evaluate(_parameters).ToList();

            results[0].RuleDescription.Should().Be("High");
            results[1].RuleDescription.Should().Be("Medium");
            results[2].RuleDescription.Should().Be("Low");
        }
    }
}