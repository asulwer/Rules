# RoslynRules

```bash
dotnet add package RoslynRules
```

A high-performance rewrite of [Microsoft RulesEngine](https://github.com/microsoft/RulesEngine) and its maintained [fork](https://github.com/asulwer/RulesEngine). Built for speed, with a focus on zero-overhead execution, compile-time validation, and modern .NET patterns.

📖 **Documentation:** [https://asulwer.github.io/RoslynRules/](https://asulwer.github.io/RoslynRules/)

## Key Differences

| Feature | RulesEngine | This Rewrite |
|---------|-------------|--------------|
| Expression compiler | `System.Linq.Dynamic.Core` | **Roslyn (Microsoft.CodeAnalysis.CSharp)** |
| Delegate invocation | `ExpressionParser.Parse` (slow) | **Typed delegates** (direct call) |
| Parameters | Multi-parameter | **Single parameter** (wrap multiples in structs) |
| Async support | None | **Async/await in expressions** |
| Validation | Runtime only | **Compile-time validation** |
| Circular reference guard | No | **Built-in tree validation** |
| Thread safety | Mutable rules | **Immutable after compilation** |
| Execution modes | Sequential | **Sequential + Parallel + Async + Streaming** |
| Rule chaining | No | **DependsOnRuleId with topological sort** |

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
using RoslynRules.Extensions;

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

RoslynRules uses typed exceptions for clear error handling:

| Exception | When Thrown |
|-----------|-------------|
| `RuleValidationException` | Rule has no Expression, Action, or active children |
| `CircularReferenceException` | Circular reference in child rule tree or dependency chain |
| `SyntaxErrorException` | Invalid C# syntax in expression |
| `RuleCompilationException` | Roslyn compilation failure |
| `NotCompiledException` | Execute called before Compile |
| `RuleExecutionException` | Runtime error in compiled code |
| `RuleTimeoutException` | Rule exceeded configured `Timeout` |
| `WorkflowException` | Workflow has no active rules |
| `DuplicateRuleIdException` | Duplicate rule IDs in same workflow |

All inherit from `RulesException`.

## RuleBatch (10+ Rules Together)

Evaluate multiple rules as a unit with shared compilation context. Useful when you need to run 10+ checks against the same input.

```csharp
using RoslynRules.Batch;

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

## Rule Action Chaining

Chain rules so the output of one rule feeds into the next. Use `DependsOnRuleId` to create data-flow dependencies between independent rules.

### Parent-Child vs DependsOn

| | Parent-Child (`ParentRuleId`) | DependsOn (`DependsOnRuleId`) |
|---|---|---|
| **Relationship** | Structural nesting — child is part of parent | Data-flow — one rule reads another&apos;s output |
| **Execution** | Child runs first, then parent expression | Dependency runs first, then dependent rule |
| **Failure impact** | Parent fails if any child fails | Dependent still runs even if dependency fails |
| **Use for** | Sub-conditions that compose a larger check | Multi-stage pipelines needing earlier outputs |

### Example: Parent-Child (Structural)

```csharp
var adultCheck = new Rule
{
    Description = "Valid adult customer",
    Expression = "customer.IsAdult && customer.HasName",
    IsActive = true
};

adultCheck.ChildRules.Add(new Rule
{
    Description = "Age check",
    Expression = "customer.Age >= 18"
});

adultCheck.ChildRules.Add(new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
});
// adultCheck succeeds only if BOTH children pass
```

### Example: DependsOn (Data-Flow)

```csharp
var validateCustomer = new Rule
{
    Description = "Validate customer",
    Expression = "customer != null && customer.IsActive",
    IsActive = true
};

var checkCredit = new Rule
{
    Description = "Check credit",
    DependsOnRuleId = validateCustomer.Id,  // Runs AFTER validateCustomer
    Expression = "customer.CreditScore >= 700",
    IsActive = true
};

var workflow = new Workflow
{
    Rules = new List<Rule> { checkCredit, validateCustomer }
};

workflow.Validate();   // Validates dependencies exist, no cycles
workflow.Compile(parameters);

var results = workflow.Execute(parameters).ToList();
// Execution order: validateCustomer → checkCredit (dependency order)
```

### Accessing Dependency Results

Use `RuleContext` to read previous rule outputs:

```csharp
var taxRule = new Rule
{
    Description = "Calculate tax",
    Action = "customer.TaxAmount = customer.Amount * 0.08",
    IsActive = true
};

var totalRule = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = taxRule.Id,
    Expression = "context.GetResult(taxRule.Id).Success",
    Action = "customer.Total = customer.Amount + customer.TaxAmount",
    IsActive = true
};
```

**`TryGetValue` — safer alternative to `GetValue`:**

```csharp
// GetValue returns default(T) when rule not found — ambiguous for value types
int value = context.GetValue<int>(taxRule.Id);  // 0 if not found OR value was actually 0

