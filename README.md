# RoslynRules

> Turn business rules into compiled C# delegates — JSON-configurable, type-safe, and fast.

```bash
dotnet add package RoslynRules
```

📖 **Full docs:** [asulwer.github.io/RoslynRules](https://asulwer.github.io/RoslynRules/)

---

## What it does

You write rules in JSON or C#. RoslynRules compiles them to native IL delegates using the actual Roslyn compiler. No expression tree interpreters. No reflection at runtime.

The result: rules execute at near-hand-coded speed, with full compile-time validation, async support, and dependency chaining between rules.

```csharp
var workflow = new Workflow
{
    Rules =
    {
        new Rule
        {
            Description = "Adult customer",
            Expression = "customer.Age >= 18",
            Action = "customer.Processed = true"
        }
    }
};

workflow.Compile(parameters);
var results = workflow.Execute(parameters);
```

| Instead of... | RoslynRules does... |
|--------------|---------------------|
| Interpreting expressions at runtime | **Compiles to typed delegates** (Roslyn → IL) |
| No validation until execution fails | **Compile-time syntax + semantic checks** |
| Sequential-only execution | **Sequential, parallel, async, and streaming** |
| No rule dependencies | **Topological sort + `DependsOnRuleId` chaining** |
| Rules mutate after creation | **Immutable after `Compile()`** |
| No built-in predicates | **25+ static factory methods** (`IsNotNull`, `GreaterThan`, etc.) |
| No caching | **Per-rule memoization** with TTL |

## Quick start

### 1. Define your model

```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Processed { get; set; }
}
```

### 2. Create a rule

```csharp
var rule = new Rule
{
    Description = "Check adult customers",
    Expression = "customer.Age >= 18",
    Action = "customer.Processed = true"
};
```

### 3. Compile and run

```csharp
var parameters = new[]
{
    new RuleParameter("customer", typeof(Customer), new Customer { Name = "Alice", Age = 25 })
};

workflow.Compile(parameters);

// Run once
var result = rule.Execute(parameters);
Console.WriteLine(result.Success); // True

// Run a thousand times — zero recompilation
foreach (var customer in customers)
{
    var results = workflow.Execute(new[]
    {
        new RuleParameter("customer", typeof(Customer), customer)
    });
}
```

### Or load rules from JSON

```json
{
  "description": "Customer validation",
  "rules": [
    {
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "action": "customer.IsAdult = true"
    }
  ]
}
```

```csharp
var workflow = JsonRuleLoader.LoadFromFile("rules.json");
workflow.Compile(parameters);
```

## Multi-Parameter Rules

Rules can accept multiple parameters directly — no wrapper struct needed.

```csharp
var rule = new Rule
{
    Description = "Price check",
    Expression = "price > 0 && quantity > 0",
    IsActive = true
};

var parameters = new[]
{
    new RuleParameter("price", typeof(decimal), 9.99m),
    new RuleParameter("quantity", typeof(int), 5)
};

rule.Compile(compiler, parameters);
var result = rule.Execute(parameters);
```

## What makes it different

**Not an interpreter.**
Most rules engines parse expressions into trees and walk them every execution. RoslynRules emits real C# assemblies via `Microsoft.CodeAnalysis.CSharp`. The first compile takes ~50ms. Every call after that is a direct delegate invocation — nanoseconds, not milliseconds.

**Compile once, run forever.**
The `ExpressionCompiler` caches delegates in a `ConcurrentDictionary`. Compile a rule, and you get back the same delegate on subsequent calls. No recompilation, no assembly bloat.

**Rules can depend on each other.**
Use `DependsOnRuleId` to build pipelines where one rule reads another's output. Dependencies are validated at compile time (no missing references) and resolved with topological sorting.

```csharp
var validate = new Rule
{
    Description = "Validate",
    Expression = "customer.IsActive"
};

var process = new Rule
{
    Description = "Process",
    DependsOnRuleId = validate.Id,
    Expression = "context.GetResult(validate.Id).Success"
};
```

**Async without ceremony.**
Expressions with `await` are auto-detected and compiled to async delegates. No manual `Task` wrapping.

```csharp
new Rule
{
    Expression = "await GetPriceAsync(customer.ProductId) > 100"
};
```

**Memory-safe by design.**
Compiled assemblies live in collectible `AssemblyLoadContexts`. After 1000 compilations (configurable), the ALC unloads and a fresh one takes over. No permanent assembly accumulation in long-running apps.

## When to use it

| Scenario | Why RoslynRules |
|----------|----------------|
| Business rules that change frequently | JSON-driven, no redeploy needed |
| High-throughput validation (10K+ items/sec) | Compiled delegates, parallel execution |
| Multi-stage approval workflows | `DependsOnRuleId` with dependency resolution |
| Rules with external API calls | Native async/await support |
| Compliance/audit requirements | Per-rule logging, lifecycle events, structured results |

## Safety

RoslynRules compiles arbitrary C# expressions. By default, the compiler only references a whitelist of safe assemblies (`System.Runtime`, `System.Linq`, your app assembly). Dangerous namespaces like `System.IO` and `System.Net` are excluded.

Never compile expressions from untrusted sources without additional validation. See [SECURITY.md](SECURITY.md) for hardening guidance.

## Requirements

- .NET 8.0 or .NET 9.0 (multi-targeted)
- `Microsoft.CodeAnalysis.CSharp` (pulled in via NuGet)

## License

MIT — see [LICENSE.txt](LICENSE.txt)
