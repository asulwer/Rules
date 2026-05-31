using FluentAssertions;
using Microsoft.Extensions.Logging;
using Rules.Compiler;
using Rules.Exceptions;
using Rules.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rules.Tests
{
    /// <summary>
    /// Tests for async expressions, logging, and workflow edge cases.
    /// </summary>
    public class AsyncAndLoggingTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        public AsyncAndLoggingTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "Rules.Tests", "System", "System.Threading.Tasks" };
        }

        

        

        

        

        

        [Fact]
        public void Execute_WithLogger_FiresEvent()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Logged rule",
                Expression = "customer.Age >= 18",
                IsActive = true,
                Logger = logger
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Logged rule");
            logger.EventIds[0].Id.Should().Be(1002); // RulePassed
        }

        [Fact]
        public void Execute_FailingRule_LogsFailure()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Failing rule",
                Expression = "customer.Age >= 100",
                IsActive = true,
                Logger = logger
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Failing rule");
            logger.EventIds[0].Id.Should().Be(1003); // RuleFailed
        }

        [Fact]
        public void Execute_RuntimeError_LogsException()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Error rule",
                Expression = "1 / (customer.Age - 25) == 1",
                IsActive = true,
                Logger = logger
            };

            var compiler = new ExpressionCompiler();
            rule.Compile(compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Error rule");
            logger.EventIds[0].Id.Should().Be(1004); // RuleError
        }

        [Fact]
        public void Validate_DuplicateRuleIds_ThrowsDuplicateRuleIdException()
        {
            var workflow = new Workflow();
            var rule1 = new Rule { Description = "Rule 1", Expression = "true" };
            var rule2 = new Rule { Description = "Rule 2", Expression = "true" };
            
            // Force same ID
            typeof(Rule).GetProperty("Id")?.SetValue(rule2, rule1.Id);
            
            workflow.Rules.Add(rule1);
            workflow.Rules.Add(rule2);

            var act = () => workflow.Validate();
            act.Should().Throw<DuplicateRuleIdException>();
        }

        [Fact]
        public void Validate_EmptyWorkflow_ThrowsWorkflowException()
        {
            var workflow = new Workflow { Description = "Empty" };

            var act = () => workflow.Validate();
            act.Should().Throw<WorkflowException>();
        }
    }

    /// <summary>
    /// Test logger that captures RuleExecutedEvent for verification.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<RuleExecutedEvent> Events { get; } = new List<RuleExecutedEvent>();
        public List<string> LogMessages { get; } = new List<string>();
        public List<EventId> EventIds { get; } = new List<EventId>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LogMessages.Add(message);
            EventIds.Add(eventId);
            
            // Extract RuleExecutedEvent if present in the state
            if (state is RuleExecutedEvent evt)
                Events.Add(evt);
        }
    }
}


