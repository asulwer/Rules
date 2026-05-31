using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Testing
{
    /// <summary>
    /// A suite of rule tests that can be run together.
    /// Collects results and reports pass/fail status.
    /// </summary>
    public class RuleTestSuite
    {
        private readonly List<RuleTest> _tests = new();

        /// <summary>
        /// Adds a test to the suite.
        /// </summary>
        public RuleTestSuite AddTest(RuleTest test)
        {
            _tests.Add(test);
            return this;
        }

        /// <summary>
        /// Adds multiple tests to the suite.
        /// </summary>
        public RuleTestSuite AddTests(params RuleTest[] tests)
        {
            foreach (var test in tests)
                _tests.Add(test);
            return this;
        }

        /// <summary>
        /// Runs all tests synchronously and returns the results.
        /// </summary>
        public RuleTestSuiteResult Run()
        {
            var results = new List<RuleTestRun>();

            foreach (var test in _tests)
            {
                try
                {
                    var result = test.Run();
                    results.Add(new RuleTestRun(test.ToString(), true, result));
                }
                catch (RuleAssertionException ex)
                {
                    results.Add(new RuleTestRun(test.ToString(), false, null, ex));
                }
                catch (Exception ex)
                {
                    results.Add(new RuleTestRun(test.ToString(), false, null, ex));
                }
            }

            return new RuleTestSuiteResult(results);
        }

        /// <summary>
        /// Runs all tests asynchronously and returns the results.
        /// </summary>
        public async Task<RuleTestSuiteResult> RunAsync()
        {
            var results = new List<RuleTestRun>();

            foreach (var test in _tests)
            {
                try
                {
                    var result = await test.RunAsync();
                    results.Add(new RuleTestRun(test.ToString(), true, result));
                }
                catch (RuleAssertionException ex)
                {
                    results.Add(new RuleTestRun(test.ToString(), false, null, ex));
                }
                catch (Exception ex)
                {
                    results.Add(new RuleTestRun(test.ToString(), false, null, ex));
                }
            }

            return new RuleTestSuiteResult(results);
        }
    }

    /// <summary>
    /// Result of a single test execution.
    /// </summary>
    public readonly struct RuleTestRun
    {
        /// <summary>
        /// Name of the test.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// True if the test passed.
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// The rule result if the test ran successfully.
        /// </summary>
        public RuleResult? Result { get; }

        /// <summary>
        /// The exception if the test failed.
        /// </summary>
        public Exception? Exception { get; }

        public RuleTestRun(string testName, bool passed, RuleResult? result, Exception? exception = null)
        {
            TestName = testName;
            Passed = passed;
            Result = result;
            Exception = exception;
        }
    }

    /// <summary>
    /// Aggregated results from running a test suite.
    /// </summary>
    public readonly struct RuleTestSuiteResult
    {
        /// <summary>
        /// Individual test results.
        /// </summary>
        public IReadOnlyList<RuleTestRun> Tests { get; }

        /// <summary>
        /// Number of passed tests.
        /// </summary>
        public int PassedCount => Tests.Count(t => t.Passed);

        /// <summary>
        /// Number of failed tests.
        /// </summary>
        public int FailedCount => Tests.Count(t => !t.Passed);

        /// <summary>
        /// True if all tests passed.
        /// </summary>
        public bool AllPassed => Tests.All(t => t.Passed);

        public RuleTestSuiteResult(IEnumerable<RuleTestRun> tests)
        {
            Tests = tests.ToList();
        }

        /// <summary>
        /// Throws a RuleAssertionException if any tests failed, with a summary.
        /// </summary>
        public void ThrowOnFailure()
        {
            if (AllPassed) return;

            var failures = Tests.Where(t => !t.Passed).Select(t =>
                $"  - {t.TestName}: {t.Exception?.Message ?? "Assertion failed"}");

            throw new RuleAssertionException(
                $"Rule test suite failed. {PassedCount} passed, {FailedCount} failed:\n{string.Join("\n", failures)}");
        }

        /// <summary>
        /// Returns a formatted summary string.
        /// </summary>
        public override string ToString()
        {
            var lines = new List<string>
            {
                $"Rule Test Suite: {PassedCount} passed, {FailedCount} failed ({Tests.Count} total)"
            };

            foreach (var test in Tests)
            {
                var status = test.Passed ? "✅ PASS" : "❌ FAIL";
                lines.Add($"  {status} {test.TestName}");
                if (test.Exception != null)
                    lines.Add($"      → {test.Exception.Message}");
            }

            return string.Join("\n", lines);
        }
    }
}
