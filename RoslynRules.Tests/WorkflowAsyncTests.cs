using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace RoslynRules.Tests
{
    /// <summary>
    /// Tests for Workflow.ExecuteAsync and validation edge cases.
    /// </summary>
    public class WorkflowAsyncTests
    {
        private readonly RuleParameter[] _parameters;

        public WorkflowAsyncTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public async Task ExecuteAsync_SingleRule_ReturnsResult()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Test",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = new List<RuleResult>();
            await foreach (var result in workflow.ExecuteAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results.Should().HaveCount(1);
            results[0].Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_MultipleRules_ReturnsAll()
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

            var results = new List<RuleResult>();
            await foreach (var result in workflow.ExecuteAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecuteAsync_WithInactiveRule_FiltersOutInactive()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Active",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Inactive",
                        Expression = "customer.Age > 100",
                        IsActive = false
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = new List<RuleResult>();
            await foreach (var result in workflow.ExecuteAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results.Should().HaveCount(1); // Only active rules are iterated
            results[0].Success.Should().BeTrue();
            results[0].RuleDescription.Should().Be("Active");
        }

        [Fact]
        public void Execute_InactiveWorkflow_YieldsNoResults()
        {
            var workflow = new Workflow
            {
                Description = "Inactive",
                IsActive = false,
                Rules =
                {
                    new Rule { Description = "Rule", Expression = "true", IsActive = true }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });
            var results = workflow.Execute(_parameters);

            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EmptyWorkflow_ThrowsWorkflowException()
        {
            var workflow = new Workflow
            {
                Description = "Empty"
            };

            var act = () => workflow.Validate();
            act.Should().Throw<WorkflowException>();
        }

        [Fact]
        public void Execute_NotCompiledException_Throws()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Rule", Expression = "true", IsActive = true }
                }
            };
            // Don't compile - force iteration to trigger the exception
            var act = () => workflow.Execute(_parameters).ToList();
            act.Should().Throw<NotCompiledException>();
        }
    }
}
