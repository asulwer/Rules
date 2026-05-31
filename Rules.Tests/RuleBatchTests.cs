using FluentAssertions;
using Rules.Batch;
using Rules.Exceptions;
using Rules.Models;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rules.Tests
{
    /// <summary>
    /// Tests for RuleBatch batch evaluation.
    /// </summary>
    public class RuleBatchTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleBatchTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public void AddRule_BuilderPattern_ReturnsBatch()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "true" })
                .AddRule(new Rule { Description = "R2", Expression = "true" });

            batch.Rules.Should().HaveCount(2);
        }

        [Fact]
        public void Validate_EmptyBatch_Throws()
        {
            var batch = new RuleBatch();
            var act = () => batch.Validate();
            act.Should().Throw<WorkflowException>();
        }

        [Fact]
        public void Validate_DuplicateIds_Throws()
        {
            var rule = new Rule { Description = "R1", Expression = "true" };
            var batch = new RuleBatch()
                .AddRule(rule)
                .AddRule(rule); // Same instance = same ID

            var act = () => batch.Validate();
            act.Should().Throw<DuplicateRuleIdException>();
        }

        [Fact]
        public void Compile_SharedCompiler_CompilesAll()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "customer.Age > 0" })
                .AddRule(new Rule { Description = "R2", Expression = "customer.Name != null" });

            batch.Compile(_parameters, new[] { "Rules.Tests" });

            batch.Rules[0].IsActive.Should().BeTrue();
            batch.Rules[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public void Evaluate_Sequential_ReturnsAllResults()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "customer.Age > 0" })
                .AddRule(new Rule { Description = "R2", Expression = "customer.Name != null" });

            batch.Compile(_parameters, new[] { "Rules.Tests" });
            var results = batch.Evaluate(_parameters).ToList();

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void EvaluateParallel_MultipleRules_ReturnsAll()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "customer.Age > 0" })
                .AddRule(new Rule { Description = "R2", Expression = "customer.Name != null" })
                .AddRule(new Rule { Description = "R3", Expression = "true" });

            batch.Compile(_parameters, new[] { "Rules.Tests" });
            var results = batch.EvaluateParallel(_parameters);

            results.Should().HaveCount(3);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateAsync_AsyncRules_ReturnsAll()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "await Task.FromResult(customer.Age > 0)" })
                .AddRule(new Rule { Description = "R2", Expression = "await Task.FromResult(true)" });

            batch.Compile(_parameters, new[] { "Rules.Tests", "System.Threading.Tasks" });

            var results = new System.Collections.Generic.List<RuleResult>();
            await foreach (var result in batch.EvaluateAsync(_parameters))
            {
                results.Add(result);
            }

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public async Task EvaluateParallelAsync_MultipleRules_ReturnsAll()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "await Task.FromResult(customer.Age > 0)" })
                .AddRule(new Rule { Description = "R2", Expression = "await Task.FromResult(true)" })
                .AddRule(new Rule { Description = "R3", Expression = "await Task.FromResult(customer.Name != null)" });

            batch.Compile(_parameters, new[] { "Rules.Tests", "System.Threading.Tasks" });
            var results = await batch.EvaluateParallelAsync(_parameters);

            results.Should().HaveCount(3);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void Evaluate_NotCompiled_Throws()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "true" });

            var act = () => batch.Evaluate(_parameters).ToList();
            act.Should().Throw<NotCompiledException>();
        }

        [Fact]
        public void AddRule_AfterCompile_Throws()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "true" });

            batch.Compile(_parameters, new[] { "Rules.Tests" });

            var act = () => batch.AddRule(new Rule { Description = "R2", Expression = "true" });
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Evaluate_WithInactiveRule_Skips()
        {
            var batch = new RuleBatch()
                .AddRule(new Rule { Description = "Active", Expression = "true", IsActive = true })
                .AddRule(new Rule { Description = "Inactive", Expression = "true", IsActive = false });

            batch.Compile(_parameters, new[] { "Rules.Tests" });
            var results = batch.Evaluate(_parameters).ToList();

            results.Should().HaveCount(1);
            results[0].RuleDescription.Should().Be("Active");
        }

        [Fact]
        public void Evaluate_TenRules_AllPass()
        {
            var batch = new RuleBatch();
            for (int i = 0; i < 10; i++)
            {
                batch.AddRule(new Rule
                {
                    Description = $"Rule {i + 1}",
                    Expression = "customer.Age > 0"
                });
            }

            batch.Compile(_parameters, new[] { "Rules.Tests" });
            var results = batch.EvaluateParallel(_parameters);

            results.Should().HaveCount(10);
            results.All(r => r.Success).Should().BeTrue();
        }
    }
}
