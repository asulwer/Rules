using RoslynRules.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Loads a compiled assembly and creates a typed delegate from its Evaluate method.
    /// </summary>
    internal static class DelegateFactory
    {
        /// <summary>
        /// Loads the assembly bytes, extracts the Evaluate method, and creates a typed delegate.
        /// </summary>
        /// <param name="assemblyBytes">Raw bytes of the compiled assembly.</param>
        /// <param name="delegateType">The delegate type to create (e.g., Func<Customer, bool>).</param>
        /// <returns>A typed delegate pointing to the compiled Evaluate method.</returns>
        [RequiresUnreferencedCode("RoslynRules loads generated assemblies and resolves methods by name. This code may not work correctly with trimming or AOT.")]
        public static Delegate CreateDelegate(byte[] assemblyBytes, Type delegateType)
        {
            // Load the compiled assembly into the current AppDomain.
            var assembly = Assembly.Load(assemblyBytes);

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
