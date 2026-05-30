using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Models
{
    /// <summary>
    /// Immutable result returned from a single rule evaluation.
    /// Includes detailed information about which rule ran, why it failed,
    /// and results from child rules for full traceability.
    /// </summary>
    public readonly struct RuleResult
    {
        /// <summary>
        /// True if the rule passed all evaluations (expression and children).
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Unique identifier of the rule that produced this result.
        /// </summary>
        public Guid RuleId { get; }

        /// <summary>
        /// Human-readable description of the rule.
        /// </summary>
        public string RuleDescription { get; }

        /// <summary>
        /// Whether the rule was active when evaluated.
        /// Inactive rules return Success=true with IsActive=false.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Optional return value from the rule&apos;s Action delegate.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Exception that caused the failure, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Results from child rules, in evaluation order.
        /// Empty if the rule has no children.
        /// </summary>
        public IReadOnlyList<RuleResult> ChildResults { get; }

        /// <summary>
        /// Initializes a new rule result with full details.
        /// </summary>
        public RuleResult(
            bool success,
            Guid ruleId = default,
            string ruleDescription = "",
            bool isActive = true,
            object? value = null,
            Exception? exception = null,
            IReadOnlyList<RuleResult>? childResults = null)
        {
            Success = success;
            RuleId = ruleId;
            RuleDescription = ruleDescription;
            IsActive = isActive;
            Value = value;
            Exception = exception;
            ChildResults = childResults ?? Array.Empty<RuleResult>();
        }

        /// <summary>
        /// Returns the first failing child result, or null if all passed.
        /// Useful for identifying exactly which child rule caused a parent failure.
        /// </summary>
        public RuleResult? FirstFailure =>
            ChildResults.FirstOrDefault(r => !r.Success);

        /// <summary>
        /// Returns all failing child results.
        /// </summary>
        public IEnumerable<RuleResult> AllFailures =>
            ChildResults.Where(r => !r.Success);
    }
}
