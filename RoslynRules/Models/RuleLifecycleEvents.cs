using System;

namespace RoslynRules.Models
{
    /// <summary>
    /// Event args fired before a rule executes.
    /// Set Cancel = true to skip rule execution.
    /// </summary>
    public class RuleExecutingEventArgs : EventArgs
    {
        /// <summary>
        /// The rule about to execute.
        /// </summary>
        public Rule Rule { get; }

        /// <summary>
        /// The parameters passed to the rule.
        /// </summary>
        public RuleParameter[] Parameters { get; }

        /// <summary>
        /// When true, the rule execution is skipped and a RuleResult with Success=true is returned.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// When Cancel is true, this reason is logged (optional).
        /// </summary>
        public string? CancelReason { get; set; }

        public RuleExecutingEventArgs(Rule rule, RuleParameter[] parameters)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
    }

    /// <summary>
    /// Event args fired after a rule completes execution.
    /// </summary>
    public class RuleExecutedEventArgs : EventArgs
    {
        /// <summary>
        /// The rule that was executed.
        /// </summary>
        public Rule Rule { get; }

        /// <summary>
        /// The result of rule execution.
        /// </summary>
        public RuleResult Result { get; }

        /// <summary>
        /// Time spent executing the rule (excluding child rules).
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// Exception thrown during execution, if any.
        /// </summary>
        public Exception? Exception { get; }

        public RuleExecutedEventArgs(Rule rule, RuleResult result, TimeSpan elapsed, Exception? exception = null)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Result = result;
            Elapsed = elapsed;
            Exception = exception;
        }
    }
}
