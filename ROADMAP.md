# Work Queue

## 📋 Backlog (Remaining)

### High Value / Low Effort
- [x] `CompiledDelegate.Invoke()` null-safety (replace `!` null-forgiving operator)

### Medium Value
- [x] AOT/trimming compatibility annotations (`[RequiresUnreferencedCode]` on reflection-heavy code)
- [x] `IEnumerable<T>` return type support for rules — BLOCKED: expressions must return `bool`; requires architectural change to support selectors/transforms
- [-] CompileDefinitions API — REMOVED: wrapper method rejected in favor of existing `Compile(null)` pattern
- [ ] Rule composition/template system (placeholders like `{entity}.Age >= {minAge}`)
- [ ] Built-in rule predicates library (`Rule.IsNotNull()`, `Rule.GreaterThan()`, etc.)
- [x] Event/callback system for rule lifecycle (`OnRuleExecuting`, `OnRuleExecuted`)
  - `OnRuleExecuting`: cancellable pre-execution event
  - `OnRuleExecuted`: post-execution event with Result, Elapsed, Exception
  - Tests: `RuleLifecycleEventTests.cs` (9 tests)
  - Docs: README.md, docs/api-reference.md

### Nice-to-Have
- [ ] Localizable rule descriptions (i18n)
- [ ] `RuleResult` as `readonly record struct` (currently `struct` with nullable reference types = boxing)
- [ ] Replace reflection-based JSON Id restoration with a cleaner approach
- [ ] `AssemblyCompiler` assembly reference filtering (currently loads ALL loaded assemblies)
- [ ] Rule dependency graph visualization (Graphviz DOT)
- [ ] Rule metrics (eval count, avg time, failure rate)
- [ ] Rule result caching (memoization by parameter hash)

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |
| BenchmarkDotNet performance suite | Deferred — revisit when usage grows |
| Hot-reload rules at runtime | Deferred — complex, limited demand |
| Pre-compiled rule library (common validations) | Deferred — can be added by consumers |
| Shared `ExpressionCompiler` cache | Deferred — per-instance caching is sufficient |

## Status Legend

- [ ] Not started
- [~] In progress
- [-] Excluded
