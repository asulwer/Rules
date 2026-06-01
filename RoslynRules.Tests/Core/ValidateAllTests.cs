using RoslynRules.Batch;
using RoslynRules.Models;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for ValidateAll() — non-throwing validation that returns all errors.
    /// </summary>
    public class ValidateAllTests
    {
        [Fact]
        public void Workflow_ValidateAll_EmptyWorkflow_ReturnsNoActiveRulesError()
        {
            var wf = new Workflow();
            var errors = wf.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.NoActiveRules, errors[0].ErrorType);
        }

        [Fact]
        public void Workflow_ValidateAll_ValidWorkflow_ReturnsEmpty()
        {
            var wf = new Workflow();
            wf.Rules.Add(new Rule { Expression = "true", Description = "Valid" });

            var errors = wf.ValidateAll();

            Assert.Empty(errors);
        }

        [Fact]
        public void Workflow_ValidateAll_InvalidRule_ReturnsSyntaxError()
        {
            var wf = new Workflow();
            wf.Rules.Add(new Rule { Expression = "invalid syntax here @#$", Description = "Broken" });

            var errors = wf.ValidateAll();

            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
        }

        [Fact]
        public void Workflow_ValidateAll_MultipleErrors_ReturnsAll()
        {
            var wf = new Workflow();
            var rule1 = new Rule { Expression = "true" };
            var rule2 = new Rule { Expression = "true" }; // Will have duplicate ID
            var rule3 = new Rule { Expression = "broken syntax @#$" };
            wf.Rules.Add(rule1);
            wf.Rules.Add(rule2);
            wf.Rules.Add(rule3);
            typeof(Rule).GetProperty("Id")!.SetValue(rule2, rule1.Id);

            var errors = wf.ValidateAll();

            Assert.True(errors.Length >= 2, $"Expected 2+ errors but got {errors.Length}");
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.DuplicateRuleId);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.DuplicateRuleId);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
        }

        [Fact]
        public void Rule_ValidateAll_EmptyRule_ReturnsEmptyRuleError()
        {
            var rule = new Rule { Description = "Empty" };

            var errors = rule.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.EmptyRule, errors[0].ErrorType);
        }

        [Fact]
        public void Rule_ValidateAll_InvalidExpression_ReturnsSyntaxError()
        {
            var rule = new Rule { Expression = "broken @#$", Description = "Bad" };

            var errors = rule.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.SyntaxError, errors[0].ErrorType);
        }

        [Fact]
        public void RuleBatch_ValidateAll_EmptyBatch_ReturnsNoActiveRulesError()
        {
            var batch = new RuleBatch();

            var errors = batch.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.NoActiveRules, errors[0].ErrorType);
        }

        [Fact]
        public void RuleBatch_ValidateAll_DuplicateIds_ReturnsDuplicateRuleIdError()
        {
            var batch = new RuleBatch();
            var rule1 = new Rule { Expression = "true" };
            var rule2 = new Rule { Expression = "false" };
            batch.AddRule(rule1);
            batch.AddRule(rule2);
            // Force duplicate Id
            typeof(Rule).GetProperty("Id")!.SetValue(rule2, rule1.Id);

            var errors = batch.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.DuplicateRuleId, errors[0].ErrorType);
        }

        [Fact]
        public void IRuleEngine_ValidateAll_ImplementedByWorkflow()
        {
            RoslynRules.Abstractions.IRuleEngine engine = new Workflow();
            engine = new Workflow();
            ((Workflow)engine).Rules.Add(new Rule { Expression = "true" });

            var errors = engine.ValidateAll();

            Assert.Empty(errors);
        }

        [Fact]
        public void IRuleEngine_ValidateAll_ImplementedByRuleBatch()
        {
            RoslynRules.Abstractions.IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule { Expression = "true" });

            var errors = engine.ValidateAll();

            Assert.Empty(errors);
        }
    }
}