using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rules.Compiler
{
    /// <summary>
    /// Public entry point for compiling C# expression strings into typed delegates.
    /// Orchestrates CodeGenerator → AssemblyCompiler → DelegateFactory.
    /// Results are cached for reuse.
    /// </summary>
    public class ExpressionCompiler
    {
        private readonly Dictionary<string, Delegate> _cache = new();
        private readonly object _lock = new();

        /// <summary>
        /// Compiles a C# expression string into a strongly-typed delegate.
        /// Results are cached; subsequent calls with the same signature return the cached delegate.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type, e.g. Func<Customer, bool>.</typeparam>
        /// <param name="expression">The C# expression body.</param>
        /// <param name="parameterNames">Ordered parameter names matching the delegate signature.</param>
        /// <param name="additionalNamespaces">Optional extra using namespaces (e.g. "Demo.Models").</param>
        /// <returns>A typed delegate that evaluates the expression.</returns>
        /// <exception cref="InvalidOperationException">Thrown when expression compilation fails.</exception>
        public TDelegate Compile<TDelegate>(
            string expression,
            string[] parameterNames,
            string[]? additionalNamespaces = null) where TDelegate : Delegate
        {
            // STEP 1: Build a unique cache key.
            var cacheKey = BuildCacheKey<TDelegate>(expression, parameterNames, additionalNamespaces);

            // Check if we already compiled this exact expression.
            lock (_lock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                    return (TDelegate)cached;
            }

            // STEP 2: Reflect the delegate type to discover its signature.
            var delegateType = typeof(TDelegate);
            var invokeMethod = delegateType.GetMethod("Invoke")!;
            var returnType = invokeMethod.ReturnType;
            var parameters = invokeMethod.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            // STEP 3: Generate C# source code.
            var code = CodeGenerator.Generate(
                expression,
                returnType,
                parameterNames,
                parameterTypes,
                additionalNamespaces);

            // STEP 4: Compile source code into raw assembly bytes.
            var assemblyBytes = AssemblyCompiler.Compile(code);

            // STEP 5: Load assembly and create a typed delegate.
            var del = DelegateFactory.CreateDelegate(assemblyBytes, delegateType);

            // STEP 6: Cache and return.
            lock (_lock)
            {
                _cache[cacheKey] = del;
            }

            return (TDelegate)del;
        }

        /// <summary>
        /// Builds a unique cache key combining delegate type, expression, parameters, and namespaces.
        /// </summary>
        private static string BuildCacheKey<TDelegate>(
            string expression,
            string[] parameterNames,
            string[]? additionalNamespaces)
        {
            var key = $"{typeof(TDelegate).FullName}:{expression}:{string.Join(",", parameterNames)}";

            if (additionalNamespaces != null && additionalNamespaces.Length > 0)
            {
                key += $":{string.Join(",", additionalNamespaces)}";
            }

            return key;
        }
    }
}
