---
layout: default
title: Documentation
nav_order: 1
has_children: false
---

# RoslynRules Documentation

High-performance .NET rules engine with Roslyn compilation, typed delegates, and async support.

### [Migration Guide](migration.md) — Moving from Microsoft.RulesEngine

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

## Dependency Injection

Register `IRuleEngine` in your DI container:

```csharp
using RoslynRules.Abstractions;

services.AddSingleton<IRuleEngine, Workflow>();
```

[Get Started →](getting-started.md)

## API Reference

[API Reference →](api-reference.md) — Complete API documentation organized by component:

| Section | Contents |
|---------|----------|
| [Core Models](api-reference.md#core-models) | Rule, Workflow, RuleResult, RuleParameter |
| [Execution](api-reference.md#execution--context) | RuleContext, IRuleEngine, RuleBatch |
| [Compilation](api-reference.md#compilation) | ExpressionCompiler, Delegate Types |
| [Configuration](api-reference.md#configuration--data) | JSON, Templates, Predicates |
| [Runtime Features](api-reference.md#runtime-features) | Priority, Events, Caching |
| [Exceptions](api-reference.md#exceptions--diagnostics) | Exception hierarchy, ValidationError |
