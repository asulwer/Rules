using FluentAssertions;
using RoslynRules.Models;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests
{
    /// <summary>
    /// Tests for Workflow.ExecuteParallel and ExecuteParallelAsync.
    /// </summary>
    public class WorkflowParallelTests
    {
        private readonly RuleParameter[] _parameters;

        public WorkflowParallelTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public void ExecuteParallel_MultipleRules_ReturnsAll()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule 1",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Rule 2",
                        Expression = "customer.Name != null",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void ExecuteParallel_InactiveWorkflow_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                IsActive = false,
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule",
                        Expression = "true",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().BeEmpty();
        }

        [Fact]
        public void ExecuteParallel_NoActiveRules_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Inactive",
                        Expression = "true",
                        IsActive = false
                    }
                }
            };

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteParallelAsync_MultipleRules_ReturnsAll()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule 1",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Rule 2",
                        Expression = "customer.Name != null",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = await workflow.ExecuteParallelAsync(_parameters, TestContext.Current.CancellationToken);

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteParallelAsync_InactiveWorkflow_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                IsActive = false,
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule",
                        Expression = "true",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = await workflow.ExecuteParallelAsync(_parameters, TestContext.Current.CancellationToken);

            results.Should().BeEmpty();
        }
    }
}