// TryGetValue distinguishes these cases
if (context.TryGetValue<int>(taxRule.Id, out var amount))
{
    Console.WriteLine($"Tax: {amount}");
}
else
{
    Console.WriteLine("Tax rule not found or failed");
}
```

**Validation:** `Validate()` checks all `DependsOnRuleId` references exist and detects circular dependencies.

**Execution modes:** Dependencies execute first in all modes (sequential, parallel, async, buffered). Parallel mode runs independent rules concurrently while respecting dependency chains.
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
    IsActive = true,
    Timeout = TimeSpan.FromSeconds(5)  // Optional: prevent infinite loops
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

**Compile without values:**

`RuleParameter` accepts `null` for the value — only the type matters during compilation:

```csharp
// Compile with null values — types are what matter
wf.Compile(new[]
{
    new RuleParameter("customer", typeof(Customer))  // value defaults to null
});

// Execute later with real instances
var customer = new Customer { Name = "Alice", Age = 25 };
var results = wf.Execute(new[]
{
    new RuleParameter("customer", typeof(Customer), customer)
});
```

This is useful when:
- You compile at startup and execute later with different data
- You want to avoid creating dummy objects just for compilation
- You separate compilation (needs types) from execution (needs values)
```

### 4. Execute

```csharp
// Sequential
var results = wf.Execute(parameters);

// Parallel (CPU-intensive rules)
var results = wf.ExecuteParallel(parameters);

// Async streaming with cancellation
await foreach (var result in wf.ExecuteAsync(parameters, cts.Token))
{
    if (!result.Success) break; // Short-circuit
}

// Parallel async with cancellation
var results = await wf.ExecuteParallelAsync(parameters, cts.Token);

// Buffered streaming (chunked results for large rule sets)
await foreach (var chunk in wf.ExecuteBufferedAsync(parameters, bufferSize: 10))
{
    ProcessBatch(chunk);
}
```

## Supported Delegate Signatures

RoslynRules supports exactly **one input parameter**. Return multiple values by wrapping them in a struct or class.

| Type | Signature | Example |
|------|-----------|---------|
| Expression | `Func<TParam, bool>` | `Func<Customer, bool>` |
| Expression with composite return | `Func<TParam, TReturn>` | `Func<Customer, ValidationResult>` |
| Action | `Action<TParam>` | `Action<Customer>` |
| Async Expression | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| Async Action | `Func<TParam, Task>` | `Func<Customer, Task>` |

## Async Expressions

RoslynRules automatically detects `await` in expressions and compiles to async delegates.

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

**`ValidateAll()` — non-throwing alternative:**

```csharp
ValidationError[] errors = wf.ValidateAll();
if (errors.Any())
{
    foreach (var error in errors)
        Console.WriteLine($"[{error.ErrorType}] {error.Message}");
}
```

