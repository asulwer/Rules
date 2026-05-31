# Work Queue

## 🔴 Critical Gaps

| # | Gap | Why It Matters | Effort |
|---|-----|---------------|--------|
| 1 | **Shared `ExpressionCompiler` cache** | Each `Workflow`/`RuleBatch` creates its own compiler — same expressions compile multiple times, wasting ~800ms per compile | Low |
| 2 | **`CompiledDelegate.Invoke()` deadlocks async** | `.GetAwaiter().GetResult()` on async delegates = deadlock risk in UI/ASP.NET synchronization contexts | Medium |
| 3 | **No per-rule timeout** | Infinite loops or blocking I/O in rule expressions hang forever — no way to recover | Medium |

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |
| BenchmarkDotNet performance suite | Deferred |
| Hot-reload rules at runtime | Deferred |
| Pre-compiled rule library (common validations) | Deferred |

## 📋 Backlog (Future Ideas)

- Expression compilation cache (reuse compiled delegates)
- Rule result caching (memoization by parameter hash)
- Rule dependency graph visualization (Graphviz DOT)
- Rule metrics (eval count, avg time, failure rate)

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Done
- [-] Excluded
