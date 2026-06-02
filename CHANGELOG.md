# Changelog

All notable changes to RoslynRules will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Rule templates (`RuleTemplate`) with placeholder-based expression substitution
- `PlaceholderKind` enum supporting Type, Identifier, and Value placeholders
- Result caching (`Rule.CacheDuration`) with automatic expiration
- `Rule.ClearCache()` for manual cache invalidation
- Security policy (`SECURITY.md`) documenting expression injection risks
- Contributing guidelines (`CONTRIBUTING.md`)

### Changed

- Tests reorganized into categorized folders (`Compiler/`, `Core/`, `Execution/`, `Integration/`, `Models/`, `WorkflowTests/`)
- `RulesException` documented correctly as inheriting from `Exception` (not `InvalidOperationException`)

### Fixed

- `ExpressionCompiler` cache race condition (now uses `ConcurrentDictionary` with atomic `GetOrAdd`)
- `Workflow.ExecuteParallel` now respects dependency chains via `GetExecutionOrder()`
- Sync timeout implementation refactored to avoid thread-pool starvation
- `DelegateFactory` null-forgiving operators replaced with explicit null checks
- `JsonRuleLoader` reflection-based ID restoration replaced with `[JsonInclude]` attributes
- Duplicate "Result Caching" section removed from README

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
