# Changelog

All notable changes to RoslynRules will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Multi-parameter rule support — rules now accept up to 16 parameters directly (no wrapper struct needed)
- `CompiledMultiParamDelegate` and `CompiledAsyncMultiParamDelegate` for multi-parameter invocation
- Static `Rule.ValidateSemantics(string, Type)` and `Rule.ValidateSemantics(string, string)` overloads for expression validation without creating a Rule instance
- Type alias resolution (`int`, `string`, `bool`, etc.) in static `ValidateSemantics`
- `Rule.TryGetValue<T>()` with `[MaybeNullWhen(false)]` attribute for null-safe value retrieval
- `RuleCache.TryGet()` with `[NotNullWhen(true)]` attribute for null-safe cache access
- ExpressionAssemblyLoadContext unload verification tests (8 stress/integration tests)
- Demo integration tests in `RoslynRules.Tests/Integration/` (JSON round-trip, template instantiation, workflow vs batch comparison, multi-parameter)
- Advanced documentation guides:
  - `docs/security.md` — AssemblyReferenceProvider hardening, sandboxing, input validation, security checklist
  - `docs/performance-tuning.md` — CacheDuration, Timeout, maxCompilesBeforeRecycle tuning, execution strategy selection
- Dependency chaining example in `docs/examples/index.md` — `DependsOnRuleId` + `RuleContext.GetValue<T>` pattern
- BenchmarkDotNet benchmarks for compile, execute, and cache performance
- Rule templates (`RuleTemplate`) with placeholder-based expression substitution
- `PlaceholderKind` enum supporting Type, Identifier, and Value placeholders
- Result caching (`Rule.CacheDuration`) with automatic expiration
- `Rule.ClearCache()` for manual cache invalidation
- Security policy (`SECURITY.md`) documenting expression injection risks
- Contributing guidelines (`CONTRIBUTING.md`)

### Changed

- `Workflow.Rules` is now `IReadOnlyList<Rule>` (via `ReadOnlyCollection`) after compilation; mutation throws `NotSupportedException`
- `Workflow.Compile()` sets `_isCompiled = true` after compiling all rules, locking the rules list
- Tests reorganized into categorized folders (`Compiler/`, `Core/`, `Execution/`, `Integration/`, `Models/`, `WorkflowTests/`)
- `RulesException` documented correctly as inheriting from `Exception` (not `InvalidOperationException`)

### Fixed

- `aot.yml` workflow missing explicit `permissions: contents: read` (GitHub security alert)
- `ExpressionCompiler` cache race condition (now uses `ConcurrentDictionary` with atomic `GetOrAdd`)
- `Workflow.ExecuteParallel` now respects dependency chains via `GetExecutionOrder()`
- Sync timeout implementation refactored to avoid thread-pool starvation
- `DelegateFactory` null-forgiving operators replaced with explicit null checks
- `JsonRuleLoader` reflection-based ID restoration replaced with `[JsonInclude]` attributes
- Duplicate "Result Caching" section removed from README
- Removed lingering EF Core references (`virtual` keyword, EF comments) from `Workflow.cs` and `Rule.cs`
- Removed `[NotMapped]`, `[Key]`, `[JsonInclude]`, `[JsonIgnore]` attributes from core models
- `Customer.cs` in Demo no longer references `System.ComponentModel.DataAnnotations`

## [1.0.0] - 2026-06-02

### Added

- Initial release with rule engine core
- `Rule`, `Workflow`, and `RuleBatch` models
- Roslyn-based expression compilation (`ExpressionCompiler`)
- Sync and async rule execution
- Child rule nesting with bottom-up evaluation
- Dependency chains via `DependsOnRuleId`
- Action expressions (`Rule.Action`)
- Rule validation (`Validate`, `ValidateAll`)
- Circular reference detection
- Timeout support (`Rule.Timeout`)
- Lifecycle events (`OnRuleExecuting`, `OnRuleExecuted`)
- Logging integration (`Microsoft.Extensions.Logging`)
- JSON serialization (`JsonRuleLoader`)
- Testing framework (`RuleTest`, `RuleTestSuite`)
- ExpandoObject parameter support
- Partial results (`Workflow.ExecuteBufferedAsync`)
- `IRuleEngine` abstraction

---

## Release Notes Template

```
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Now removed features

### Fixed
- Bug fixes

### Security
- Security-related changes
```