| Error Type | When |
|-----------|------|
| `NoActiveRules` | Workflow has no active rules |
| `EmptyRule` | Rule has no Expression, Action, or active children |
| `CircularReference` | Circular dependency in child rules or `DependsOnRuleId` |
| `SyntaxError` | Invalid C# syntax in expression or action |
| `DuplicateRuleId` | Duplicate rule IDs in same workflow |
| `MissingDependency` | `DependsOnRuleId` points to non-existent rule |
| `General` | Other validation failure |

## Per-Rule Timeout

Prevent infinite loops or blocking I/O from hanging rule execution:

```csharp
var rule = new Rule
{
    Expression = "customer.Age >= 18",
    Timeout = TimeSpan.FromSeconds(5)  // Fail after 5 seconds
};
```

When a rule exceeds its timeout, `RuleTimeoutException` is thrown with the rule ID and configured timeout duration. Timeout is immutable after `Compile()`.

## Compile-Time Immutability

After `Compile()`, rule properties are locked. Attempting to modify throws:

```csharp
wf.Compile(parameters);
rule.Description = "New name";  // InvalidOperationException!
```

## Logging

RoslynRules supports `Microsoft.Extensions.Logging`. Set `rule.Logger` to any `ILogger` implementation (Serilog, NLog, etc.):

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

## Lifecycle Events

Attach event handlers to rules for custom logic before and after execution.

### OnRuleExecuting

Fires before a rule evaluates. Set `Cancel = true` to skip execution.

```csharp
var rule = new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18"
};

rule.OnRuleExecuting += (sender, args) =>
{
    // Inspect or modify execution
    Console.WriteLine($"About to execute: {args.Rule.Description}");
    
    // Skip evaluation entirely
    if (args.Parameters[0].Value is Customer c && c.Name == "SkipMe")
    {
        args.Cancel = true;
        args.CancelReason = "Customer opted out";
    }
};
```

**When cancelled:**
- Rule returns `Success = true` (skipped, not failed)
- `OnRuleExecuted` still fires with cancellation info
- Optional `CancelReason` is embedded as `OperationCanceledException`

### OnRuleExecuted

Fires after a rule completes — success, failure, or cancellation.

```csharp
rule.OnRuleExecuted += (sender, args) =>
{
    Console.WriteLine($"Rule {args.Rule.Description}: {(args.Result.Success ? "PASS" : "FAIL")}");
    Console.WriteLine($"Elapsed: {args.Elapsed.TotalMilliseconds}ms");
    
    if (args.Exception != null)
    {
        Console.WriteLine($"Exception: {args.Exception.Message}");
    }
};
```

**Event args:**
- `args.Rule` — The rule that executed
- `args.Result` — Full `RuleResult` (Success, Value, Exception, etc.)
- `args.Elapsed` — Execution time (excluding child rule evaluation)
- `args.Exception` — Exception if execution failed

**Note:** `OnRuleExecuted` does NOT fire when an unhandled exception propagates to the caller. Use the `ILogger` integration (Event ID 1004) to capture runtime errors.

### Use Cases

| Use Case | Handler |
|----------|---------|
| Audit logging | `OnRuleExecuted` — write result to audit table |
| Circuit breaker | `OnRuleExecuting` — cancel if service is unhealthy |
| Metrics | `OnRuleExecuted` — record timing to Prometheus |
| Conditional skip | `OnRuleExecuting` — cancel for feature flags |
| Debugging | Both — trace rule evaluation flow |

### Child Rule Events

Child rules fire their own events independently. Subscribe to each child separately:

```csharp
parent.ChildRules.Add(child);

child.OnRuleExecuting += (s, e) => { /* fires when child evaluates */ };
parent.OnRuleExecuting += (s, e) => { /* fires when parent evaluates */ };
```

## ExpandoObject Support

RoslynRules supports `ExpandoObject` via `dynamic` expressions. Useful when the data shape is not known at compile time.

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
using RoslynRules.Testing;

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

## Dependency Injection & Mocking

`IRuleEngine` abstraction enables DI registration and unit testing with mocks.

