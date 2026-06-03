using Microsoft.Extensions.Logging;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace RoslynRules.Models
{
    /// <summary>
    /// Represents an individual rule within a workflow.
    /// Each rule evaluates an optional boolean Expression, executes an optional Action,
    /// and can contain child rules that must all succeed for the parent to succeed.
    /// After compilation, rule properties become immutable to ensure thread-safe execution.
    /// Supports both synchronous and asynchronous expressions.
    /// </summary>
    public sealed partial class Rule
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
        /// Detects if EF Core lazy loading proxies are being used (not supported on sealed types).
        /// Throws a clear exception with guidance on supported loading strategies.
        /// </summary>
        private void DetectLazyLoading()
        {
            // EF Core lazy loading proxies create runtime subclasses.
            // Rule is sealed — if GetType() != typeof(Rule), it&apos;s a proxy.
            if (GetType() != typeof(Rule))
            {
                throw new InvalidOperationException(
                    "Lazy loading detected on Rule, but Rule is sealed and does not support EF Core lazy loading proxies. " +
                    "Use eager loading (.Include(r => r.ChildRules).ThenInclude(...)) or explicit loading (entry.Collection(r => r.ChildRules).Load()) instead. " +
                    "See docs/index.md#ef-core-integration-note for examples.");
            }
        }

        /// <summary>
        /// Unique identifier for the rule.
        /// </summary>
        [Key] [JsonInclude] public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Public constructor for external factory methods (e.g. EF Core mapping).
        /// Preserves the specified ID without generating a new one.
        /// </summary>
        public Rule(Guid id)
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
        /// Child rules are evaluated independently; only this rule&apos;s final result is cached.
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
        /// Lazy loading is NOT supported — Rule is sealed to enforce immutability.
        /// Use eager loading (Include/ThenInclude) or explicit loading (Load()) with EF Core.
        /// </summary>
        public IList<Rule> ChildRules 
        { 
            get
            {
                DetectLazyLoading();
                return _childRules;
            }
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

        /// <summary>
        /// Optional logger for observing rule execution.
        /// Set this to any ILogger implementation (Serilog, NLog, etc.).
        /// </summary>
        [NotMapped] public ILogger? Logger { get; set; }

        /// <summary>
        /// Clears the cached result for this rule, forcing the next evaluation to re-execute.
        /// Thread-safe.
        /// </summary>
        public void ClearCache() => _resultCache.Clear();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => $"Rule: {Description} (Id: {Id})";
    }
}
