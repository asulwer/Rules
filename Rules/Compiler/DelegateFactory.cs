using System;
using System.Reflection;

namespace Rules.Compiler
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
        public static Delegate CreateDelegate(byte[] assemblyBytes, Type delegateType)
        {
            // Load the compiled assembly into the current AppDomain.
            var assembly = Assembly.Load(assemblyBytes);

            // The generated class is always named "ExpressionAssembly".
            var type = assembly.GetType("ExpressionAssembly")!;

            // The generated method is always named "Evaluate".
            var method = type.GetMethod("Evaluate")!;

            // Convert MethodInfo into a typed delegate.
            return System.Delegate.CreateDelegate(delegateType, method);
        }
    }
}
