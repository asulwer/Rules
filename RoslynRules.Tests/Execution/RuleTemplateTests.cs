using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Templates;
using System;
using System.Collections.Generic;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    public class RuleTemplateTests
    {
        private readonly global::RoslynRules.Compiler.ExpressionCompiler _compiler;
        private readonly RuleParameter[] _parameters;

        public RuleTemplateTests()
        {
            _compiler = new ExpressionCompiler();
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 30, Name = "Alice" })
            };
        }

        [Fact]
        public void Instantiate_IdentifierAndValuePlaceholders_CreatesCompiledRule()
        {
            var template = new RuleTemplate
            {
                Description = "Adult check",
                Expression = "{entity}.Age >= {minAge}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["minAge"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["minAge"] = 18
                },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Should().NotBeNull();
            rule.Description.Should().Be("Adult check");
            rule.Expression.Should().Be("customer.Age >= 18");
            rule.IsActive.Should().BeTrue();

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_MissingPlaceholder_ThrowsArgumentException()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.Age >= {minAge}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["minAge"] = PlaceholderKind.Value
                }
            };

            var act = () => template.Instantiate(
                new Dictionary<string, object> { ["entity"] = "customer" },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Missing placeholder values: minAge*");
        }

        [Fact]
        public void Instantiate_StringValuePlaceholder_EscapesQuotes()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Name == {expectedName}",
                Placeholders = { ["expectedName"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["expectedName"] = "Alice\"Bob" },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Expression.Should().Be("customer.Name == \"Alice\\\"Bob\"");
        }

        [Fact]
        public void Instantiate_MultipleUsesOfSamePlaceholder_AllSubstituted()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.Age >= {min} && {entity}.Age <= {max}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["min"] = PlaceholderKind.Value,
                    ["max"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["min"] = 18,
                    ["max"] = 65
                },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Expression.Should().Be(
                "customer.Age >= 18 && customer.Age <= 65");
        }

        [Fact]
        public void Instantiate_WithAction_SubstitutesBoth()
        {
            var template = new RuleTemplate
            {
                Description = "Adult action",
                Expression = "{entity}.Age >= {minAge}",
                Action = "{entity}.Processed = true",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["minAge"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["minAge"] = 18
                },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Action.Should().Be("customer.Processed = true");
        }

        [Fact]
        public void Instantiate_NullExpression_ThrowsInvalidOperationException()
        {
            var template = new RuleTemplate { Expression = null! };

            var act = () => template.Instantiate(
                new Dictionary<string, object>(),
                _compiler,
                _parameters,
                Array.Empty<string>());

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Template Expression is not set*");
        }

        [Fact]
        public void Instantiate_ValuePlaceholder_String()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.Name == {expectedName}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["expectedName"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["expectedName"] = "Alice"
                },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Expression.Should().Be("customer.Name == \"Alice\"");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_ValuePlaceholder_Integer()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.Age > {minAge}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["minAge"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["minAge"] = 25
                },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            rule.Expression.Should().Be("customer.Age > 25");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();  // Age 30 > 25
        }

        [Fact]
        public void ExtractPlaceholders_ReturnsDistinctNames()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.Age >= {min} && {entity}.Age <= {max}"
            };

            var placeholders = template.ExtractPlaceholders();

            placeholders.Should().BeEquivalentTo(new[] { "entity", "min", "max" });
        }

        [Fact]
        public void Instantiate_DifferentValues_CreateDifferentRules()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Age >= {minAge}",
                Placeholders = { ["minAge"] = PlaceholderKind.Value }
            };

            var adultRule = template.Instantiate(
                new Dictionary<string, object> { ["minAge"] = 18 },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            var seniorRule = template.Instantiate(
                new Dictionary<string, object> { ["minAge"] = 65 },
                _compiler,
                _parameters,
                new[] { "RoslynRules.Tests" });

            adultRule.Expression.Should().Be("customer.Age >= 18");
            seniorRule.Expression.Should().Be("customer.Age >= 65");

            adultRule.Execute(_parameters).Success.Should().BeTrue();
            seniorRule.Execute(_parameters).Success.Should().BeFalse();
        }
    }
}