```csharp
using RoslynRules.Abstractions;

// Register in DI container
services.AddSingleton<IRuleEngine, Workflow>();

// Or use RuleBatch for high-throughput scenarios
services.AddSingleton<IRuleEngine, RuleBatch>();
```

**Mocking with Moq:**

```csharp
var mockEngine = new Mock<IRuleEngine>();
mockEngine.Setup(x => x.Execute(It.IsAny<RuleParameter[]>()))
    .Returns(new[] { new RuleResult { Success = true } });

// Use mock in your service tests
var service = new MyService(mockEngine.Object);
```

**Available methods on `IRuleEngine`:**

| Method | Returns | Use Case |
|--------|---------|----------|
| `Compile(params, namespaces?)` | `void` | One-time compilation |
| `Execute(params)` | `IEnumerable<RuleResult>` | Sequential execution |
| `ExecuteAsync(params, ct)` | `IAsyncEnumerable<RuleResult>` | Streaming async |
| `ExecuteParallel(params)` | `RuleResult[]` | CPU-bound parallel |
| `ExecuteParallelAsync(params, ct)` | `Task<RuleResult[]>` | Async parallel |
| `Validate()` | `void` | Pre-compile validation (throws) |
| `ValidateAll()` | `ValidationError[]` | Pre-compile validation (returns) |

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
RoslynRules/
├── Abstractions/
│   └── IRuleEngine.cs       # Interface for DI and mocking
├── Batch/
│   └── RuleBatch.cs         # Batch evaluation (sealed, 10+ rules)
├── Compiler/
│   ├── ExpressionCompiler.cs   # Public API: Compile(expression) -> Delegate
│   ├── CodeGenerator.cs        # Generates C# source from expression string
│   ├── AssemblyCompiler.cs     # Compiles source to assembly bytes
│   └── DelegateFactory.cs      # Loads assembly and creates typed delegate
├── Exceptions/
│   └── RulesException.cs       # Typed exception hierarchy
├── Execution/
│   ├── RuleContext.cs          # Dependency result access (GetResult, TryGetValue)
│   └── RuleExecutionContext.cs # Execution helpers
├── Extensions/
│   └── JsonRuleLoader.cs       # JSON serialization (preserves Priority, Timeout, DependsOn, Parent)
├── Models/
│   ├── Rule.cs              # Individual rule (sealed, immutable after compile)
│   ├── Workflow.cs          # Container with sequential/parallel/async execution
│   ├── RuleParameter.cs     # Parameter definition (name, type, value)
│   ├── RuleResult.cs        # Execution result (success, data)
│   ├── CompiledDelegate.cs  # Typed delegate wrappers (no DynamicInvoke)
│   ├── RuleDiagnostics.cs   # Compilation diagnostics
│   └── ValidationError.cs   # Structured validation errors with ValidationErrorType enum
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

- .NET 8.0 or .NET 9.0 (library multi-targets both)
- NuGet: `Microsoft.CodeAnalysis.CSharp`

## AOT and Trimming Compatibility

RoslynRules uses runtime reflection for delegate type construction and compiler invocation. If you publish with trimming or NativeAOT enabled, the linker may strip types that are only referenced dynamically.

**Recommended approach:** Exclude RoslynRules from trimming in your project file:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="RoslynRules" />
</ItemGroup>
```

Or exclude it from trimming entirely:

```xml
<ItemGroup>
  <TrimmableAssembly Include="RoslynRules" TrimMode="none" />
</ItemGroup>
```

Reflection-heavy methods are annotated with `[RequiresUnreferencedCode]` to produce build-time warnings when used in trimmed applications. The polyfilled attribute is included for .NET Standard 2.1 compatibility.

## Source Link

RoslynRules includes Source Link support. When you reference the NuGet package, you can step into the library source code during debugging in Visual Studio or VS Code.

## License

MIT License — see [LICENSE.txt](LICENSE.txt) for details.
