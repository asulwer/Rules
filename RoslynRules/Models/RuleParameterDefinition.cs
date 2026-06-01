using System;

namespace RoslynRules.Models
{
    /// <summary>
    /// A lightweight parameter definition used during compilation.
    /// Contains only the name and type — no runtime value required.
    /// Use this to compile workflows and rules without creating dummy instances.
    /// </summary>
    /// <param name="Name">The parameter name as it appears in expression strings.</param>
    /// <param name="Type">The CLR type of the parameter.</param>
    public record RuleParameterDefinition(string Name, Type Type);
}
