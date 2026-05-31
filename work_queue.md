# Work Queue

## 🔴 Critical Gaps

| # | Gap | Why It Matters | Effort |
|---|-----|---------------|--------|
| 1 | **`CompiledDelegate.Invoke()` deadlocks async** | `.GetAwaiter().GetResult()` on async delegates = deadlock risk in UI/ASP.NET synchronization contexts | Medium |
| 2 | **No per-rule timeout** | Infinite loops or blocking I/O in rule expressions hang forever — no way to recover | Medium |

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |
| BenchmarkDotNet performance suite | Deferred |
| Hot-reload rules at runtime | Deferred |
| Pre-compiled rule library (common validations) | Deferred |
| Shared `ExpressionCompiler` cache | Deferred |

## 📋 Backlog (Future Ideas)

- Expression compilation cache (reuse compiled delegates)
- Rule result caching (memoization by parameter hash)
- Rule dependency graph visualization (Graphviz DOT)
- Rule metrics (eval count, avg time, failure rate)
- Shared `ExpressionCompiler` cache (reuse compiled delegates across instances)

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Done
- [-] Excluded
