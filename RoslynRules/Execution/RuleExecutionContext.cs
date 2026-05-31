using RoslynRules.Models;
using System;
using System.Threading;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Provides ambient access to the current RuleContext during rule execution.
    /// This allows expressions to access the results of previously executed rules
    /// without changing the single-parameter contract of rule expressions.
    /// 
    /// Usage in expressions: RuleExecutionContext.Current.GetResult(ruleId)
    /// 
    /// Note: For parallel execution, dependencies are resolved sequentially
    /// before parallel rules execute, so the context is always valid.
    /// </summary>
    public static class RuleExecutionContext
    {
        private static readonly AsyncLocal<RuleContext?> _current = new AsyncLocal<RuleContext?>();

        /// <summary>
        /// Gets or sets the RuleContext for the current execution scope.
        /// </summary>
        public static RuleContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        /// <summary>
        /// Clears the current execution context.
        /// Called by the workflow engine after execution completes.
        /// </summary>
        public static void Clear() => _current.Value = null;
    }
}
