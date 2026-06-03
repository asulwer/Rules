using Microsoft.CodeAnalysis.CSharp;
using RoslynRules.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Models
{
    public sealed partial class Rule
    {
        // ==================== COMPILATION ====================

        /// <summary>
        /// Compiles the Expression and Action into delegates using the supplied compiler.
        /// Validates against circular child references before compilation.
        /// After compilation, all properties become immutable to ensure thread safety.
        /// Recursively compiles all active child rules.
        /// </summary>
        /// <param name="compiler">The expression compiler instance.</param>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <param name="referenceProvider">Optional custom assembly reference provider for sandboxing.</param>
        public void Compile(Compiler.ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            // Validate parameter constraints.
            if (parameters.Length != 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            // Store parameter schema for execution-time validation.
            _compiledParameterType = parameters[0].Type;
            _compiledParameterName = parameters[0].Name;

            // Validate no circular references before compiling.
            ValidateNoCircularReferences();

            if (!string.IsNullOrEmpty(Expression))
            {
                var isAsync = IsAsyncExpression(Expression);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(bool), parameters)
                    : BuildDelegateType(typeof(bool), parameters);
                var rawDelegate = CompileDelegate(compiler, Expression, delegateType, parameters, additionalNamespaces, referenceProvider);
                _compiledExpression = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            if (!string.IsNullOrEmpty(Action))
            {
                var isAsync = IsAsyncExpression(Action);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(void), parameters)
                    : BuildDelegateType(typeof(void), parameters);
                var rawDelegate = CompileDelegate(compiler, Action, delegateType, parameters, additionalNamespaces, referenceProvider);
                _compiledAction = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            // Compile children recursively
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Compile(compiler, parameters, additionalNamespaces, referenceProvider);
            }

            _isCompiled = true; // Lock properties after compilation
        }

        /// <summary>
        /// Checks if an expression contains async/await keywords by parsing the syntax tree.
        /// This avoids false positives from variable names like "awaiting" or string literals.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <returns>True if the expression contains an await expression node.</returns>
        private static bool IsAsyncExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(expression);
                var root = tree.GetRoot();
                return root.DescendantNodes().Any(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.AwaitExpressionSyntax);
            }
            catch
            {
                // If parsing fails, fall back to the simple but fragile check
                return expression.Contains("await");
            }
        }

        /// <summary>
        /// Builds a Func or Action delegate type matching the parameter signature.
        /// Supports exactly one input parameter and one return type (or void).
        /// For multiple inputs/outputs, wrap them in a struct or class.
        /// </summary>
        /// <example>
        /// Single input, single return: Func<Customer, bool>
        /// Single input, void return: Action<Customer>
        /// Single input, composite return: Func<Customer, Returned>
        /// </example>
        [RequiresUnreferencedCode("RoslynRules builds delegate types via MakeGenericType which trimming may strip.")]
        private static Type BuildDelegateType(Type returnType, RuleParameter[] parameters)
        {
            if (parameters.Length > 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            var paramType = parameters[0].Type;
            
            if (returnType == typeof(void))
            {
                return typeof(Action<>).MakeGenericType(paramType);
            }
            else
            {
                return typeof(Func<,>).MakeGenericType(paramType, returnType);
            }
        }

        /// <summary>
        /// Builds an async delegate type returning Task<TReturn> or Task.
        /// </summary>
        /// <param name="returnType">The result type (not Task, the inner type).</param>
        /// <param name="parameters">Parameter definitions.</param>
        /// <returns>Func<TParam, Task<TReturn>> or Func<TParam, Task>.</returns>
        [RequiresUnreferencedCode("RoslynRules builds async delegate types via MakeGenericType which trimming may strip.")]
        private static Type BuildAsyncDelegateType(Type returnType, RuleParameter[] parameters)
        {
            if (parameters.Length > 1)
                throw new NotSupportedException(
                    $"Rules support exactly one input parameter. You provided {parameters.Length}. " +
                    "Wrap multiple inputs in a struct/class.");

            var paramType = parameters[0].Type;
            
            if (returnType == typeof(void))
            {
                return typeof(Func<,>).MakeGenericType(paramType, typeof(Task));
            }
            else
            {
                var taskType = typeof(Task<>).MakeGenericType(returnType);
                return typeof(Func<,>).MakeGenericType(paramType, taskType);
            }
        }

        /// <summary>
        /// Invokes the compiler via reflection to create a typed delegate.
        /// </summary>
        [RequiresUnreferencedCode("RoslynRules invokes the compiler via reflection (GetMethod, MakeGenericMethod). This code may not work correctly with trimming or AOT.")]
        private static Delegate CompileDelegate(Compiler.ExpressionCompiler compiler, string expression, Type delegateType, RuleParameter[] parameters, string[]? additionalNamespaces, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            var paramNames = parameters.Select(p => p.Name).ToArray();
            var method = compiler.GetType().GetMethod("Compile")!.MakeGenericMethod(delegateType);
            var result = method.Invoke(compiler, new object?[] { expression, paramNames, additionalNamespaces ?? Array.Empty<string>(), referenceProvider });
            if (result is not Delegate delegateResult)
                throw new RuleCompilationException("Compiler did not return a valid delegate.");
            return delegateResult;
        }
    }
}
