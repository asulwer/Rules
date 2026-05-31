# Work Queue

## ✅ Completed

| # | Feature | Status |
|---|---------|--------|
| 1 | Rule result streaming | Done — CancellationToken, IAsyncEnumerable, ExecuteBufferedAsync |
| 2 | Rename to RoslynRules | Done — solution, projects, namespaces, package ID |
| 3 | NuGet packaging | Done — README, LICENSE, metadata, v1.0.1 published |
| 4 | Docs site | Done — Jekyll config, all docs updated to RoslynRules |
| 7 | Rule action chaining | Done — DependsOnRuleId, topological sort, cycle detection, RuleContext |
| 12 | Rule testing framework | Done — 15 fluent assertions, RuleTest builder, test suites |
| 13 | Examples, use cases & docs | Done — 6 detailed guides |

## ✅ All Tasks Complete

| Priority | Task | Status |
|----------|------|--------|
| **1** | Fix all build/test warnings | ✅ Clean — 0 warnings |
| **2** | Update local git remote | ✅ Done by user |
| **3** | XML doc comments | ✅ Already present on all public APIs |
| **4** | README polish | ✅ Title, install cmd, namespace refs updated |
| **6** | Strong-name signing | ✅ Correctly scoped to release builds only |

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |

## 📋 Backlog (Future Ideas)

- Expression compilation cache (reuse compiled delegates)
- Rule result caching (memoization by parameter hash)
- BenchmarkDotNet performance suite
- Rule dependency graph visualization (Graphviz DOT)
- Hot-reload rules at runtime
- Rule metrics (eval count, avg time, failure rate)
- Pre-compiled rule library (common validations)

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Done
- [-] Excluded
