using Rules.Models;
using System;
using System.Collections.Generic;

namespace Rules.Execution
{
    /// <summary>
    /// Provides access to the results of previously executed rules within a workflow.
    /// Used when rules depend on each other (DependsOnRuleId) to share outputs.
    /// </summary>
    public class RuleContext
    {
        private readonly Dictionary<Guid, RuleResult> _results = new Dictionary<Guid, RuleResult>();

        /// <summary>
        /// Stores the result of a rule execution.
        /// Called by the workflow engine after each rule completes.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <param name="result">The execution result.</param>
        public void StoreResult(Guid ruleId, RuleResult result)
        {
            _results[ruleId] = result;
        }

        /// <summary>
        /// Retrieves the result of a previously executed rule.
        /// Returns null if the rule has not been executed yet.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>The rule result, or null if not found.</returns>
        public RuleResult? GetResult(Guid ruleId)
        {
            _results.TryGetValue(ruleId, out var result);
            return result;
        }

        /// <summary>
        /// Checks if a rule has been executed and its result is available.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>True if the result is available.</returns>
        public bool HasResult(Guid ruleId) => _results.ContainsKey(ruleId);

        /// <summary>
        /// Gets the typed value from a rule&apos;s result.
        /// Returns default(T) if the rule is not found, failed, or has no value.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>The typed value, or default(T).</returns>
        public T? GetValue<T>(Guid ruleId)
        {
            if (_results.TryGetValue(ruleId, out var result) && result.Success && result.Value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Gets all stored results.
        /// </summary>
        public IReadOnlyDictionary<Guid, RuleResult> Results => _results;

        /// <summary>
        /// Clears all stored results.
        /// Called by the workflow engine before each workflow execution.
        /// </summary>
        public void Clear() => _results.Clear();
    }
}
