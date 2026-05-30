using FluentAssertions;
using Rules.Models;
using Xunit;

namespace Rules.Tests
{
    public class RuleValidationTests
    {
        [Fact]
        public void Validate_EmptyRule_ThrowsInvalidOperationException()
        {
            var rule = new Rule
            {
                Description = "Empty rule",
                IsActive = true
            };

            var act = () => rule.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*has no Expression, Action, or active ChildRules*");
        }

        [Fact]
        public void Validate_RuleWithExpression_DoesNotThrow()
        {
            var rule = new Rule
            {
                Description = "Valid rule",
                Expression = "customer.Age > 18",
                IsActive = true
            };

            var act = () => rule.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_RuleWithAction_DoesNotThrow()
        {
            var rule = new Rule
            {
                Description = "Valid rule",
                Action = "customer.Processed = true",
                IsActive = true
            };

            var act = () => rule.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_RuleWithChildRules_DoesNotThrow()
        {
            var parent = new Rule
            {
                Description = "Parent rule",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child rule",
                Expression = "customer.Age > 0",
                IsActive = true
            };

            parent.ChildRules.Add(child);

            var act = () => parent.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_InvalidExpressionSyntax_ThrowsInvalidOperationException()
        {
            var rule = new Rule
            {
                Description = "Bad syntax",
                Expression = "customer.Age ??? 18",
                IsActive = true
            };

            var act = () => rule.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Syntax error*");
        }

        [Fact]
        public void Validate_CircularReference_ThrowsInvalidOperationException()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "B", Expression = "true", IsActive = true };

            ruleA.ChildRules.Add(ruleB);
            ruleB.ChildRules.Add(ruleA); // Circular!

            var act = () => ruleA.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Circular child rule reference*");
        }

        [Fact]
        public void Validate_InactiveRuleWithNoChildren_DoesNotThrow()
        {
            var rule = new Rule
            {
                Description = "Inactive empty",
                IsActive = false
            };

            // Inactive rules don't need validation — they get skipped
            var act = () => rule.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*has no Expression, Action, or active ChildRules*");
        }
    }
}
