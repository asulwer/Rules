using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rules.Compiler;
using Rules.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rules.Models
{
    /// <summary>
    /// Represents an individual rule within a workflow.
    /// Each rule evaluates an optional boolean Expression, executes an optional Action,
    /// and can contain child rules that must all succeed for the parent to succeed.
    /// After compilation, rule properties become immutable to ensure thread-safe execution.
    /// Supports both synchronous and asynchronous expressions.
    /// </summary>
    public class Rule
    {
        // Compiled delegates wrapped for fast invocation (no DynamicInvoke).
        [NotMapped] private CompiledDelegate? _compiledExpression;
        [NotMapped] private CompiledDelegate? _compiledAction;
        [NotMapped] private bool _isCompiled;

        /// <summary>
        /// Throws if an attempt is made to mutate a property after compilation.
        /// </summary>
        /// <param name="propertyName">Name of the property being modified.</param>
        private void EnsureNotCompiled(string propertyName)
        {
            if (_isCompiled)
                throw new RuleCompilationException($"Cannot modify {propertyName} after rule has been compiled.");
        }
                
        /// <summary>
        /// Unique identifier for the rule.
        /// </summary>
        [Key] public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Human-readable description of the rule&apos;s purpose.
        /// </summary>
        public string Description 
        { 
            get => _description;
            set { EnsureNotCompiled(nameof(Description)); _description = value; }
        }
        private string _description = string.Empty;

        /// <summary>
        /// When false, the rule is skipped during execution.
        /// </summary>
        public bool IsActive 
        { 
            get => _isActive;
            set { EnsureNotCompiled(nameof(IsActive)); _isActive = value; }
        }
        private bool _isActive = true;

        /// <summary>
        /// C# boolean expression evaluated during execution.
        /// Can contain async code (await) if the expression is marked async.
        /// If empty, the expression is treated as passing.
        /// </summary>
        public string Expression 
        { 
            get => _expression;
            set { EnsureNotCompiled(nameof(Expression)); _expression = value; }
        }
        private string _expression = string.Empty;

        /// <summary>
        /// C# expression executed as an action when the rule succeeds.
        /// Can contain async code (await) if the expression is marked async.
        /// If empty, no action is performed.
        /// </summary>
        public string Action 
        { 
            get => _action;
            set { EnsureNotCompiled(nameof(Action)); _action = value; }
        }
        private string _action = string.Empty;

        /// <summary>
        /// Foreign key referencing the parent workflow.
        /// </summary>
        public Guid? WorkflowId 
        { 
            get => _workflowId;
            set { EnsureNotCompiled(nameof(WorkflowId)); _workflowId = value; }
        }
        private Guid? _workflowId;

        /// <summary>
        /// Navigation property to the parent workflow.
        /// </summary>
        public Workflow? Workflow 
        { 
            get => _workflow;
            set { EnsureNotCompiled(nameof(Workflow)); _workflow = value; }
        }
        private Workflow? _workflow;
        
        /// <summary>
        /// Foreign key referencing the parent rule.
        /// </summary>
        public Guid? ParentRuleId 
        { 
            get => _parentRuleId;
            set { EnsureNotCompiled(nameof(ParentRuleId)); _parentRuleId = value; }
        }
        private Guid? _parentRuleId;

        /// <summary>
        /// Navigation property to the parent rule.
        /// </summary>
        public Rule? ParentRule 
        { 
            get => _parentRule;
            set { EnsureNotCompiled(nameof(ParentRule)); _parentRule = value; }
        }
        private Rule? _parentRule;

        /// <summary>
        /// Child rules that must all succeed for this parent rule to succeed.
        /// Evaluated bottom-up before the parent&apos;s Expression and Action.
        /// </summary>
        public IList<Rule> ChildRules 
        { 
            get => _childRules;
            set { EnsureNotCompiled(nameof(ChildRules)); _childRules = value; }
        }
        private IList<Rule> _childRules = new List<Rule>();

        // ==================== LOGGING ====================

        /// <summary>
        /// Optional logger for observing rule execution.
        /// Set this to any ILogger implementation (Serilog, NLog, etc.).
        /// </summary>
        [NotMapped] public ILogger? Logger { get; set; }

        /// <summary>
        /// Logs rule execution via Microsoft.Extensions.Logging if a logger is set.
        /// </summary>
        private void LogExecuted(RuleExecutedEvent @event)
        {
            Logger?.LogRuleExecuted(@event);
        }

        // ==================== VALIDATION ====================

        /// <summary>
        /// Validates the rule structure and expression syntax before compilation.
        /// Checks that the rule has valid content, that expressions are syntactically
        /// correct C#, and that child rules are valid.
        /// </summary>
        /// <exception cref="RuleValidationException">Thrown when structural or syntax validation fails.</exception>
        public void Validate()
        {
            // 1. Structural validation: a rule must have something to do.
            if (string.IsNullOrEmpty(Expression) && string.IsNullOrEmpty(Action) && !ChildRules.Any(r => r.IsActive))
            {
                throw new RuleValidationException(
                    $"Rule &apos;{Description}&apos; (Id: {Id}) has no Expression, Action, or active ChildRules.");
            }

            // 2. Validate no circular references FIRST (before recursing into children).
            ValidateNoCircularReferences();

            // 3. Validate Expression syntax if present.
            if (!string.IsNullOrEmpty(Expression))
            {
                ValidateExpressionSyntax(Expression, mustReturnBool: true);
            }

            // 4. Validate Action syntax if present.
            if (!string.IsNullOrEmpty(Action))
            {
                ValidateExpressionSyntax(Action, mustReturnBool: false);
            }

            // 5. Validate child rules recursively (safe now that circular refs are checked).
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Validate();
            }
        }

        /// <summary>
        /// Validates that a C# expression string is syntactically valid.
        /// Uses Roslyn to parse the expression without compiling it.
        /// </summary>
        /// <param name="expression">The expression string to validate.</param>
        /// <param name="mustReturnBool">If true, expression should logically return a boolean (not enforced at syntax level).</param>
        private static void ValidateExpressionSyntax(string expression, bool mustReturnBool)
        {
            // Wrap the expression in a minimal method body so Roslyn can parse it.
            var code = $@"class X {{ void M() {{ {(mustReturnBool ? "var __result = " : "")}{expression}; }} }}";

            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();

            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
            {
                throw new SyntaxErrorException(expression, errors.Select(e => e.GetMessage()).ToArray());
            }
        }

        // ==================== COMPILATION ====================

        /// <summary>
        /// Compiles the Expression and Action into delegates using the supplied compiler.
        /// Validates against circular child references before compilation.
        /// After compilation, all properties become immutable to ensure thread safety.
        /// Recursively compiles all active child rules.
        /// </summary>
        /// <param name="compiler">The expression compiler instance.</param>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        public void Compile(ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null)
        {
            // Validate no circular references before compiling.
            ValidateNoCircularReferences();

            if (!string.IsNullOrEmpty(Expression))
            {
                var isAsync = IsAsyncExpression(Expression);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(bool), parameters)
                    : BuildDelegateType(typeof(bool), parameters);
                var rawDelegate = CompileDelegate(compiler, Expression, delegateType, parameters, additionalNamespaces);
                _compiledExpression = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            if (!string.IsNullOrEmpty(Action))
            {
                var isAsync = IsAsyncExpression(Action);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(object), parameters)
                    : BuildDelegateType(typeof(object), parameters);
                var rawDelegate = CompileDelegate(compiler, Action, delegateType, parameters, additionalNamespaces);
                _compiledAction = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            // Compile children recursively
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Compile(compiler, parameters, additionalNamespaces);
            }

            _isCompiled = true; // Lock properties after compilation
        }

        /// <summary>
        /// Checks if an expression contains async/await keywords.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <returns>True if the expression contains await.</returns>
        private static bool IsAsyncExpression(string expression)
        {
            return expression.Contains("await");
        }

        /// <summary>
        /// Builds a Func or Action delegate type matching the parameter signature.
        /// Supports exactly one input parameter and one return type (or void).
        /// For multiple inputs/outputs, wrap them in a struct or class.
        /// </summary>
        /// <example>
        /// Single input, single return: Func<Customer, bool>
        /// Single input, void return: Action<Customer>
        /// Single input, composite return: Func<Customer, Returned>
        /// </example>
        private static Type BuildDelegateType(Type returnType, RuleParameter[] parameters)
        {
            if (parameters.Length > 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            var paramType = parameters[0].Type;
            
            if (returnType == typeof(void))
            {
                return typeof(Action<>).MakeGenericType(paramType);
            }
            else
            {
                return typeof(Func<,>).MakeGenericType(paramType, returnType);
            }
        }

        /// <summary>
        /// Builds an async delegate type returning Task<TReturn> or Task.
        /// </summary>
        /// <param name="returnType">The result type (not Task, the inner type).</param>
        /// <param name="parameters">Parameter definitions.</param>
        /// <returns>Func<TParam, Task<TReturn>> or Func<TParam, Task>.</returns>
        private static Type BuildAsyncDelegateType(Type returnType, RuleParameter[] parameters)
        {
            if (parameters.Length > 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            var paramType = parameters[0].Type;
            
            if (returnType == typeof(void))
            {
                return typeof(Func<,>).MakeGenericType(paramType, typeof(Task));
            }
            else
            {
                var taskType = typeof(Task<>).MakeGenericType(returnType);
                return typeof(Func<,>).MakeGenericType(paramType, taskType);
            }
        }

        /// <summary>
        /// Invokes the compiler via reflection to create a typed delegate.
        /// </summary>
        private static Delegate CompileDelegate(ExpressionCompiler compiler, string expression, Type delegateType, RuleParameter[] parameters, string[]? additionalNamespaces)
        {
            var paramNames = parameters.Select(p => p.Name).ToArray();
            var method = compiler.GetType().GetMethod("Compile")!.MakeGenericMethod(delegateType);
            var result = method.Invoke(compiler, new object[] { expression, paramNames, additionalNamespaces ?? Array.Empty<string>() });
            if (result is not Delegate delegateResult)
                throw new RuleCompilationException("Compiler did not return a valid delegate.");
            return delegateResult;
        }

        /// <summary>
        /// Validates that no circular references exist in the child rule tree.
        /// A rule cannot reference itself or an ancestor as a child.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
        private void ValidateNoCircularReferences()
        {
            var visited = new HashSet<Guid>();
            ValidateNoCircularReferences(this, visited);
        }

        /// <summary>
        /// Recursive helper that walks the child rule tree looking for loops.
        /// </summary>
        /// <param name="current">The current rule being checked.</param>
        /// <param name="visited">Set of rule IDs in the current path.</param>
        private static void ValidateNoCircularReferences(Rule current, HashSet<Guid> visited)
        {
            if (!visited.Add(current.Id))
            {
                throw new CircularReferenceException(current.Id, current.Description);
            }

            foreach (var child in current.ChildRules.Where(r => r.IsActive))
            {
                ValidateNoCircularReferences(child, visited);
            }

            visited.Remove(current.Id); // Allow same rule in different branches of the tree
        }

        // ==================== EXECUTION ====================

        /// <summary>
        /// Executes the rule bottom-up: children first, then Expression, then Action.
        /// Returns a RuleResult indicating success or failure.
        /// For async expressions, use ExecuteAsync.
        /// Fires logging events if Logger is set.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Result of the rule evaluation.</returns>
        public RuleResult Execute(params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = ExecuteCore(parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false);
            }

            sw.Stop();
            LogExecuted(new RuleExecutedEvent
            {
                RuleId = Id,
                RuleDescription = Description,
                IsActive = IsActive,
                Success = result.Success,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
                Exception = exception
            });

            return result;
        }

        /// <summary>
        /// Core execution logic without logging overhead.
        /// </summary>
        private RuleResult ExecuteCore(RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            var paramValue = parameters[0].Value;

            // Bottom-up: evaluate all active children first
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                var childResult = child.Execute(parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                return new RuleResult(false, Id, Description, IsActive, childResults: childResults);
            }

            // Evaluate compiled Expression if present
            if (_compiledExpression != null)
            {
                var exprResult = _compiledExpression.Invoke(paramValue);
                if (!(bool)exprResult!)
                return new RuleResult(false, Id, Description, IsActive, childResults: childResults);
            }

            // Execute compiled Action if present
            if (_compiledAction != null)
            {
                var actionResult = _compiledAction.Invoke(paramValue);
                return new RuleResult(true, Id, Description, IsActive, actionResult, childResults: childResults);
            }

                return new RuleResult(true, Id, Description, IsActive, childResults: childResults);
        }

        /// <summary>
        /// Executes the rule asynchronously. Supports async expressions containing await.
        /// Children are executed sequentially (bottom-up dependency), but their internal
        /// async operations are properly awaited.
        /// Fires logging events if Logger is set.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Task containing the rule evaluation result.</returns>
        public async Task<RuleResult> ExecuteAsync(params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = await ExecuteCoreAsync(parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false);
            }

            sw.Stop();
            LogExecuted(new RuleExecutedEvent
            {
                RuleId = Id,
                RuleDescription = Description,
                IsActive = IsActive,
                Success = result.Success,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
                Exception = exception
            });

            return result;
        }

        /// <summary>
        /// Core async execution logic without logging overhead.
        /// </summary>
        private async Task<RuleResult> ExecuteCoreAsync(RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            var paramValue = parameters[0].Value;

            // Bottom-up: evaluate all active children first (async)
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                var childResult = await child.ExecuteAsync(parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                return new RuleResult(false, Id, Description, IsActive, childResults: childResults);
            }

            // Evaluate compiled Expression if present
            if (_compiledExpression != null)
            {
                object? exprResult;
                if (_compiledExpression is CompiledAsyncFunc<object?, object?> asyncExpr)
                {
                    exprResult = await asyncExpr.InvokeAsync(paramValue);
                }
                else
                {
                    exprResult = _compiledExpression.Invoke(paramValue);
                }
                
                if (!(bool)exprResult!)
                return new RuleResult(false, Id, Description, IsActive, childResults: childResults);
            }

            // Execute compiled Action if present
            if (_compiledAction != null)
            {
                object? actionResult;
                if (_compiledAction is CompiledAsyncAction<object?> asyncAction)
                {
                    actionResult = await asyncAction.InvokeAsync(paramValue);
                }
                else
                {
                    actionResult = _compiledAction.Invoke(paramValue);
                }
                return new RuleResult(true, Id, Description, IsActive, actionResult, childResults: childResults);
            }

                return new RuleResult(true, Id, Description, IsActive, childResults: childResults);
        }
    }
}
