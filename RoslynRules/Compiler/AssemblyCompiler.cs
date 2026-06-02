using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Compiles C# source code into an in-memory assembly using Roslyn.
    /// Uses an AssemblyReferenceProvider to restrict which assemblies are exposed to expressions,
    /// preventing arbitrary code execution from user-supplied expression strings.
    /// </summary>
    internal static class AssemblyCompiler
    {
        private static int _assemblyCounter = 0;
        private static readonly AssemblyReferenceProvider _defaultProvider = new AssemblyReferenceProvider();

        /// <summary>
        /// Compiles C# source code into an in-memory assembly.
        /// </summary>
        /// <param name="code">The C# source code to compile.</param>
        /// <param name="referenceProvider">Optional custom reference provider for sandboxing. Defaults to safe whitelist.</param>
        /// <returns>The compiled assembly bytes.</returns>
        /// <exception cref="InvalidOperationException">Thrown when compilation produces errors.</exception>
        public static byte[] Compile(string code, AssemblyReferenceProvider? referenceProvider = null)
        {
            var provider = referenceProvider ?? _defaultProvider;
            var assemblyName = $"ExpressionAssembly{System.Threading.Interlocked.Increment(ref _assemblyCounter)}";

            // Parse the C# source into a syntax tree.
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Gather all assembly references needed for compilation.
            var references = provider.BuildReferences();

            // Create the Roslyn compilation.
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Compile into a memory stream (no disk writes).
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            // If compilation failed, collect all error messages and throw.
            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
                throw new InvalidOperationException($"Compilation failed:\n{errors}");
            }

            // Return the raw assembly bytes.
            return ms.ToArray();
        }
    }
}
