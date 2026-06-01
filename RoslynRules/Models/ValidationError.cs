using System;

namespace RoslynRules.Models
{
    /// <summary>
    /// Represents a validation error found during workflow or rule validation.
    /// Returned by <see cref="Workflow.ValidateAll"/> and <see cref="Batch.RuleBatch.ValidateAll"/>.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// The rule or workflow ID where the error was found.
        /// </summary>
        public Guid? EntityId { get; }

        /// <summary>
        /// Human-readable description of the affected entity.
        /// </summary>
        public string? EntityDescription { get; }

        /// <summary>
        /// The error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The type of validation failure.
        /// </summary>
        public ValidationErrorType ErrorType { get; }

        public ValidationError(string message, ValidationErrorType errorType, Guid? entityId = null, string? entityDescription = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ErrorType = errorType;
            EntityId = entityId;
            EntityDescription = entityDescription;
        }

        public override string ToString() => $"[{ErrorType}] {Message}";
    }

    /// <summary>
    /// Categories of validation errors.
    /// </summary>
    public enum ValidationErrorType
    {
        /// <summary>No active rules in workflow or batch.</summary>
        NoActiveRules,
        /// <summary>Rule has no Expression, Action, or active ChildRules.</summary>
        EmptyRule,
        /// <summary>Circular reference detected in child rules or dependencies.</summary>
        CircularReference,
        /// <summary>C# expression or action has syntax errors.</summary>
        SyntaxError,
        /// <summary>Duplicate rule IDs within a workflow.</summary>
        DuplicateRuleId,
        /// <summary>Dependency rule does not exist or is inactive.</summary>
        MissingDependency,
        /// <summary>Generic validation failure.</summary>
        General
    }
}
