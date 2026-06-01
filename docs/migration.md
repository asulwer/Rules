---
layout: default
title: Migration
nav_order: 5
---

[← Back to Documentation Index](index.md)

# Migration from Microsoft.RulesEngine

## Key Changes

| RulesEngine | This Engine |
|-------------|-------------|
| `new RulesEngine.RulesEngine()` | `new Workflow()` |
| `engine.ExecuteAllRulesAsync()` | `workflow.Execute()` or `workflow.ExecuteParallelAsync()` |
| `Rule.Expression` | `Rule.Expression` (same) |
| `Rule.RuleName` | `Rule.Description` |
| `Rule.SuccessEvent` | Use `Rule.Action` |
| `Rule.ErrorMessage` | Not needed — exceptions on failure |
| Multi-parameter | Single parameter only |
| `DynamicInvoke` | Typed delegates |

## Example Migration

### Before (RulesEngine)

```csharp
var rules = new[] {
    new Rule {
        RuleName = "CheckAge",
        Expression = "input1.Age >= 18"
    }
};

var engine = new RulesEngine.RulesEngine(rules);
var result = await engine.ExecuteAllRulesAsync("CheckAge", customer);
```

### After (This Engine)

```csharp
var rule = new Rule {
    Description = "CheckAge",
    Expression = "customer.Age >= 18"
};

var workflow = new Workflow {
    Rules = new List<Rule> { rule }
};

var param = new RuleParameter("customer", typeof(Customer), customer);
workflow.Validate();
workflow.Compile(new[] { param });

var results = workflow.Execute(new[] { param });
```

**Tip:** You can compile without values if you separate compilation from execution:

```csharp
// Compile at startup with just types
workflow.CompileDefinitions(new[]
{
    new RuleParameterDefinition("customer", typeof(Customer))
});

// Execute later with real instances
var results = workflow.Execute(new[]
{
    new RuleParameter("customer", typeof(Customer), customer)
});
```

## Breaking Changes

1. **Single parameter only** — Wrap multiple inputs in a struct
2. **No built-in error message** — Handle exceptions in caller
3. **Expression uses parameter name** — Not `input1`, use declared name
4. **Compile once** — Call `Compile()` before executing

## Benefits of Migrating

| Metric | Improvement |
|--------|-------------|
| Execution speed | 10-100x faster (no System.Linq.Dynamic.Core) |
| Memory | Lower allocation (single parameter) |
| Thread safety | Immutable rules, no locks |
| Validation | Catch errors before runtime |
| Async | Native async/await support |

## License

MIT License — same permissive terms as before.
