using System;
using System.Linq.Expressions;

namespace Rules.Models
{
    /// <summary>
    /// Lightweight parameter container used to pass typed values into rule expressions during compilation and execution.
    /// Replaces DynamicExpresso.Parameter with a minimal, own-implementation alternative.
    /// </summary>
    public class RuleParameter
    {
        /// <summary>
        /// The parameter name as it appears in the expression string.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The CLR type of the parameter.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The runtime value supplied during execution. May be null for compilation-only parameters.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Cached expression-tree parameter used during Roslyn delegate generation.
        /// </summary>
        internal ParameterExpression Expression { get; }

        /// <summary>
        /// Initializes a new parameter definition.
        /// </summary>
        /// <param name="name">Parameter name matching the expression identifier.</param>
        /// <param name="type">CLR type of the parameter.</param>
        /// <param name="value">Optional runtime value.</param>
        public RuleParameter(string name, Type type, object? value = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
            Expression = System.Linq.Expressions.Expression.Parameter(type, name);
        }
    }
}
