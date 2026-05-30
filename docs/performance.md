# Performance

## Benchmarks

Typical execution for 999 customers on .NET 8:

| Phase | Time | Per Customer | Notes |
|-------|------|-------------|-------|
| Validation | ~46ms | — | One-time, before compile |
| Compilation | ~812ms | — | One-time, cached per signature |
| Sequential Execute | ~2ms | 0.002ms | Best for few simple rules |
| Parallel Execute | ~3ms | 0.003ms | Best for many CPU-intensive rules |
| Parallel Async | ~5ms | 0.005ms | Best for async I/O rules |

## Why It's Fast

### 1. Roslyn Compilation

Expressions compile to IL once. No interpreter overhead.

### 2. Typed Delegates

```csharp
// RulesEngine (slow)
var result = delegate.DynamicInvoke(args);  // Boxing + reflection

// This engine (fast)
var result = _delegate(customer);  // Direct call, zero overhead
```

### 3. Single Parameter

One input = no array allocation per call. Wrap multiples in structs:

```csharp
// Fast
Func<Customer, bool>

// Slower (array allocation)
Func<Customer, int, bool>
```

### 4. Immutable Rules

Properties locked after `Compile()`. Zero locks, zero contention.

### 5. Parallel.For

Independent rules run on thread pool. No manual Task creation.

## Tuning Tips

| Scenario | Recommendation |
|----------|---------------|
| Few simple rules | Use `Execute()` — parallel overhead not worth it |
| Many complex rules | Use `ExecuteParallel()` |
| Rules hit database/HTTP | Use `ExecuteParallelAsync()` |
| One-time setup | Call `Validate()` + `Compile()` at startup |
| Hot path | Reuse compiled workflow, never recompile |

## Memory

- Compiled delegates: ~2KB per expression
- Rule objects: ~200 bytes each
- RuleResult: ~32 bytes per execution

Typical 100-rule workflow: ~50KB compiled, executes in microseconds.
