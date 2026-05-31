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
    /// </summary>
    internal static class AssemblyCompiler
    {
        private static int _assemblyCounter = 0;

        /// <summary>
        /// Compiles C# source code into an in-memory assembly.
        /// </summary>
        /// <param name="code">The C# source code to compile.</param>
        /// <returns>The compiled assembly bytes.</returns>
        /// <exception cref="InvalidOperationException">Thrown when compilation produces errors.</exception>
        public static byte[] Compile(string code)
        {
            var assemblyName = $"ExpressionAssembly{System.Threading.Interlocked.Increment(ref _assemblyCounter)}";

            // Parse the C# source into a syntax tree.
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Gather all assembly references needed for compilation.
            var references = BuildReferences();

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

        /// <summary>
        /// Builds the list of assembly references for the Roslyn compiler.
        /// Includes core .NET assemblies and all currently loaded assemblies.
        /// </summary>
        private static List<MetadataReference> BuildReferences()
        {
            var references = new List<MetadataReference>
            {
                // System.dll - basic types: object, string, int, bool, etc.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                // System.Core.dll - LINQ, Enumerable extensions.
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            // Add all currently loaded assemblies so expressions can reference
            // types from the calling application (e.g., Demo.Models.Customer).
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(asm.Location));
                }
            }

            return references;
        }
    }
}
