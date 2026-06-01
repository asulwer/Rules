# Work Queue

## 🔴 Critical Gaps

All critical gaps resolved. See backlog for future ideas.

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

- Source Link support (debug into library from consumer projects)
- Multi-target `net8.0` alongside `netstandard2.1`
- AOT/trimming compatibility annotations
- Localizable rule descriptions (i18n)
- `IEnumerable<T>` return type support for rules
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
