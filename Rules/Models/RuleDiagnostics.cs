using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Rules.Models
{
    /// <summary>
    /// Event fired during rule execution for diagnostics, logging, and auditing.
    /// Captures timing, success/failure, and metadata about each rule evaluation.
    /// </summary>
    public class RuleExecutedEvent
    {
        /// <summary>
        /// Unique identifier of the rule.
        /// </summary>
        public Guid RuleId { get; set; }

        /// <summary>
        /// Human-readable description of the rule.
        /// </summary>
        public string RuleDescription { get; set; } = string.Empty;

        /// <summary>
        /// Whether the rule is active (false means it was skipped).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether the rule passed evaluation (true = success or skipped).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public double ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Optional exception if execution failed.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// UTC timestamp when execution occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extension methods for logging rule execution events via Microsoft.Extensions.Logging.
    /// Any logger supporting ILogger (Serilog, NLog, log4net, etc.) will work automatically.
    /// </summary>
    public static class RuleLoggingExtensions
    {
        // Structured logging event IDs for filtering and routing
        private static readonly EventId RuleSkipped = new EventId(1001, "RuleSkipped");
        private static readonly EventId RulePassed = new EventId(1002, "RulePassed");
        private static readonly EventId RuleFailed = new EventId(1003, "RuleFailed");
        private static readonly EventId RuleError = new EventId(1004, "RuleError");

        /// <summary>
        /// Logs a RuleExecutedEvent using the structured Microsoft.Extensions.Logging pipeline.
        /// </summary>
        /// <param name="logger">Any ILogger implementation (Serilog, NLog, etc.).</param>
        /// <param name="event">The execution event to log.</param>
        public static void LogRuleExecuted(this ILogger logger, RuleExecutedEvent @event)
        {
            if (!@event.IsActive)
            {
                logger.LogDebug(RuleSkipped,
                    "[SKIP] {RuleDescription} (Id: {RuleId}) — inactive rule skipped",
                    @event.RuleDescription, @event.RuleId);
                return;
            }

            if (@event.Exception != null)
            {
                logger.LogError(RuleError, @event.Exception,
                    "[ERROR] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds:0.000}ms — {ErrorMessage}",
                    @event.RuleDescription, @event.RuleId, @event.ElapsedMilliseconds, @event.Exception.Message);
                return;
            }

            if (@event.Success)
            {
                logger.LogDebug(RulePassed,
                    "[PASS] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds:0.000}ms",
                    @event.RuleDescription, @event.RuleId, @event.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug(RuleFailed,
                    "[FAIL] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds:0.000}ms",
                    @event.RuleDescription, @event.RuleId, @event.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Logs rule execution at Information level (visible by default in most log configs).
        /// Use this when you want rule results in standard logs, not just Debug output.
        /// </summary>
        public static void LogRuleExecutedInfo(this ILogger logger, RuleExecutedEvent @event)
        {
            if (!@event.IsActive)
            {
                logger.LogInformation(RuleSkipped,
                    "[SKIP] {RuleDescription} (Id: {RuleId}) — inactive",
                    @event.RuleDescription, @event.RuleId);
                return;
            }

            if (@event.Exception != null)
            {
                logger.LogError(RuleError, @event.Exception,
                    "[ERROR] {RuleDescription} (Id: {RuleId}) — {ErrorMessage}",
                    @event.RuleDescription, @event.RuleId, @event.Exception.Message);
                return;
            }

            var status = @event.Success ? "PASS" : "FAIL";
            logger.LogInformation(
                "[{Status}] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds:0.000}ms",
                status, @event.RuleDescription, @event.RuleId, @event.ElapsedMilliseconds);
        }
    }
}
