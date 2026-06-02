using RoslynRules.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Loads a compiled assembly from a collectible AssemblyLoadContext
    /// and creates a typed delegate from its Evaluate method.
    /// </summary>
    internal static class DelegateFactory
    {
        /// <summary>
        /// Loads the assembly bytes into a collectible context, extracts the Evaluate method,
        /// and creates a typed delegate.
        /// </summary>
        /// <param name="assemblyBytes">Raw bytes of the compiled assembly.</param>
        /// <param name="delegateType">The delegate type to create (e.g., Func&lt;Customer, bool&gt;).</param>
        /// <param name="context">The collectible load context to load the assembly into.</param>
        /// <returns>A typed delegate pointing to the compiled Evaluate method.</returns>
        [RequiresUnreferencedCode("RoslynRules loads generated assemblies and resolves methods by name. This code may not work correctly with trimming or AOT.")]
        public static Delegate CreateDelegate(byte[] assemblyBytes, Type delegateType, ExpressionAssemblyLoadContext context)
        {
            // Load the compiled assembly into the collectible context.
            var assembly = context.LoadAssembly(assemblyBytes);

            // The generated class is always named "ExpressionAssembly".
            var type = assembly.GetType("ExpressionAssembly");
            if (type is null)
                throw new RuleCompilationException("Compiled assembly does not contain expected type 'ExpressionAssembly'.");

            // The generated method is always named "Evaluate".
            var method = type.GetMethod("Evaluate");
            if (method is null)
                throw new RuleCompilationException($"Type '{type.FullName}' does not contain expected method 'Evaluate'.");

            // Convert MethodInfo into a typed delegate.
            return System.Delegate.CreateDelegate(delegateType, method);
        }
    }
}
