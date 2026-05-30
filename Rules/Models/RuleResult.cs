using System;
using System.Collections.Generic;
using System.Text;

namespace Rules.Models
{
    /// <summary>
    /// Immutable result returned from a single rule evaluation.
    /// </summary>
    public readonly struct RuleResult
    {
        /// <summary>
        /// True if the rule passed all evaluations (expression and children).
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Optional return value from the rule&apos;s Action delegate.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Initializes a new rule result.
        /// </summary>
        /// <param name="success">Pass/fail status.</param>
        /// <param name="value">Optional action return value.</param>
        public RuleResult(bool success, object? value = null)
        {
            Success = success;
            Value = value;
        }
    }
}
