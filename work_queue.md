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

## 🔄 Active

| Priority | Task | Description |
|----------|------|-------------|
| **1** | Fix all build/test warnings | CI warnings, nullable refs, xUnit analyzers |
| **2** | Update git remote | Local repo still pushes to old `asulwer/Rules` URL |
| **3** | XML doc comments | Public APIs need IntelliSense documentation |
| **4** | README polish | Installation section, NuGet badge, remaining `Rules` refs |
| **5** | Benchmarks | BenchmarkDotNet vs Microsoft RulesEngine |
| **6** | Strong-name signing | Verify signing actually works or remove |

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
