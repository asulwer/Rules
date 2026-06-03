using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Execution;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace RoslynRules.Tests.AotCompatibility
{
    /// <summary>
    /// Validates AOT-safe APIs that don&apos;t require JIT compilation.
    /// These tests confirm RoslynRules can be referenced from AOT apps
    /// without linker errors for model/validation scenarios.
    /// </summary>
    public class AotCompatibilityTests
    {
        [Fact]
        public void Workflow_ModelCreation()
        {
            var workflow = new Workflow
            {
                Description = "Test Workflow",
                Rules =
                {
                    new Rule
                    {
                        Description = "Age check",
                        Expression = "x >= 18",
                        IsActive = true,
                        Priority = 10
                    },
                    new Rule
                    {
                        Description = "Name check",
                        Expression = "!string.IsNullOrEmpty(name)",
                        IsActive = true,
                        Priority = 5
                    }
                }
            };

            workflow.Rules.Should().HaveCount(2);
            workflow.Rules[0].Priority.Should().Be(10);
        }

        [Fact]
        public void Workflow_Validate_WithoutCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true", IsActive = true },
                    new Rule { Description = "R2", Expression = "false", IsActive = true }
                }
            };

            // Validation does not require compilation
            workflow.Validate();
        }

        [Fact]
        public void Workflow_ValidateDependencyChain()
        {
            var depId = Guid.NewGuid();
            var ruleId = Guid.NewGuid();
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Id = depId, Description = "Dep", Expression = "true", IsActive = true },
                    new Rule { Id = ruleId, Description = "Dependent", Expression = "true", IsActive = true, DependsOnRuleId = depId }
                }
            };

            workflow.Validate();
        }

        [Fact]
        public void Workflow_DetectCycle()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Id = id1, Description = "A", Expression = "true", IsActive = true, DependsOnRuleId = id2 },
                    new Rule { Id = id2, Description = "B", Expression = "true", IsActive = true, DependsOnRuleId = id1 }
                }
            };

            var act = () => workflow.Validate();
            act.Should().Throw<CircularReferenceException>();
        }

        [Fact]
        public void RuleResult_CreateAndAccess()
        {
            var result = new RuleResult(
                Success: true,
                RuleId: Guid.NewGuid(),
                RuleDescription: "Test"
            );

            result.Success.Should().BeTrue();
            result.RuleDescription.Should().Be("Test");
        }

        [Fact]
        public void RuleContext_StoreAndGet()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            var result = new RuleResult(Success: true, RuleId: ruleId, Value: 123);

            context.StoreResult(ruleId, result);
            var retrieved = context.GetResult(ruleId);

            retrieved.Should().NotBeNull();
            retrieved!.Value.Value.Should().Be(123);
        }

        [Fact]
        public void Workflow_GetExecutionOrder_WithoutCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Low", Expression = "true", IsActive = true, Priority = 0 },
                    new Rule { Description = "High", Expression = "true", IsActive = true, Priority = 10 }
                }
            };

            var method = typeof(Workflow).GetMethod("GetExecutionOrder",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull();

            var ordered = method!.Invoke(workflow, null) as List<Rule>;
            ordered.Should().NotBeNull().And.HaveCount(2);
            ordered![0].Description.Should().Be("High");
        }

        [Fact]
        public void Compile_HasRequiresUnreferencedCodeAttribute()
        {
            var compilerType = typeof(RoslynRules.Compiler.ExpressionCompiler);
            var compileMethod = compilerType.GetMethods()
                .FirstOrDefault(m => m.Name == "Compile");

            compileMethod.Should().NotBeNull();

            var attributes = compileMethod!.GetCustomAttributes(
                typeof(System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute), false);
            attributes.Should().NotBeEmpty("Compile should have RequiresUnreferencedCode attribute for AOT safety");
        }
    }
}
