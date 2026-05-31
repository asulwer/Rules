using Rules.Compiler;
using Rules.Exceptions;
using Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rules.Batch
{
    /// <summary>
    /// A batch of rules compiled and evaluated together as a unit.
    /// Useful when evaluating 10+ rules against the same input with shared context.
    /// </summary>
    public class RuleBatch
    {
        private readonly List<Rule> _rules = new List<Rule>();
        private readonly ExpressionCompiler _compiler = new ExpressionCompiler();
        private bool _isCompiled;

        /// <summary>
        /// Rules in this batch.
        /// </summary>
        public IReadOnlyList<Rule> Rules => _rules;

        /// <summary>
        /// Adds a rule to the batch.
        /// </summary>
        public RuleBatch AddRule(Rule rule)
        {
            if (_isCompiled)
                throw new InvalidOperationException("Cannot add rules after compilation.");

            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds multiple rules to the batch.
        /// </summary>
        public RuleBatch AddRules(IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                AddRule(rule);
            return this;
        }

        /// <summary>
        /// Validates all rules in the batch.
        /// </summary>
        public void Validate()
        {
            if (!_rules.Any())
                throw new WorkflowException("Batch has no rules.");

            var ids = _rules.Select(r => r.Id).ToList();
            var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
                throw new DuplicateRuleIdException(duplicates.ToArray());

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.Validate();
            }
        }

        /// <summary>
        /// Compiles all rules in the batch using a shared ExpressionCompiler.
        /// Single compile pass for all rules.
        /// </summary>
        public void Compile(RuleParameter[] parameters, string[]? additionalNamespaces = null)
        {
            Validate();

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.Compile(_compiler, parameters, additionalNamespaces);
            }

            _isCompiled = true;
        }

        /// <summary>
        /// Evaluates all active rules sequentially.
        /// </summary>
        public IEnumerable<RuleResult> Evaluate(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                yield return rule.Execute(parameters);
            }
        }

        /// <summary>
        /// Evaluates all active rules in parallel.
        /// </summary>
        public RuleResult[] EvaluateParallel(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            var activeRules = _rules.Where(r => r.IsActive).ToArray();
            var results = new RuleResult[activeRules.Length];

            System.Threading.Tasks.Parallel.For(0, activeRules.Length, i =>
            {
                results[i] = activeRules[i].Execute(parameters);
            });

            return results;
        }

        /// <summary>
        /// Evaluates all active rules asynchronously.
        /// </summary>
        public async IAsyncEnumerable<RuleResult> EvaluateAsync(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                yield return await rule.ExecuteAsync(parameters);
            }
        }

        /// <summary>
        /// Evaluates all active rules in parallel asynchronously.
        /// </summary>
        public async Task<RuleResult[]> EvaluateParallelAsync(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            var activeRules = _rules.Where(r => r.IsActive).ToArray();
            var tasks = activeRules.Select(rule => rule.ExecuteAsync(parameters));
            var results = await Task.WhenAll(tasks);

            return results;
        }

        private void EnsureCompiled()
        {
            if (!_isCompiled)
                throw new NotCompiledException(Guid.Empty);
        }
    }
}
