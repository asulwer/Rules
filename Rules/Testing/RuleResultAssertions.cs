using Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Testing
{
    /// <summary>
    /// Fluent assertion extensions for <see cref="RuleResult">.
    /// Provides a built-in, dependency-free way to assert rule behavior in tests.
    /// </summary>
    public static class RuleResultAssertions
    {
        // ==================== SUCCESS / FAILURE ====================

        /// <summary>
        /// Asserts that the rule passed (Success is true and IsActive is true).
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when the rule did not pass.</exception>
        public static RuleResult ShouldPass(this RuleResult result)
        {
            if (!result.IsActive)
                throw new RuleAssertionException($"Expected rule '{result.RuleDescription}' to pass, but it was inactive.");

            if (!result.Success)
            {
                var reason = result.Exception != null
                    ? $"Exception: {result.Exception.Message}"
                    : result.FirstFailure.HasValue
                        ? $"First failure: {result.FirstFailure.Value.RuleDescription}"
                        : "Rule expression evaluated to false";

                throw new RuleAssertionException($"Expected rule '{result.RuleDescription}' to pass, but it failed. {reason}");
            }

            return result;
        }

        /// <summary>
        /// Asserts that the rule failed.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when the rule did not fail.</exception>
        public static RuleResult ShouldFail(this RuleResult result)
        {
            if (result.Success && result.IsActive)
                throw new RuleAssertionException($"Expected rule '{result.RuleDescription}' to fail, but it passed.");

            return result;
        }

        /// <summary>
        /// Asserts that the rule was inactive (skipped).
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when the rule was active.</exception>
        public static RuleResult ShouldBeInactive(this RuleResult result)
        {
            if (result.IsActive)
                throw new RuleAssertionException($"Expected rule '{result.RuleDescription}' to be inactive, but it was active.");

            return result;
        }

        // ==================== VALUE ASSERTIONS ====================

        /// <summary>
        /// Asserts that the rule produced the expected return value.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <exception cref="RuleAssertionException">Thrown when the value does not match.</exception>
        public static RuleResult ShouldHaveValue(this RuleResult result, object? expected)
        {
            if (!Equals(result.Value, expected))
                throw new RuleAssertionException(
                    $"Expected rule '{result.RuleDescription}' to return '{expected}', but got '{result.Value}'.");

            return result;
        }

        /// <summary>
        /// Asserts that the rule produced a non-null value.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when the value is null.</exception>
        public static RuleResult ShouldHaveValue(this RuleResult result)
        {
            if (result.Value == null)
                throw new RuleAssertionException($"Expected rule '{result.RuleDescription}' to return a value, but it was null.");

            return result;
        }

        /// <summary>
        /// Asserts that the rule value is of the expected type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <exception cref="RuleAssertionException">Thrown when the value is not of the expected type.</exception>
        public static RuleResult ShouldHaveValueOfType<T>(this RuleResult result)
        {
            if (result.Value is not T)
                throw new RuleAssertionException(
                    $"Expected rule '{result.RuleDescription}' to return type '{typeof(T).Name}', but got '{result.Value?.GetType().Name ?? "null"}'.");

            return result;
        }

        // ==================== CHILD RULE ASSERTIONS ====================

        /// <summary>
        /// Asserts that all child rules passed.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when any child failed.</exception>
        public static RuleResult ShouldHaveAllChildrenPass(this RuleResult result)
        {
            var failures = result.AllFailures.ToList();
            if (failures.Any())
            {
                var names = string.Join(", ", failures.Select(f => f.RuleDescription));
                throw new RuleAssertionException(
                    $"Expected all children of '{result.RuleDescription}' to pass, but these failed: {names}.");
            }

            return result;
        }

        /// <summary>
        /// Asserts that at least one child rule failed.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when no children failed.</exception>
        public static RuleResult ShouldHaveChildFailure(this RuleResult result)
        {
            if (!result.ChildResults.Any(r => !r.Success))
                throw new RuleAssertionException(
                    $"Expected '{result.RuleDescription}' to have at least one failing child, but all passed.");

            return result;
        }

        /// <summary>
        /// Asserts that the rule has the expected number of child results.
        /// </summary>
        /// <param name="expectedCount">Expected number of child results.</param>
        /// <exception cref="RuleAssertionException">Thrown when the count does not match.</exception>
        public static RuleResult ShouldHaveChildCount(this RuleResult result, int expectedCount)
        {
            if (result.ChildResults.Count != expectedCount)
                throw new RuleAssertionException(
                    $"Expected '{result.RuleDescription}' to have {expectedCount} child results, but found {result.ChildResults.Count}.");

            return result;
        }

        /// <summary>
        /// Asserts that a child rule with the given description exists and returns it for further assertions.
        /// </summary>
        /// <param name="description">The child rule description to find.</param>
        /// <returns>The matching child result for chaining.</returns>
        /// <exception cref="RuleAssertionException">Thrown when no matching child is found.</exception>
        public static RuleResult ShouldHaveChild(this RuleResult result, string description)
        {
            var child = result.ChildResults.FirstOrDefault(r =>
                r.RuleDescription.Equals(description, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(child.RuleDescription))
                throw new RuleAssertionException(
                    $"Expected '{result.RuleDescription}' to have a child '{description}', but it was not found. Available: {string.Join(", ", result.ChildResults.Select(r => r.RuleDescription))}.");

            return child;
        }

        // ==================== EXCEPTION ASSERTIONS ====================

        /// <summary>
        /// Asserts that the rule threw an exception during execution.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when no exception was captured.</exception>
        public static RuleResult ShouldHaveThrown<T>(this RuleResult result) where T : Exception
        {
            if (result.Exception == null)
                throw new RuleAssertionException(
                    $"Expected rule '{result.RuleDescription}' to throw '{typeof(T).Name}', but no exception was thrown.");

            if (result.Exception is not T)
                throw new RuleAssertionException(
                    $"Expected rule '{result.RuleDescription}' to throw '{typeof(T).Name}', but got '{result.Exception.GetType().Name}'.");

            return result;
        }

        /// <summary>
        /// Asserts that the rule did NOT throw an exception.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when an exception was captured.</exception>
        public static RuleResult ShouldNotHaveThrown(this RuleResult result)
        {
            if (result.Exception != null)
                throw new RuleAssertionException(
                    $"Expected rule '{result.RuleDescription}' to not throw, but it threw '{result.Exception.GetType().Name}': {result.Exception.Message}.");

            return result;
        }

        // ==================== COLLECTION ASSERTIONS (Workflow results) ====================

        /// <summary>
        /// Asserts that all results in the collection passed.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when any result failed.</exception>
        public static IEnumerable<RuleResult> ShouldAllPass(this IEnumerable<RuleResult> results)
        {
            var list = results.ToList();
            var failures = list.Where(r => !r.Success && r.IsActive).ToList();
            if (failures.Any())
            {
                var names = string.Join(", ", failures.Select(f => $"'{f.RuleDescription}'"));
                throw new RuleAssertionException($"Expected all rules to pass, but these failed: {names}.");
            }

            return list;
        }

        /// <summary>
        /// Asserts that at least one result in the collection failed.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when all passed.</exception>
        public static IEnumerable<RuleResult> ShouldHaveAnyFailure(this IEnumerable<RuleResult> results)
        {
            var list = results.ToList();
            if (list.All(r => r.Success || !r.IsActive))
                throw new RuleAssertionException("Expected at least one rule to fail, but all passed.");

            return list;
        }

        /// <summary>
        /// Asserts that the collection contains the expected number of results.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when the count does not match.</exception>
        public static IEnumerable<RuleResult> ShouldHaveCount(this IEnumerable<RuleResult> results, int expected)
        {
            var list = results.ToList();
            if (list.Count != expected)
                throw new RuleAssertionException($"Expected {expected} results, but got {list.Count}.");

            return list;
        }

        /// <summary>
        /// Returns the result for the rule with the given description for further assertions.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when no matching result is found.</exception>
        public static RuleResult ShouldContainRule(this IEnumerable<RuleResult> results, string description)
        {
            var foundResult = results.FirstOrDefault(r =>
                r.RuleDescription.Equals(description, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(foundResult.RuleDescription))
                throw new RuleAssertionException(
                    $"Expected results to contain rule '{description}', but it was not found. Available: {string.Join(", ", results.Select(r => r.RuleDescription))}.");

            return foundResult;
        }
    }
}
