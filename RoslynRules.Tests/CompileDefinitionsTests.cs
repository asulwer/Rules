using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests
{
    public class CompileDefinitionsTests
    {
        [Fact]
        public void CompileDefinitions_WithoutDummyInstance_CompilesSuccessfully()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var workflow = new Workflow { Rules = new List<Rule> { rule } };

            // Compile with just type info — no dummy Customer needed
            workflow.CompileDefinitions(new[]
            {
                new RuleParameterDefinition("customer", typeof(TestCustomer))
            });

            // Execute with real instance
            var customer = new TestCustomer { Age = 25, Name = "Alice" };
            var results = workflow.Execute(new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), customer)
            }).ToList();

            results.Should().HaveCount(1);
            results[0].Success.Should().BeTrue();
        }

        [Fact]
        public void CompileDefinitions_WithAction_CompilesSuccessfully()
        {
            var rule = new Rule
            {
                Description = "Set processed",
                Expression = "customer.Age >= 18",
                Action = "customer.Processed = true",
                IsActive = true
            };

            var workflow = new Workflow { Rules = new List<Rule> { rule } };

            workflow.CompileDefinitions(new[]
            {
                new RuleParameterDefinition("customer", typeof(TestCustomer))
            });

            var customer = new TestCustomer { Age = 25, Name = "Alice" };
            var results = workflow.Execute(new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), customer)
            }).ToList();

            results[0].Success.Should().BeTrue();
            customer.Processed.Should().BeTrue();
        }

        [Fact]
        public void CompileDefinitions_MultipleRules_CompilesOnceExecutesMany()
        {
            var rule = new Rule
            {
                Description = "Name check",
                Expression = "customer.Name.Contains(\"A\")",
                IsActive = true
            };

            var workflow = new Workflow { Rules = new List<Rule> { rule } };

            workflow.CompileDefinitions(new[]
            {
                new RuleParameterDefinition("customer", typeof(TestCustomer))
            });

            // Execute multiple times with different instances
            var customers = new[]
            {
                new TestCustomer { Name = "Alice" },
                new TestCustomer { Name = "Bob" },
                new TestCustomer { Name = "Anna" }
            };

            foreach (var customer in customers)
            {
                var results = workflow.Execute(new[]
                {
                    new RuleParameter("customer", typeof(TestCustomer), customer)
                }).ToList();

                var expected = customer.Name.Contains("A");
                results[0].Success.Should().Be(expected);
            }
        }
    }
}
