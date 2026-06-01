# Work Queue

## ✅ Completed

| Date | Feature | Commit |
|------|---------|--------|
| 2026-05-18 | Solution rename `Rules` → `RoslynRules` | `5095ad8` |
| 2026-05-18 | NuGet v1.0.1 publish with README + LICENSE | `5095ad8` |
| 2026-05-18 | `.editorconfig` for consistent formatting | `e5f1bb5` |
| 2026-05-31 | JSON serialization fix (Priority, DependsOnRuleId, ParentRuleId, Timeout) | `bb7e533` |
| 2026-05-31 | `IRuleEngine` abstraction + tests + docs | `b0f2e7d` |
| 2026-05-31 | Async deadlock fix (`ConfigureAwait(false)`) | `8a57309` |
| 2026-05-31 | Per-rule timeout (`RuleTimeoutException`) | `329f646` |
| 2026-05-31 | Source Link support | `e3cad84` |
| 2026-05-31 | `net8.0` multi-targeting | `5436845` |

## ❌ Excluded / Won't Implement

| Feature | Reason |
|---------|--------|
| Rule versioning | SQL Server temporal tables + EF Core handle this natively |
| Distributed evaluation | Out of scope |
| BenchmarkDotNet performance suite | Deferred — revisit when usage grows |
| Hot-reload rules at runtime | Deferred — complex, limited demand |
| Pre-compiled rule library (common validations) | Deferred — can be added by consumers |
| Shared `ExpressionCompiler` cache | Deferred — `ExpressionCompiler` already caches per-instance; global cache is a micro-optimization |

## 📋 Backlog (Future Ideas)

### High Value / Low Effort
- [ ] `TryGetValue` pattern on `RuleContext` (currently returns `default(T)` on failure — ambiguous)
- [ ] Validation result aggregation (`ValidateAll()` that returns errors without throwing)
- [ ] `sealed` on non-inheritable classes (`Rule`, `Workflow`, `RuleBatch`)
- [ ] `CompiledDelegate.Invoke()` null-safety (replace `!` null-forgiving operator)

### Medium Value
- [ ] AOT/trimming compatibility annotations (`[RequiresUnreferencedCode]` on reflection-heavy code)
- [ ] `IEnumerable<T>` return type support for rules (currently only `object? Value`)
- [ ] Rule composition/template system (placeholders like `{entity}.Age >= {minAge}`)
- [ ] Built-in rule predicates library (`Rule.IsNotNull()`, `Rule.GreaterThan()`, etc.)
- [ ] Event/callback system for rule lifecycle (`OnRuleExecuting`, `OnRuleExecuted`)

### Nice-to-Have
- [ ] Localizable rule descriptions (i18n)
- [ ] `RuleResult` as `readonly record struct` (currently `struct` with nullable reference types = boxing)
- [ ] Replace reflection-based JSON Id restoration with a cleaner approach
- [ ] `AssemblyCompiler` assembly reference filtering (currently loads ALL loaded assemblies)
- [ ] Rule dependency graph visualization (Graphviz DOT)
- [ ] Rule metrics (eval count, avg time, failure rate)
- [ ] Rule result caching (memoization by parameter hash)

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Done
- [-] Excluded
