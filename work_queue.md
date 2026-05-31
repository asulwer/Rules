# Work Queue & Backlog

Merged backlog and active work queue for the Rules engine project.

## ✅ Completed

| # | Feature | Status |
|---|---------|--------|
| 1 | Rule result streaming | Done — CancellationToken, IAsyncEnumerable, ExecuteBufferedAsync |
| 7 | Rule action chaining | Done — DependsOnRuleId, topological sort, cycle detection, RuleContext |
| 12 | Rule testing framework | Done — 15 fluent assertions, RuleTest builder, test suites |
| 13 | Examples, use cases & docs | Done — 6 detailed guides |

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |

## 📋 Backlog (Future Ideas)

### Rule Result Caching (Memoization)
Cache rule evaluation results by `(ruleId, parameterHash)` to skip re-evaluation when the same inputs are seen again.

**Use case:** High-frequency evaluation of the same customer/transaction against unchanged rules.

### Expression Compilation Cache
Cache compiled delegates by `(expressionString, parameterType)` key to avoid recompiling identical expressions.

**Use case:** Multiple rules or workflows share the same expression. Compile once, reuse everywhere.

### BenchmarkDotNet Suite
Performance measurement harness.

**Metrics:** Compilation time, execution throughput, memory allocations, parallel vs sequential speedup, async overhead.

### Rule Dependency Graph
Visualize rule relationships (structural parent-child + data-flow dependencies). Generate DOT format for Graphviz.

### Hot-Reload Rules
Swap rule expressions at runtime without restarting the application.

### Rule Metrics
Track evaluation count, average execution time, failure rate per rule.

### Pre-Compiled Rule Library
Ship common rules (age validation, email format, etc.) as pre-compiled assemblies.

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Done
- [-] Excluded
