using RoslynRules.Abstractions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Extension methods for executing rules and extracting strongly-typed IEnumerable<T> results.
    /// These are convenience wrappers around the core IRuleEngine execution methods.
    /// </summary>
    public static class RuleEngineExtensions
    {
        // ==================== SEQUENTIAL ====================

        /// <summary>
        /// Executes all rules sequentially and extracts an IEnumerable<T> from each result's Value.
        /// Rules whose Value is null or not IEnumerable<T> are filtered out.
        /// </summary>
        /// <typename="T">The element type expected in the returned collections.</typename>
        /// <param name="engine">The rule engine to execute.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>A flattened sequence of all elements from all rule results.</returns>
        /// <exception cref="InvalidCastException">Thrown when a rule's Value is not null and cannot be cast to IEnumerable<T>.</exception>
        public static IEnumerable<T> Execute<T>(this IRuleEngine engine, params RuleParameter[] parameters)
        {
            foreach (var result in engine.Execute(parameters))
            {
                if (result.Value is null)
                    continue;

                if (result.Value is not IEnumerable<T> enumerable)
                    throw new InvalidCastException(
                        $"Rule '{result.RuleDescription}' (Id: {result.RuleId}) returned a value of type {result.Value.GetType().Name}, " +
                        $"but IEnumerable<{typeof(T).Name}> was expected.");

                foreach (var item in enumerable)
                    yield return item;
            }
        }

        // ==================== ASYNC ====================

        /// <summary>
        /// Executes all rules asynchronously and extracts an IEnumerable<T> from each result's Value.
        /// Rules whose Value is null or not IEnumerable<T> are filtered out.
        /// </summary>
        /// <typename="T">The element type expected in the returned collections.</typename>
        /// <param name="engine">The rule engine to execute.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel execution.</param>
        /// <returns>A flattened async sequence of all elements from all rule results.</returns>
        /// <exception cref="InvalidCastException">Thrown when a rule's Value is not null and cannot be cast to IEnumerable<T>.</exception>
        public static async IAsyncEnumerable<T> ExecuteAsync<T>(
            this IRuleEngine engine,
            RuleParameter[] parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in engine.ExecuteAsync(parameters, cancellationToken))
            {
                if (result.Value is null)
                    continue;

                if (result.Value is not IEnumerable<T> enumerable)
                    throw new InvalidCastException(
                        $"Rule '{result.RuleDescription}' (Id: {result.RuleId}) returned a value of type {result.Value.GetType().Name}, " +
                        $"but IEnumerable<{typeof(T).Name}> was expected.");

                foreach (var item in enumerable)
                    yield return item;
            }
        }

        // ==================== PARALLEL ====================

        /// <summary>
        /// Executes all rules in parallel and extracts an IEnumerable<T> from each result's Value.
        /// Rules whose Value is null or not IEnumerable<T> are filtered out.
        /// Results are flattened into a single array.
        /// </summary>
        /// <typename="T">The element type expected in the returned collections.</typename>
        /// <param name="engine">The rule engine to execute.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>A flattened array of all elements from all rule results.</returns>
        /// <exception cref="InvalidCastException">Thrown when a rule's Value is not null and cannot be cast to IEnumerable<T>.</exception>
        public static T[] ExecuteParallel<T>(this IRuleEngine engine, params RuleParameter[] parameters)
        {
            var results = engine.ExecuteParallel(parameters);
            return ExtractAndFlatten<T>(results);
        }

        /// <summary>
        /// Executes all rules in parallel asynchronously and extracts an IEnumerable<T> from each result's Value.
        /// Rules whose Value is null or not IEnumerable<T> are filtered out.
        /// Results are flattened into a single array.
        /// </summary>
        /// <typename="T">The element type expected in the returned collections.</typename>
        /// <param name="engine">The rule engine to execute.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel execution.</param>
        /// <returns>A flattened array of all elements from all rule results.</returns>
        /// <exception cref="InvalidCastException">Thrown when a rule's Value is not null and cannot be cast to IEnumerable<T>.</exception>
        public static async Task<T[]> ExecuteParallelAsync<T>(
            this IRuleEngine engine,
            RuleParameter[] parameters,
            CancellationToken cancellationToken = default)
        {
            var results = await engine.ExecuteParallelAsync(parameters, cancellationToken);
            return ExtractAndFlatten<T>(results);
        }

        // ==================== BUFFERED ====================

        /// <summary>
        /// Executes rules in buffered chunks and extracts IEnumerable<T> from each result's Value.
        /// Rules whose Value is null or not IEnumerable<T> are filtered out.
        /// </summary>
        /// <typename="T">The element type expected in the returned collections.</typename>
        /// <param name="engine">The workflow to execute (only Workflow supports buffered execution).</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="bufferSize">Number of rules per batch.</param>
        /// <param name="cancellationToken">Token to cancel execution.</param>
        /// <returns>Async stream of flattened element arrays per batch.</returns>
        /// <exception cref="InvalidCastException">Thrown when a rule's Value is not null and cannot be cast to IEnumerable<T>.</exception>
        public static async IAsyncEnumerable<T[]> ExecuteBufferedAsync<T>(
            this Workflow workflow,
            RuleParameter[] parameters,
            int bufferSize = 10,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var batch in workflow.ExecuteBufferedAsync(parameters, bufferSize, cancellationToken))
            {
                yield return ExtractAndFlatten<T>(batch);
            }
        }

        // ==================== HELPERS ====================

        private static T[] ExtractAndFlatten<T>(IEnumerable<RuleResult> results)
        {
            var flattened = new List<T>();

            foreach (var result in results)
            {
                if (result.Value is null)
                    continue;

                if (result.Value is not IEnumerable<T> enumerable)
                    throw new InvalidCastException(
                        $"Rule '{result.RuleDescription}' (Id: {result.RuleId}) returned a value of type {result.Value.GetType().Name}, " +
                        $"but IEnumerable<{typeof(T).Name}> was expected.");

                flattened.AddRange(enumerable);
            }

            return flattened.ToArray();
        }
    }
}
