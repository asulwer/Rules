# Rules

A high-performance rewrite of [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) and its maintained [fork](https://github.com/asulwer/RulesEngine). Built for speed, with a focus on zero-overhead execution, compile-time validation, and modern .NET patterns.

📖 **Documentation:** [https://asulwer.github.io/Rules/](https://asulwer.github.io/Rules/)

## Key Differences

| Feature | RulesEngine | This Rewrite |
|---------|-------------|--------------|
| Expression compiler | DynamicExpresso | **Roslyn (Microsoft.CodeAnalysis.CSharp)** |
| Delegate invocation | `DynamicInvoke` (slow) | **Typed delegates** (direct call) |
| Parameters | Multi-parameter | **Single parameter** (wrap multiples in structs) |
| Async support | None | **Async/await in expressions** |
| Validation | Runtime only | **Compile-time validation** |
| Circular reference guard | No | **Built-in tree validation** |
| Thread safety | Mutable rules | **Immutable after compilation** |
| Execution modes | Sequential | **Sequential + Parallel + Async** |

## Architecture

```
Workflow
├── Rules[] (top-level)
│   ├── Expression: "customer.Name.Contains(\"A\")"
│   ├── Action: "customer.Processed = true"
│   └── ChildRules[] (bottom-up evaluation)
│       ├── Expression: "customer.Age > 18"
│       └── Action: "customer.IsAdult = true"
```

## Partial Results (Child Rule Traceability)

When a parent rule fails, `RuleResult` includes the full child evaluation tree so you can identify exactly which rule failed:

```csharp
var result = parentRule.Execute(parameters);

if (!result.Success)
{
    Console.WriteLine($"Rule failed: {result.RuleDescription}");
    
    // Get the first failing child
    var failure = result.FirstFailure;
    Console.WriteLine($"Caused by: {failure?.RuleDescription}");
    
    // Or iterate all failures
    foreach (var fail in result.AllFailures)
    {
        Console.WriteLine($"Failed: {fail.RuleDescription} (Id: {fail.RuleId})");
    }
}
```

**RuleResult properties:**
| Property | Description |
|----------|-------------|
| `Success` | True if rule passed |
| `RuleId` | GUID of the evaluated rule |
| `RuleDescription` | Human-readable rule name |
| `IsActive` | Whether rule was active |
| `Value` | Return value from Action |
| `Exception` | Exception if execution failed |
| `ChildResults` | Nested results from child rules |
| `FirstFailure` | Helper: first failing child (or null) |
| `AllFailures` | Helper: all failing children |

## JSON Rule Loader

Rules and workflows can be serialized to/from JSON for configuration-driven setups:

```csharp
using Rules.Extensions;

// Save workflow to JSON
var json = JsonRuleLoader.Serialize(workflow);
File.WriteAllText("rules.json", json);

// Load workflow from JSON
var loaded = JsonRuleLoader.LoadFromFile("rules.json");
loaded.Validate();
loaded.Compile(parameters);
```

**JSON format:**
```json
{
  "description": "Customer validation",
  "isActive": true,
  "rules": [
    {
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "action": "customer.IsAdult = true",
      "isActive": true,
      "childRules": [
        {
          "description": "Name check",
          "expression": "!string.IsNullOrEmpty(customer.Name)",
          "isActive": true
        }
      ]
    }
  ]
}
```

## Custom Exceptions

Rules uses typed exceptions for clear error handling:

| Exception | When Thrown |
|-----------|-------------|
| `RuleValidationException` | Rule has no Expression, Action, or active children |
| `CircularReferenceException` | Circular reference in child rule tree |
| `SyntaxErrorException` | Invalid C# syntax in expression |
| `RuleCompilationException` | Roslyn compilation failure |
| `NotCompiledException` | Execute called before Compile |
| `RuleExecutionException` | Runtime error in compiled code |
| `WorkflowException` | Workflow has no active rules |
| `DuplicateRuleIdException` | Duplicate rule IDs in same workflow |

All inherit from `RulesException`.

## RuleBatch (10+ Rules Together)

Evaluate multiple rules as a unit with shared compilation context. Useful when you need to run 10+ checks against the same input.

```csharp
using Rules.Batch;

var batch = new RuleBatch()
    .AddRule(new Rule { Expression = "customer.Age >= 18", Description = "Adult" })
    .AddRule(new Rule { Expression = "customer.Balance > 0", Description = "Positive balance" })
    .AddRules(GetRulesFromDatabase()); // IEnumerable<Rule>

// Single compile pass for all rules
batch.Compile(parameters);

// Evaluate all at once
var results = batch.EvaluateParallel(parameters);
foreach (var result in results)
{
    Console.WriteLine($"{result.RuleDescription}: {(result.Success ? "PASS" : "FAIL")}");
}
```

**Loading rules into a batch:**

| Source | Method |
|--------|--------|
| Manual | `batch.AddRule(new Rule { ... })` |
| List | `batch.AddRules(existingRules)` |
| JSON | `batch.AddRules(JsonRuleLoader.LoadFromFile("rules.json").Rules)` |
| Database | `batch.AddRules(dbContext.Rules.Where(r => r.IsActive).ToList())` |

## Rule Priority

Control execution order with the `Priority` property. Higher values execute first.

```csharp
var workflow = new Workflow
{
    Rules =
    {
        new Rule { Expression = "true", Description = "Low", Priority = 0 },
        new Rule { Expression = "true", Description = "High", Priority = 10 },
        new Rule { Expression = "true", Description = "Medium", Priority = 5 }
    }
};

// Execution order: High → Medium → Low
var results = workflow.Execute(parameters);
```

| Priority | Execution Order |
|----------|-----------------|
| `10` | First |
| `5` | Second |
| `0` | Third (default) |
| `-5` | After defaults |

Priority is immutable after `Compile()`. Works in Workflow and RuleBatch.

## Quick Start

### 1. Define Your Model

```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Processed { get; set; }
}
```

### 2. Create Rules

```csharp
var rule = new Rule
{
    Description = "Check adult customers",
    Expression = "customer.Age >= 18",
    Action = "customer.Processed = true",
    IsActive = true
};

var childRule = new Rule
{
    Description = "Name contains A",
    Expression = "customer.Name.Contains(\"A\")"
};

rule.ChildRules.Add(childRule);
```

### 3. Create Workflow and Compile

```csharp
var wf = new Workflow
{
    Description = "Customer processing",
    Rules = new List<Rule> { rule }
};

var parameters = new RuleParameter[]
{
    new RuleParameter("customer", typeof(Customer), new Customer { Name = "Alice", Age = 25 })
};

// Validate before compiling (catches syntax errors, circular refs)
wf.Validate();

// Compile once, execute many times
wf.Compile(parameters);
```

### 4. Execute

```csharp
// Sequential
var results = wf.Execute(parameters);

// Parallel (CPU-intensive rules)
var results = wf.ExecuteParallel(parameters);

// Async (rules with await)
var results = await wf.ExecuteParallelAsync(parameters);
```

## Supported Delegate Signatures

Rules support exactly **one input parameter**. Return multiple values by wrapping them in a struct or class.

| Type | Signature | Example |
|------|-----------|---------|
| Expression | `Func<TParam, bool>` | `Func<Customer, bool>` |
| Expression with composite return | `Func<TParam, TReturn>` | `Func<Customer, ValidationResult>` |
| Action | `Action<TParam>` | `Action<Customer>` |
| Async Expression | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| Async Action | `Func<TParam, Task>` | `Func<Customer, Task>` |

## Async Expressions

Rules automatically detect `await` in expressions and compile to async delegates.

```csharp
var rule = new Rule
{
    Description = "Check external price",
    Expression = "await GetPriceAsync(customer.ProductId) > 100"
};

wf.Compile(parameters);
var results = await wf.ExecuteParallelAsync(parameters);
```

## Validation

Call `Validate()` before `Compile()` to catch errors early:

```csharp
wf.Validate();  // Throws on:
                // - Empty rules (no Expression, Action, or children)
                // - Invalid C# syntax in expressions
                // - Circular child references
                // - Duplicate rule IDs
                // - No active rules
```

## Compile-Time Immutability

After `Compile()`, rule properties are locked. Attempting to modify throws:

```csharp
wf.Compile(parameters);
rule.Description = "New name";  // InvalidOperationException!
```

## Logging

Rules supports `Microsoft.Extensions.Logging`. Set `rule.Logger` to any `ILogger` implementation (Serilog, NLog, etc.):

```csharp
// With Serilog
rule.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Automatic structured output:
// [15:35:12.345] [PASS] Adult check (Id: abc-123) — 0.042ms
```

**Event IDs for log filtering:**
- `1001` — RuleSkipped
- `1002` — RulePassed
- `1003` — RuleFailed
- `1004` — RuleError

## ExpandoObject Support

Rules supports `ExpandoObject` via `dynamic` expressions. Useful when the data shape is not known at compile time.

```csharp
dynamic customer = new ExpandoObject();
customer.Name = "Alice";
customer.Age = 25;

var parameters = new[]
{
    new RuleParameter("customer", typeof(object), customer)
};

var rule = new Rule
{
    Expression = "((dynamic)customer).Age >= 18",
    Action = "((dynamic)customer).Processed = true"
};

workflow.Compile(parameters, new[] { "System.Dynamic" });
```

**Trade-offs:**
| | Typed Object | ExpandoObject |
|--|-------------|---------------|
| Speed | Native IL (~2ms/999) | Dynamic dispatch (~20-50ms) |
| Validation | Compile-time | Runtime only |
| Safety | Property must exist | Returns null if missing |

## Rule Testing Framework

Built-in assertions for testing rules without external test libraries.

```csharp
using Rules.Testing;

// Test an individual rule
var test = RuleTest.For(rule)
    .WithInput("customer", new Customer { Age = 25, Name = "Alice" })
    .ExpectSuccess()
    .ExpectAllChildrenPass()
    .ExpectValue(true);

var result = test.Run();
```

**Fluent assertions on RuleResult:**

```csharp
var result = rule.Execute(parameters);

// Success / failure
result.ShouldPass();
result.ShouldFail();
result.ShouldBeInactive();

// Value assertions
result.ShouldHaveValue(expectedValue);
result.ShouldHaveValueOfType<int>();

// Child rule assertions
result.ShouldHaveAllChildrenPass();
result.ShouldHaveChildFailure();
result.ShouldHaveChildCount(2);
result.ShouldHaveChild("Name check").ShouldPass();

// Exception assertions
result.ShouldHaveThrown<ArgumentException>();
result.ShouldNotHaveThrown();

// Workflow result collections
var results = workflow.Execute(parameters);
results.ShouldAllPass();
results.ShouldHaveAnyFailure();
results.ShouldContainRule("Adult check").ShouldPass();
```

**Test suites:**

```csharp
var suite = new RuleTestSuite()
    .AddTest(RuleTest.For(adultRule)
        .WithInput("customer", new Customer { Age = 25 })
        .ExpectSuccess())
    .AddTest(RuleTest.For(nameRule)
        .WithInput("customer", new Customer { Name = "" })
        .ExpectFailure());

var suiteResult = suite.Run();
Console.WriteLine(suiteResult.ToString());
// Rule Test Suite: 2 passed, 0 failed (2 total)
//   ✅ PASS Adult check
//   ✅ PASS Name check

suiteResult.ThrowOnFailure(); // Throws if any test failed
```

**Custom assertions:**

```csharp
var test = RuleTest.For(rule)
    .WithInput("customer", customer)
    .Assert(r => {
        customer.IsAdult.Should().BeTrue(); // Any assertion logic
        return r;
    });
```

## Performance

Typical execution for 999 customers:

| Mode | Time | Per Customer |
|------|------|-------------|
| Sequential | 2ms | 0.002ms |
| Parallel | 3ms | 0.003ms |
| Validation | 46ms | One-time |
| Compilation | 812ms | One-time, cached |

## Project Structure

```
Rules/
├── Models/
│   ├── Rule.cs              # Individual rule with Expression/Action/Children
│   ├── Workflow.cs          # Container with sequential/parallel/async execution
│   ├── RuleParameter.cs     # Parameter definition (name, type, value)
│   ├── RuleResult.cs        # Execution result (success, data)
│   └── CompiledDelegate.cs  # Typed delegate wrappers (no DynamicInvoke)
├── Compiler/
│   ├── ExpressionCompiler.cs   # Public API: Compile(expression) -> Delegate
│   ├── CodeGenerator.cs        # Generates C# source from expression string
│   ├── AssemblyCompiler.cs     # Compiles source to assembly bytes
│   └── DelegateFactory.cs      # Loads assembly and creates typed delegate
├── Testing/
│   ├── RuleResultAssertions.cs # Fluent assertions for RuleResult
│   ├── RuleTest.cs             # Declarative test builder for rules/workflows
│   └── RuleTestSuite.cs        # Test suite runner with aggregated results
└── Utilities/
    └── Execute.cs           # Execution helpers
```

## EF Core Ready

Models include parameterless constructors, virtual collections, and navigation properties for Entity Framework:

```csharp
public class RulesDbContext : DbContext
{
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<Rule> Rules => Set<Rule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure JSON storage for child rules
        modelBuilder.Entity<Rule>()
            .Property(r => r.ChildRules)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Rule>>(v, (JsonSerializerOptions?)null) ?? new List<Rule>());
    }
}
```

## Requirements

- .NET Standard 2.1 (library)
- .NET 8+ (demo project)
- NuGet: `Microsoft.CodeAnalysis.CSharp`

## License

MIT License — see [LICENSE.txt](LICENSE.txt) for details.
