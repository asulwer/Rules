using RoslynRules.Abstractions;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RoslynRules.Execution;

namespace RoslynRules.Models
{
    /// <summary>
    /// A workflow is a container for a collection of rules that are evaluated together.
    /// Owns an ExpressionCompiler used to compile all contained rules.
    /// Supports both sequential and parallel execution of independent rules.
    /// Supports both synchronous and asynchronous expressions.
    /// Supports rule action chaining via DependsOnRuleId for data-flow dependencies.
    /// </summary>
    public class Workflow : IRuleEngine
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
        /// Also validates that dependency chains (DependsOnRuleId) contain no cycles.
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

            // 4. Validate dependency chains: no cycles, all referenced rules exist.
            ValidateDependencies();
        }

        /// <summary>
        /// Validates that all DependsOnRuleId references are valid and form no cycles.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a dependency is invalid or cyclic.</exception>
        private void ValidateDependencies()
        {
            var ruleLookup = Rules.Where(r => r.IsActive).ToDictionary(r => r.Id);
            var visited = new HashSet<Guid>();
            var recursionStack = new HashSet<Guid>();

            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                if (!visited.Contains(rule.Id))
                {
                    ValidateDependencyChain(rule, ruleLookup, visited, recursionStack);
                }
            }
        }

        /// <summary>
        /// DFS-based cycle detection for dependency chains.
        /// </summary>
        private static void ValidateDependencyChain(Rule rule, Dictionary<Guid, Rule> lookup, HashSet<Guid> visited, HashSet<Guid> recursionStack)
        {
            // Check for cycle first
            if (recursionStack.Contains(rule.Id))
            {
                throw new CircularReferenceException(rule.Id, $"Dependency chain on rule &apos;{rule.Description}&apos;");
            }

            // Already fully processed
            if (!visited.Add(rule.Id))
                return;

            recursionStack.Add(rule.Id);

            if (rule.DependsOnRuleId.HasValue)
            {
                var depId = rule.DependsOnRuleId.Value;
                if (!lookup.ContainsKey(depId))
                {
                    throw new RuleValidationException(
                        $"Rule &apos;{rule.Description}&apos; (Id: {rule.Id}) depends on rule {depId} which does not exist or is inactive.");
                }

                var depRule = lookup[depId];
                ValidateDependencyChain(depRule, lookup, visited, recursionStack);
            }

            recursionStack.Remove(rule.Id);
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

        // ==================== DEPENDENCY MANAGEMENT ====================

        /// <summary>
        /// Builds a topologically sorted list of rules respecting both Priority and DependsOnRuleId.
        /// Rules with dependencies execute after their dependencies.
        /// Within the same dependency level, higher Priority executes first.
        /// </summary>
        /// <returns>Rules in execution order.</returns>
        private List<Rule> GetExecutionOrder()
        {
            var activeRules = Rules.Where(r => r.IsActive).ToList();
            if (!activeRules.Any(r => r.DependsOnRuleId.HasValue))
            {
                // No dependencies — simple priority sort
                return activeRules.OrderByDescending(r => r.Priority).ToList();
            }

            // Kahn&apos;s algorithm for topological sort with priority
            var inDegree = new Dictionary<Guid, int>();
            var adjacency = new Dictionary<Guid, List<Guid>>();

            foreach (var rule in activeRules)
            {
                inDegree[rule.Id] = 0;
                adjacency[rule.Id] = new List<Guid>();
            }

            foreach (var rule in activeRules)
            {
                if (rule.DependsOnRuleId.HasValue && adjacency.ContainsKey(rule.DependsOnRuleId.Value))
                {
                    adjacency[rule.DependsOnRuleId.Value].Add(rule.Id);
                    inDegree[rule.Id]++;
                }
            }

            var result = new List<Rule>();
            var queue = new SortedSet<Rule>(Comparer<Rule>.Create((a, b) =>
            {
                // Higher priority first; use Id as tiebreaker for stable sort
                var priorityCompare = b.Priority.CompareTo(a.Priority);
                return priorityCompare != 0 ? priorityCompare : a.Id.CompareTo(b.Id);
            }));

            // Start with all rules that have no dependencies
            foreach (var rule in activeRules.Where(r => inDegree[r.Id] == 0))
            {
                queue.Add(rule);
            }

            while (queue.Count > 0)
            {
                var current = queue.Min;
                queue.Remove(current);
                result.Add(current);

                foreach (var neighborId in adjacency[current.Id])
                {
                    inDegree[neighborId]--;
                    if (inDegree[neighborId] == 0)
                    {
                        var neighbor = activeRules.First(r => r.Id == neighborId);
                        queue.Add(neighbor);
                    }
                }
            }

            // Cycle detection: if we didn&apos;t process all rules, there&apos;s a cycle
            if (result.Count != activeRules.Count)
            {
                var unprocessed = activeRules.First(r => !result.Any(res => res.Id == r.Id));
                throw new CircularReferenceException(unprocessed.Id, $"Dependency cycle detected at rule &apos;{unprocessed.Description}&apos;");
            }

            return result;
        }

        // ==================== SYNCHRONOUS EXECUTION ====================

        /// <summary>
        /// Executes all active rules sequentially in dependency order, yielding a RuleResult for each.
        /// Does not short-circuit; every active rule is evaluated.
        /// Rules with DependsOnRuleId execute after their dependencies.
        /// Use this when rules are simple and overhead of parallelism isn&apos;t worth it.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Enumerable of results, one per evaluated rule.</returns>
        public IEnumerable<RuleResult> Execute(params RuleParameter[] parameters)
        {
            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            foreach (var rule in orderedRules)
            {
                yield return rule.ExecuteWithContext(context, parameters);
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

            var activeRules = Rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority).ToArray();
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
        /// Executes all active rules asynchronously in dependency order, yielding a RuleResult for each.
        /// Supports cancellation to stop mid-stream.
        /// Rules with DependsOnRuleId execute after their dependencies.
        /// Properly awaits async expressions in rules.
        /// Use this when rules contain async I/O (database lookups, HTTP calls)
        /// or when consuming results via await foreach.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <param name="cancellationToken">Token to cancel execution mid-stream.</param>
        /// <returns>Enumerable of async results, one per evaluated rule.</returns>
        public async IAsyncEnumerable<RuleResult> ExecuteAsync(
            RuleParameter[] parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            foreach (var rule in orderedRules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await rule.ExecuteWithContextAsync(context, parameters);
            }
        }

        /// <summary>
        /// Executes all active rules in parallel asynchronously for maximum throughput.
        /// Independent rules run concurrently; dependent rules execute after their dependencies.
        /// Results are returned in rule order.
        /// Supports cancellation to abort before all rules complete.
        /// Properly awaits async expressions in rules.
        /// Use this for maximum performance with async I/O-bound rules.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel the parallel execution.</param>
        /// <returns>Array of results in rule order.</returns>
        public async Task<RuleResult[]> ExecuteParallelAsync(
            RuleParameter[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return Array.Empty<RuleResult>();

            var activeRules = Rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority).ToArray();
            if (activeRules.Length == 0)
                return Array.Empty<RuleResult>();

            // Separate rules with and without dependencies
            var rulesWithDeps = activeRules.Where(r => r.DependsOnRuleId.HasValue).ToList();
            var independentRules = activeRules.Where(r => !r.DependsOnRuleId.HasValue).ToArray();

            var context = new RuleContext();

            // Execute independent rules in parallel first
            var independentTasks = independentRules.Select(rule => 
                rule.ExecuteWithContextAsync(context, parameters)).ToArray();

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var independentResults = await Task.WhenAll(independentTasks);

            // Execute dependent rules sequentially after their dependencies
            var dependentResults = new List<RuleResult>();
            foreach (var rule in rulesWithDeps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Ensure dependency has been executed
                if (rule.DependsOnRuleId.HasValue && !context.HasResult(rule.DependsOnRuleId.Value))
                {
                    var depRule = activeRules.FirstOrDefault(r => r.Id == rule.DependsOnRuleId.Value);
                    if (depRule != null && !context.HasResult(depRule.Id))
                    {
                        var depResult = await depRule.ExecuteWithContextAsync(context, parameters);
                        dependentResults.Add(depResult);
                    }
                }

                var result = await rule.ExecuteWithContextAsync(context, parameters);
                dependentResults.Add(result);
            }

            // Combine and return in original rule order
            var allResults = independentResults.Concat(dependentResults).ToList();
            return activeRules.Select(r => allResults.First(ar => ar.RuleId == r.Id)).ToArray();
        }

        /// <summary>
        /// Executes rules in buffered chunks, yielding arrays of results.
        /// Rules with dependencies are executed in dependency order within each batch.
        /// Useful for processing large rule sets in batches rather than one at a time.
        /// Supports cancellation and respects priority ordering within each batch.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="bufferSize">Number of rules to evaluate per batch.</param>
        /// <param name="cancellationToken">Token to cancel the stream.</param>
        /// <returns>IAsyncEnumerable of result arrays, one chunk per yield.</returns>
        public async IAsyncEnumerable<RuleResult[]> ExecuteBufferedAsync(
            RuleParameter[] parameters,
            int bufferSize = 10,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            for (int i = 0; i < orderedRules.Count; i += bufferSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = orderedRules.Skip(i).Take(bufferSize).ToArray();
                var tasks = batch.Select(rule => rule.ExecuteWithContextAsync(context, parameters)).ToArray();
                var results = await Task.WhenAll(tasks);

                yield return results;
            }
        }
    }
}
