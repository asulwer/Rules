using FluentAssertions;
using Rules.Compiler;
using Rules.Models;
using Xunit;

namespace Rules.Tests
{
    public class RuleExecutionTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        public RuleExecutionTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "Rules.Tests" };
        }

        [Fact]
        public void Execute_SimpleExpression_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_SimpleExpression_ReturnsFalse()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 30",
                IsActive = true
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_InactiveRule_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Inactive",
                Expression = "customer.Age >= 100",
                IsActive = false
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            // Inactive rules return true (skip)
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_Action_ModifiesParameter()
        {
            var customer = new TestCustomer { Age = 25, Name = "Alice" };
            var parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), customer)
            };

            var rule = new Rule
            {
                Description = "Mark adult",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
            customer.IsAdult.Should().BeTrue();
        }

        [Fact]
        public void Execute_ParentWithChild_ChildFails_ParentFails()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child fails",
                Expression = "customer.Age > 100",
                IsActive = true
            };

            parent.ChildRules.Add(child);

            var compiler = new ExpressionCompiler();
            parent.Compile(compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_ParentWithChild_ChildPasses_ParentPasses()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child passes",
                Expression = "customer.Age > 0",
                IsActive = true
            };

            parent.ChildRules.Add(child);

            var compiler = new ExpressionCompiler();
            parent.Compile(compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Compile_MultipleParameters_ThrowsNotSupportedException()
        {
            var rule = new Rule
            {
                Description = "Multi-param",
                Expression = "true",
                IsActive = true
            };

            var parameters = new[]
            {
                new RuleParameter("a", typeof(int), 1),
                new RuleParameter("b", typeof(int), 2)
            };

            var compiler = new ExpressionCompiler();
            var act = () => rule.Compile(compiler, parameters, _namespaces);
            
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*Rules support exactly one input parameter*");
        }

        [Fact]
        public void Execute_NotCompiled_ThrowsInvalidOperationException()
        {
            var rule = new Rule
            {
                Description = "Not compiled",
                Expression = "customer.Age > 18",
                IsActive = true
            };

            var act = () => rule.Execute(_parameters);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*must be compiled before execution*");
        }
    }
}
