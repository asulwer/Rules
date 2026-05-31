---
layout: default
title: Documentation
nav_order: 1
has_children: false
---

# Rules Engine Documentation

High-performance .NET rules engine with Roslyn compilation, typed delegates, and async support.

## Menu

| Guide | Description |
|-------|-------------|
| [Getting Started](getting-started.md) | Your first rule in 5 minutes |
| [API Reference](api-reference.md) | Complete class and method documentation |
| [Examples](examples/) | Code samples and real-world scenarios |
| [Performance](performance.md) | Benchmarks and tuning tips |
| [Migration Guide](migration.md) | Moving from Microsoft RulesEngine |

## Quick Links

- [Getting Started](getting-started.md) — Your first rule in 5 minutes
- [API Reference](api-reference.md) — Complete class documentation
- [Examples](examples/) — Sample rules and workflows
- [Performance](performance.md) — Benchmarks and tuning tips
- [Migration Guide](migration.md) — Moving from Microsoft RulesEngine
- [Logging](api-reference.md#logging) — Microsoft.Extensions.Logging integration

## What Makes It Fast

| Feature | How It Works |
|---------|-------------|
| **Roslyn Compilation** | Expressions compile to IL once, execute as native code |
| **Typed Delegates** | Direct `Func<T,R>` calls — no `DynamicInvoke` overhead |
| **Single Parameter** | One input, one output — no array allocation per call |
| **Immutable Rules** | Lock after compile — zero thread contention |
| **Parallel Execution** | `Parallel.For` for independent rule evaluation |

## When to Use Which Execution Mode

| Mode | Best For |
|------|----------|
| `Execute()` | Few simple rules, low latency requirements |
| `ExecuteParallel()` | Many CPU-intensive rules |
| `ExecuteParallelAsync()` | Rules with async I/O (DB, HTTP) |
| `ExecuteAsync()` | Streaming results, async iterators |

## Example

```csharp
var rule = new Rule
{
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true"
};

var wf = new Workflow { Rules = new List<Rule> { rule } };
var param = new RuleParameter("customer", typeof(Customer), customer);

wf.Validate();
wf.Compile(new[] { param });
var results = wf.Execute(new[] { param });
```

[Get Started →](getting-started.md)
