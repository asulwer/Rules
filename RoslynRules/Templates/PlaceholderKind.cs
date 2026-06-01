namespace RoslynRules.Templates
{
    /// <summary>
    /// Specifies whether a template placeholder represents a type or a value.
    /// </summary>
    public enum PlaceholderKind
    {
        /// <summary>
        /// The placeholder represents a CLR type (e.g., Customer, System.String).
        /// Substituted as an unquoted type name in the expression.
        /// </summary>
        Type,

        /// <summary>
        /// The placeholder represents a code identifier (e.g., parameter name, variable).
        /// Substituted as raw text without quotes.
        /// </summary>
        Identifier,

        /// <summary>
        /// The placeholder represents a literal value (e.g., 18, "active").
        /// Substituted as a properly formatted literal in the expression.
        /// </summary>
        Value
    }
}
