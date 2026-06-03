using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using RoslynRules.Execution;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynRules.Models
{
    /// <summary>
    /// Represents an individual rule within a workflow.
    /// Each rule evaluates an optional boolean Expression, executes an optional Action,
    /// and can contain child rules that must all succeed for the parent to succeed.
    /// After compilation, rule properties become immutable to ensure thread-safe execution.
    /// Supports both synchronous and asynchronous expressions.
    /// </summary>
    public sealed class Rule
    {
        // Compiled delegates wrapped for fast invocation (no DynamicInvoke).
        [NotMapped] private CompiledDelegate? _compiledExpression;
        [NotMapped] private CompiledDelegate? _compiledAction;
        [NotMapped] private bool _isCompiled;

        // Stores the compile-time parameter schema for validation at execution.
        [NotMapped] private Type? _compiledParameterType;
        [NotMapped] private string? _compiledParameterName;

        // Result cache for memoization.
        [NotMapped] private readonly Execution.RuleCache _resultCache = new();

        /// <summary>
        /// EF Core requires a parameterless constructor.
        /// Initializes a new rule with default values.
        /// </summary>
        public Rule()
        {
        }

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
        [Key] [JsonInclude] public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Internal constructor for testing purposes. Allows explicit ID assignment
        /// without reflection, which is required for AOT/trimming compatibility.
        /// </summary>
        /// <param name="id">Explicit rule ID.</param>
        internal Rule(Guid id)
        {
            Id = id;
        }

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
        /// Execution priority. Higher values execute first.
        /// Default is 0. Negative values execute after default priority rules.
        /// </summary>
        public int Priority
        {
            get => _priority;
            set { EnsureNotCompiled(nameof(Priority)); _priority = value; }
        }
        private int _priority = 0;

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
        /// Maximum time allowed for rule execution. Null means no timeout.
        /// Applies to both Expression and Action execution.
        /// </summary>
        public TimeSpan? Timeout
        {
            get => _timeout;
            set { EnsureNotCompiled(nameof(Timeout)); _timeout = value; }
        }
        private TimeSpan? _timeout;

        /// <summary>
        /// Duration to cache rule evaluation results. Null means no caching.
        /// When set, repeated executions with identical parameters return cached results.
        /// Child rules are evaluated independently; only this rule's final result is cached.
        /// </summary>
        public TimeSpan? CacheDuration
        {
            get => _cacheDuration;
            set { EnsureNotCompiled(nameof(CacheDuration)); _cacheDuration = value; }
        }
        private TimeSpan? _cacheDuration;

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

        /// <summary>
        /// Foreign key referencing another rule that this rule depends on.
        /// When set, the dependency rule&apos;s result is made available during execution.
        /// The dependent rule executes after its dependency.
        /// </summary>
        public Guid? DependsOnRuleId
        {
            get => _dependsOnRuleId;
            set { EnsureNotCompiled(nameof(DependsOnRuleId)); _dependsOnRuleId = value; }
        }
        private Guid? _dependsOnRuleId;

        /// <summary>
        /// Navigation property to the rule this rule depends on.
        /// </summary>
        [NotMapped]
        public Rule? DependsOnRule
        {
            get => _dependsOnRule;
            set { EnsureNotCompiled(nameof(DependsOnRule)); _dependsOnRule = value; }
        }
        private Rule? _dependsOnRule;

        // ==================== LOGGING & EVENTS ====================

        /// <summary>
        /// Optional logger for observing rule execution.
        /// Set this to any ILogger implementation (Serilog, NLog, etc.).
        /// </summary>
        [NotMapped] public ILogger? Logger { get; set; }

        /// <summary>
        /// Fired before a rule executes. Set Cancel = true to skip execution.
        /// </summary>
        public event EventHandler<RuleExecutingEventArgs>? OnRuleExecuting;

        /// <summary>
        /// Fired after a rule completes execution.
        /// </summary>
        public event EventHandler<RuleExecutedEventArgs>? OnRuleExecuted;

        /// <summary>
        /// Logs rule execution via Microsoft.Extensions.Logging if a logger is set.
        /// </summary>
        private void LogExecuted(RuleExecutedEvent @event)
        {
            Logger?.LogRuleExecuted(@event);
        }

        /// <summary>
        /// Clears the cached result for this rule, forcing the next evaluation to re-execute.
        /// Thread-safe.
        /// </summary>
        public void ClearCache() => _resultCache.Clear();

        // ==================== VALIDATION ====================

        /// <summary>
        /// Validates the rule structure and expression syntax before compilation.
        /// Checks that the rule has valid content, that expressions are syntactically
        /// correct C#, and that child rules are valid.
        /// <br/><br/>
        /// <b>Note:</b> This method only validates <i>syntax</i>, not semantics.
        /// Undefined variables or missing types will pass syntax validation but fail
        /// at compile time. Use <see cref="ValidateSemantics">ValidateSemantics</see>
        /// for full semantic validation (requires a compiler and parameters).
        /// </summary>
        /// <exception cref="RuleValidationException">Thrown when structural or syntax validation fails.</exception>
        public void Validate(IEnumerable<Guid>? availableRuleIds = null)
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

            // 5. Validate dependency references if available rule IDs are provided.
            if (DependsOnRuleId.HasValue && availableRuleIds != null)
            {
                var available = availableRuleIds as ICollection<Guid> ?? availableRuleIds.ToList();
                if (!available.Contains(DependsOnRuleId.Value))
                {
                    throw new RuleValidationException(
                        $"Rule '{Description}' (Id: {Id}) depends on rule {DependsOnRuleId.Value} which does not exist or is inactive in the current workflow.");
                }
            }

            // 6. Validate child rules recursively (safe now that circular refs are checked).
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Validate(availableRuleIds);
            }
        }

        /// <summary>
        /// Validates the rule and all child rules, returning all errors found.
        /// Does not throw — returns an empty array if validation succeeds.
        /// </summary>
        /// <returns>Array of validation errors. Empty if valid.</returns>
        public ValidationError[] ValidateAll(IEnumerable<Guid>? availableRuleIds = null)
        {
            var errors = new List<ValidationError>();

            // 1. Structural validation: a rule must have something to do.
            if (string.IsNullOrEmpty(Expression) && string.IsNullOrEmpty(Action) && !ChildRules.Any(r => r.IsActive))
            {
                errors.Add(new ValidationError(
                    $"Rule &apos;{Description}&apos; (Id: {Id}) has no Expression, Action, or active ChildRules.",
                    ValidationErrorType.EmptyRule, Id, Description));
                return errors.ToArray();
            }

            // 2. Validate no circular references FIRST.
            try
            {
                ValidateNoCircularReferences();
            }
            catch (CircularReferenceException ex)
            {
                errors.Add(new ValidationError(
                    ex.Message, ValidationErrorType.CircularReference, ex.RuleId, ex.RuleDescription));
            }

            // 3. Validate Expression syntax if present.
            if (!string.IsNullOrEmpty(Expression))
            {
                ValidateExpressionSyntax(Expression, mustReturnBool: true, errors);
            }

            // 4. Validate Action syntax if present.
            if (!string.IsNullOrEmpty(Action))
            {
                ValidateExpressionSyntax(Action, mustReturnBool: false, errors);
            }

            // 5. Validate dependency references if available rule IDs are provided.
            if (DependsOnRuleId.HasValue && availableRuleIds != null)
            {
                var available = availableRuleIds as ICollection<Guid> ?? availableRuleIds.ToList();
                if (!available.Contains(DependsOnRuleId.Value))
                {
                    errors.Add(new ValidationError(
                        $"Rule '{Description}' (Id: {Id}) depends on rule {DependsOnRuleId.Value} which does not exist or is inactive in the current workflow.",
                        ValidationErrorType.MissingDependency, Id, Description));
                }
            }

            // 6. Validate child rules recursively.
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                errors.AddRange(child.ValidateAll(availableRuleIds));
            }

            return errors.ToArray();
        }

        /// <summary>
        /// Validates that a C# expression string is syntactically valid.
        /// Uses Roslyn to parse the expression without compiling it.
        /// Throws on syntax errors.
        /// </summary>
        private static void ValidateExpressionSyntax(string expression, bool mustReturnBool)
        {
            var code = $@"class X {{ void M() {{ {(mustReturnBool ? "var __result = " : "")}{expression}; }} }}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
            {
                throw new SyntaxErrorException(expression, errors.Select(e => e.GetMessage()).ToArray());
            }
        }

        /// <summary>
        /// Validates that a C# expression string is syntactically valid.
        /// Uses Roslyn to parse the expression without compiling it.
        /// Errors are collected into the provided list instead of thrown.
        /// </summary>
        private static void ValidateExpressionSyntax(string expression, bool mustReturnBool, List<ValidationError> errors)
        {
            var code = $@"class X {{ void M() {{ {(mustReturnBool ? "var __result = " : "")}{expression}; }} }}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();
            var syntaxErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (syntaxErrors.Any())
            {
                errors.Add(new ValidationError(
                    $"Syntax error in expression: {string.Join("; ", syntaxErrors.Select(e => e.GetMessage()))}",
                    ValidationErrorType.SyntaxError));
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
        /// <param name="referenceProvider">Optional custom assembly reference provider for sandboxing.</param>
        public void Compile(ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            // Validate parameter constraints.
            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            // Store parameter schema for execution-time validation.
            _compiledParameterType = parameters[0].Type;
            _compiledParameterName = parameters[0].Name;

            // Validate no circular references before compiling.
            ValidateNoCircularReferences();

            if (!string.IsNullOrEmpty(Expression))
            {
                var isAsync = IsAsyncExpression(Expression);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(bool), parameters)
                    : BuildDelegateType(typeof(bool), parameters);
                var rawDelegate = CompileDelegate(compiler, Expression, delegateType, parameters, additionalNamespaces, referenceProvider);
                _compiledExpression = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            if (!string.IsNullOrEmpty(Action))
            {
                var isAsync = IsAsyncExpression(Action);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(void), parameters)
                    : BuildDelegateType(typeof(void), parameters);
                var rawDelegate = CompileDelegate(compiler, Action, delegateType, parameters, additionalNamespaces, referenceProvider);
                _compiledAction = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            // Compile children recursively
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Compile(compiler, parameters, additionalNamespaces, referenceProvider);
            }

            _isCompiled = true; // Lock properties after compilation
        }

        /// <summary>
        /// Checks if an expression contains async/await keywords by parsing the syntax tree.
        /// This avoids false positives from variable names like "awaiting" or string literals.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <returns>True if the expression contains an await expression node.</returns>
        private static bool IsAsyncExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            try
            {
                var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(expression);
                var root = tree.GetRoot();
                return root.DescendantNodes().Any(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.AwaitExpressionSyntax);
            }
            catch
            {
                // If parsing fails, fall back to the simple but fragile check
                return expression.Contains("await");
            }
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
        [RequiresUnreferencedCode("RoslynRules builds delegate types via MakeGenericType which trimming may strip.")]
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
        [RequiresUnreferencedCode("RoslynRules builds async delegate types via MakeGenericType which trimming may strip.")]
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
        [RequiresUnreferencedCode("RoslynRules invokes the compiler via reflection (GetMethod, MakeGenericMethod). This code may not work correctly with trimming or AOT.")]
        private static Delegate CompileDelegate(ExpressionCompiler compiler, string expression, Type delegateType, RuleParameter[] parameters, string[]? additionalNamespaces, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            var paramNames = parameters.Select(p => p.Name).ToArray();
            var method = compiler.GetType().GetMethod("Compile")!.MakeGenericMethod(delegateType);
            var result = method.Invoke(compiler, new object?[] { expression, paramNames, additionalNamespaces ?? Array.Empty<string>(), referenceProvider });
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
            => ExecuteWithContext(null, parameters);

        /// <summary>
        /// Executes the rule with access to dependency results via a RuleContext.
        /// The context provides access to the outputs of rules this rule depends on.
        /// </summary>
        /// <param name="context">Context containing results of previously executed rules.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Result of the rule evaluation.</returns>
        public RuleResult ExecuteWithContext(RuleContext? context, params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = ExecuteCore(context, parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false, Id, Description, IsActive, Exception: ex);
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

            // Store result in context for dependent rules
            context?.StoreResult(Id, result);

            return result;
        }

        /// <summary>
        /// Core execution logic without logging overhead.
        /// Enforces per-rule timeout if configured.
        /// </summary>
        /// <param name="context">Optional context for accessing dependency results.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        private RuleResult ExecuteCore(RuleContext? context, RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // If timeout is configured, wrap execution in a timeout.
            // NOTE: This blocks a thread-pool thread. For production use with timeouts,
            // prefer ExecuteAsync() which uses cooperative cancellation without blocking.
            if (Timeout.HasValue)
            {
                using var cts = new CancellationTokenSource();
                var workTask = Task.Run(() => ExecuteCoreInternal(context, parameters, cts.Token), cts.Token);
                var timeoutTask = Task.Delay(Timeout.Value, cts.Token);
                var completed = Task.WhenAny(workTask, timeoutTask).GetAwaiter().GetResult();

                if (completed == timeoutTask)
                {
                    cts.Cancel(); // Signal cancellation to the work task
                    throw new RuleTimeoutException(Id, Timeout.Value);
                }

                return workTask.GetAwaiter().GetResult();
            }

            return ExecuteCoreInternal(context, parameters);
        }

        /// <summary>
        /// Validates that execution-time parameters match the compile-time schema.
        /// Checks parameter count, name match, and type compatibility.
        /// </summary>
        /// <param name="parameters">Runtime parameters passed to Execute.</param>
        /// <exception cref="RuleValidationException">Thrown when parameter name or type mismatch is detected.</exception>
        private void ValidateExecutionParameters(RuleParameter[] parameters)
        {
            // Skip validation if this rule has no compiled delegates (e.g., only child rules).
            if (_compiledParameterType == null)
                return;

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            var param = parameters[0];

            // Validate name matches compile-time name.
            if (!string.Equals(param.Name, _compiledParameterName, StringComparison.Ordinal))
            {
                throw new RuleValidationException(
                    $"Parameter name mismatch for rule '{Description}' (Id: {Id}). " +
                    $"Expected parameter name '{_compiledParameterName}' (compiled), but received '{param.Name}'. " +
                    "Ensure Execute() uses the same parameter name as Compile().");
            }

            // Validate type is assignable to compile-time type.
            if (!param.Type.IsAssignableTo(_compiledParameterType))
            {
                var valueTypeName = param.Value?.GetType()?.Name ?? "null";
                throw new RuleValidationException(
                    $"Parameter type mismatch for rule '{Description}' (Id: {Id}). " +
                    $"Expected type '{_compiledParameterType.Name}' (compiled), but received '{param.Type.Name}'. " +
                    $"Value type '{valueTypeName}' is not assignable to '{_compiledParameterType.Name}'.");
            }
        }

        /// <summary>
        /// Core execution logic without timeout or logging.
        /// Fires OnRuleExecuting and OnRuleExecuted lifecycle events.
        /// Exceptions propagate naturally — caught by ExecuteWithContext for logging.
        /// </summary>
        private RuleResult ExecuteCoreInternal(RuleContext? context, RuleParameter[] parameters, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Validate execute-time parameters match compile-time schema.
            ValidateExecutionParameters(parameters);

            // Check cache first
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                if (_resultCache.TryGet(cacheKey, out var cachedResult))
                    return cachedResult;
            }

            var sw = Stopwatch.StartNew();
            RuleResult result;

            // Fire OnRuleExecuting event
            var executingArgs = new RuleExecutingEventArgs(this, parameters);
            OnRuleExecuting?.Invoke(this, executingArgs);
            if (executingArgs.Cancel)
            {
                result = new RuleResult(true, Id, Description, IsActive, Value: null,
                    ChildResults: new List<RuleResult>(),
                    Exception: executingArgs.CancelReason != null
                        ? new OperationCanceledException(executingArgs.CancelReason)
                        : null);
                goto Completed;
            }

            var paramValue = parameters[0].Value;

            // Bottom-up: evaluate all active children first
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childResult = child.ExecuteWithContext(context, parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                {
                    result = new RuleResult(false, Id, Description, IsActive, ChildResults: childResults);
                    goto Completed;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate compiled Expression if present
            if (_compiledExpression != null)
            {
                var exprResult = _compiledExpression.Invoke(paramValue);
                if (!(bool)exprResult!)
                {
                    result = new RuleResult(false, Id, Description, IsActive, ChildResults: childResults);
                    goto Completed;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Execute compiled Action if present
            if (_compiledAction != null)
            {
                var actionResult = _compiledAction.Invoke(paramValue);
                result = new RuleResult(true, Id, Description, IsActive, actionResult, ChildResults: childResults);
                goto Completed;
            }

            result = new RuleResult(true, Id, Description, IsActive, ChildResults: childResults);

        Completed:
            sw.Stop();

            // Fire OnRuleExecuted event
            var executedArgs = new RuleExecutedEventArgs(this, result, sw.Elapsed);
            OnRuleExecuted?.Invoke(this, executedArgs);

            // Store result in cache
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                _resultCache.Set(cacheKey, result, CacheDuration.Value);
            }

            return result;
        }

        /// <summary>
        /// Executes the rule asynchronously. Supports async expressions containing await.
        /// Children are executed sequentially (bottom-up dependency), but their internal
        /// async operations are properly awaited.
        /// Fires logging events if Logger is set.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Task containing the rule evaluation result.</returns>
        public Task<RuleResult> ExecuteAsync(params RuleParameter[] parameters)
            => ExecuteWithContextAsync(null, parameters);

        /// <summary>
        /// Executes the rule asynchronously with access to dependency results via a RuleContext.
        /// The context provides access to the outputs of rules this rule depends on.
        /// </summary>
        /// <param name="context">Context containing results of previously executed rules.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Task containing the rule evaluation result.</returns>
        public async Task<RuleResult> ExecuteWithContextAsync(RuleContext? context, params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = await ExecuteCoreAsync(context, parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false, Id, Description, IsActive, Exception: ex);
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

            // Store result in context for dependent rules
            context?.StoreResult(Id, result);

            return result;
        }

        /// <summary>
        /// Core async execution logic without logging overhead.
        /// Enforces per-rule timeout if configured.
        /// Fires OnRuleExecuting and OnRuleExecuted lifecycle events.
        /// Exceptions propagate naturally — caught by ExecuteWithContextAsync for logging.
        /// </summary>
        /// <param name="context">Optional context for accessing dependency results.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        private async Task<RuleResult> ExecuteCoreAsync(RuleContext? context, RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Check cache first
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                if (_resultCache.TryGet(cacheKey, out var cachedResult))
                    return cachedResult;
            }

            var sw = Stopwatch.StartNew();
            RuleResult result;

            // Fire OnRuleExecuting event
            var executingArgs = new RuleExecutingEventArgs(this, parameters);
            OnRuleExecuting?.Invoke(this, executingArgs);
            if (executingArgs.Cancel)
            {
                result = new RuleResult(true, Id, Description, IsActive, Value: null,
                    ChildResults: new List<RuleResult>(),
                    Exception: executingArgs.CancelReason != null
                        ? new OperationCanceledException(executingArgs.CancelReason)
                        : null);
            }
            else if (Timeout.HasValue)
            {
                // If timeout is configured, wrap execution in a timed task
                using var cts = new CancellationTokenSource((int)Timeout.Value.TotalMilliseconds);
                var task = ExecuteCoreAsyncInternal(context, parameters, cts.Token);
                var completed = await Task.WhenAny(task, Task.Delay(Timeout.Value, cts.Token));
                if (completed != task)
                    throw new RuleTimeoutException(Id, Timeout.Value);
                result = await task;
            }
            else
            {
                result = await ExecuteCoreAsyncInternal(context, parameters, CancellationToken.None);
            }

            sw.Stop();

            // Fire OnRuleExecuted event
            var executedArgs = new RuleExecutedEventArgs(this, result, sw.Elapsed);
            OnRuleExecuted?.Invoke(this, executedArgs);

            // Store result in cache
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                _resultCache.Set(cacheKey, result, CacheDuration.Value);
            }

            return result;
        }

        /// <summary>
        /// Core async execution logic without timeout or logging.
        /// </summary>
        private async Task<RuleResult> ExecuteCoreAsyncInternal(RuleContext? context, RuleParameter[] parameters, CancellationToken cancellationToken)
        {
            if (!IsActive)
                return new RuleResult(true, Id, Description, IsActive);

            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Validate execute-time parameters match compile-time schema.
            ValidateExecutionParameters(parameters);

            var paramValue = parameters[0].Value;

            // Bottom-up: evaluate all active children first (async)
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childResult = await child.ExecuteWithContextAsync(context, parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                return new RuleResult(false, Id, Description, IsActive, ChildResults: childResults);
            }

            cancellationToken.ThrowIfCancellationRequested();

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
                return new RuleResult(false, Id, Description, IsActive, ChildResults: childResults);
            }

            cancellationToken.ThrowIfCancellationRequested();

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
                return new RuleResult(true, Id, Description, IsActive, actionResult, ChildResults: childResults);
            }

                return new RuleResult(true, Id, Description, IsActive, ChildResults: childResults);
        }

        /// <summary>
        /// Performs semantic validation by attempting to compile the rule's Expression and Action.
        /// This catches errors that syntax-only validation misses: undefined identifiers,
        /// missing types, incorrect method signatures, missing using directives, etc.
        /// </summary>
        /// <param name="compiler">Expression compiler for the dry-run compilation.</param>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <exception cref="RuleCompilationException">Thrown if semantic errors are found in the Expression or Action.</exception>
        public void ValidateSemantics(ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null)
        {
            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"ValidateSemantics supports exactly one parameter. You provided {parameters.Length}. " +
                    "Wrap multiple values in a struct/class.");

            if (!string.IsNullOrEmpty(Expression))
            {
                try
                {
                    var delegateType = BuildDelegateType(typeof(object), parameters);
                    var exprDelegate = CompileDelegate(compiler, Expression, delegateType, parameters, additionalNamespaces);
                }
                catch (Exception ex)
                {
                    throw new RuleCompilationException(
                        $"Semantic error in rule '{Description}' (Id: {Id}) Expression: {ex.Message}", ex);
                }
            }

            if (!string.IsNullOrEmpty(Action))
            {
                try
                {
                    var delegateType = BuildDelegateType(typeof(void), parameters);
                    var actionDelegate = CompileDelegate(compiler, Action, delegateType, parameters, additionalNamespaces);
                }
                catch (Exception ex)
                {
                    throw new RuleCompilationException(
                        $"Semantic error in rule '{Description}' (Id: {Id}) Action: {ex.Message}", ex);
                }
            }

            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.ValidateSemantics(compiler, parameters, additionalNamespaces);
            }
        }    }
}
