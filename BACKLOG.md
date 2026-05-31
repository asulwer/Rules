# Backlog

Features noted for future implementation. Not prioritized yet.

## Rule Result Caching (Memoization)

Cache rule evaluation results by (ruleId, parameterHash) to skip re-evaluation when the same inputs are seen again.

**Use case:** High-frequency evaluation of the same customer/transaction against unchanged rules. Saves CPU when rules are stable but inputs repeat.

**Implementation sketch:**
- Add `Dictionary<(Guid, int), RuleResult>` cache on Rule
- Hash parameter values for cache key
- Invalidate on Compile() or property mutation
- Optional: LRU eviction for memory-bound scenarios

## Expression Compilation Cache

Cache compiled delegates by (expressionString, parameterType) key to avoid recompiling identical expressions.

**Use case:** Multiple rules or workflows share the same expression. Compile once, reuse everywhere.

**Implementation sketch:**
- Add static `ConcurrentDictionary<string, Delegate>` in ExpressionCompiler
- Key = hash of expression + parameter type names
- Thread-safe access
- Clear on app domain unload

## BenchmarkDotNet Suite

Performance measurement harness for the Rules engine.

**Metrics to track:**
- Compilation time vs expression complexity
- Execution throughput (rules/sec)
- Memory allocations per evaluation
- Parallel vs sequential speedup ratio
- Async overhead (Task creation vs sync)

**Implementation sketch:**
- Add `Rules.Benchmarks` project
- Benchmarks for: simple rule, complex rule, 100-rule workflow, parallel execution
- CI integration to catch regressions

## Potential Additions

- Rule result streaming (IAsyncEnumerable for large rule sets)
- Hot-reload rules without restart
- Distributed evaluation (multiple nodes)
