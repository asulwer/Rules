using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rules.Compiler
{
    /// <summary>
    /// Generates the C# source code string that wraps a user expression.
    /// Produces a static class with a single Evaluate method.
    /// </summary>
    internal static class CodeGenerator
    {
        // Hardcoded default namespaces included in every generated class.
        private static readonly string[] DefaultUsings = new[]
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text"
        };

        /// <summary>
        /// Generates a complete C# source file containing a static Evaluate method.
        /// </summary>
        /// <param name="expression">The raw C# expression body.</param>
        /// <param name="returnType">CLR return type of the delegate.</param>
        /// <param name="parameterNames">Ordered parameter names.</param>
        /// <param name="parameterTypes">Ordered parameter types.</param>
        /// <param name="additionalNamespaces">Extra using directives.</param>
        /// <returns>Complete C# source code string.</returns>
        public static string Generate(
            string expression,
            Type returnType,
            string[] parameterNames,
            Type[] parameterTypes,
            string[]? additionalNamespaces = null)
        {
            var methodSignature = BuildMethodSignature(returnType, parameterNames, parameterTypes);
            var returnKeyword = returnType == typeof(void) ? "" : "return ";
            var usingStatements = BuildUsingStatements(additionalNamespaces);

            var code = $@"
{usingStatements}

public static class ExpressionAssembly
{{
    {methodSignature}
    {{
        {returnKeyword}{expression};
    }}
}}
";
            return code;
        }

        /// <summary>
        /// Builds the method signature line, e.g.:
        /// "public static bool Evaluate(Customer customer)"
        /// </summary>
        private static string BuildMethodSignature(Type returnType, string[] parameterNames, Type[] parameterTypes)
        {
            var paramDecls = parameterNames.Select((name, i) =>
            {
                var paramType = parameterTypes[i];
                return $"{TypeNameResolver.GetTypeName(paramType)} {name}";
            });

            var signaturePrefix = returnType == typeof(void)
                ? "public static void Evaluate"
                : $"public static {TypeNameResolver.GetTypeName(returnType)} Evaluate";

            return $"{signaturePrefix}({string.Join(", ", paramDecls)})";
        }

        /// <summary>
        /// Combines default and additional namespaces into using directives.
        /// </summary>
        private static string BuildUsingStatements(string[]? additionalNamespaces)
        {
            var allUsings = new HashSet<string>(DefaultUsings);

            if (additionalNamespaces != null)
            {
                foreach (var ns in additionalNamespaces)
                    allUsings.Add(ns);
            }

            return string.Join("\n", allUsings.Select(u => $"using {u};"));
        }
    }
}
