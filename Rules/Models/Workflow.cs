using Rules.Compiler;
using Rules.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rules.Models
{
    /// <summary>
    /// A workflow is a container for a collection of rules that are evaluated together.
    /// Owns an ExpressionCompiler used to compile all contained rules.
    /// Supports both sequential and parallel execution of independent rules.
    /// Supports both synchronous and asynchronous expressions.
    /// </summary>
    public class Workflow
    {
        private ExpressionCompiler _compiler = new ExpressionCompiler();

        /// <summary>
        /// EF Core requires a parameterless constructor.
        /// </summary>
        public Workflow()
        {
        }

        /// <summary>
        /// Unique identifier for the workflow.
        /// </summary>
        [Key] public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Human-readable description of the workflow.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When false, the entire workflow and its rules are skipped.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Top-level rules in this workflow. Child rules are nested inside their parents.
        /// </summary>
        public virtual IList<Rule> Rules { get; set; } = new List<Rule>();

        // ==================== VALIDATION ====================

        /// <summary>
        /// Validates the entire workflow and all contained rules.
        /// Checks workflow consistency and delegates to each rule&apos;s Validate method.
        /// Call before Compile to catch errors early.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when workflow or rule validation fails.</exception>
        public void Validate()
        {
            // 1. Workflow must have at least one active rule.
            var activeRules = Rules.Where(r => r.IsActive).ToList();
            if (activeRules.Count == 0)
            {
                throw new WorkflowException(
                    $"Workflow &apos;{Description}&apos; (Id: {Id}) has no active rules.");
            }

            // 2. Validate each top-level rule.
            foreach (var rule in activeRules)
            {
                rule.Validate();
            }

            // 3. Detect duplicate rule IDs within this workflow.
            var ids = activeRules.Select(r => r.Id).ToList();
            var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                throw new DuplicateRuleIdException(duplicates.ToArray());

            }
        }

        // ==================== COMPILATION ====================

        /// <summary>
        /// Compiles all active rules in this workflow using the shared ExpressionCompiler.
        /// After compilation, rule properties become immutable.
        /// Call once after workflow creation or when rules change.
        /// </summary>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        public void Compile(RuleParameter[] parameters, string[]? additionalNamespaces = null)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                rule.Compile(_compiler, parameters, additionalNamespaces);
            }
        }

        // ==================== SYNCHRONOUS EXECUTION ====================

        /// <summary>
        /// Executes all active rules sequentially, yielding a RuleResult for each.
        /// Does not short-circuit; every active rule is evaluated.
        /// Use this when rules are simple and overhead of parallelism isn&apos;t worth it.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Enumerable of results, one per evaluated rule.</returns>
        public IEnumerable<RuleResult> Execute(params RuleParameter[] parameters)
        {
            if (!IsActive)
                yield break;

            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                yield return rule.Execute(parameters);
            }
        }

        /// <summary>
        /// Executes all active rules in parallel for maximum throughput.
        /// Rules run concurrently; results are returned in rule order.
        /// Child rules within a parent still execute sequentially (bottom-up dependency).
        /// Use this when rules are complex, numerous, or CPU-intensive.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Array of results in rule order.</returns>
        public RuleResult[] ExecuteParallel(params RuleParameter[] parameters)
        {
            if (!IsActive)
                return Array.Empty<RuleResult>();

            var activeRules = Rules.Where(r => r.IsActive).ToArray();
            if (activeRules.Length == 0)
                return Array.Empty<RuleResult>();

            // Pre-allocate result array to maintain rule order.
            var results = new RuleResult[activeRules.Length];

            // Execute independent rules in parallel.
            Parallel.For(0, activeRules.Length, i =>
            {
                results[i] = activeRules[i].Execute(parameters);
            });

            return results;
        }

        // ==================== ASYNCHRONOUS EXECUTION ====================

        /// <summary>
        /// Executes all active rules asynchronously, yielding a RuleResult for each.
        /// Properly awaits async expressions in rules.
        /// Use this when rules contain async I/O (database lookups, HTTP calls).
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Enumerable of async results, one per evaluated rule.</returns>
        public async IAsyncEnumerable<RuleResult> ExecuteAsync(params RuleParameter[] parameters)
        {
            if (!IsActive)
                yield break;

            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                yield return await rule.ExecuteAsync(parameters);
            }
        }

        /// <summary>
        /// Executes all active rules in parallel asynchronously for maximum throughput.
        /// Rules run concurrently; results are returned in rule order.
        /// Properly awaits async expressions in rules.
        /// Use this for maximum performance with async I/O-bound rules.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Array of results in rule order.</returns>
        public async Task<RuleResult[]> ExecuteParallelAsync(params RuleParameter[] parameters)
        {
            if (!IsActive)
                return Array.Empty<RuleResult>();

            var activeRules = Rules.Where(r => r.IsActive).ToArray();
            if (activeRules.Length == 0)
                return Array.Empty<RuleResult>();

            // Execute all rules concurrently via Task.WhenAll.
            var tasks = activeRules.Select(rule => rule.ExecuteAsync(parameters)).ToArray();
            return await Task.WhenAll(tasks);
        }
    }
}
