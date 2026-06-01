---
layout: default
title: API Reference
nav_order: 3
---

[← Back to Documentation Index](index.md)

# API Reference

## Rule

An individual rule with optional boolean Expression, Action, and child rules.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (auto-generated) |
| `Description` | `string` | Human-readable purpose |
| `IsActive` | `bool` | When false, rule is skipped |
| `Expression` | `string` | C# boolean expression evaluated during execution |
| `Action` | `string` | C# expression executed when rule succeeds |
| `Priority` | `int` | Execution priority (higher = first; default 0) |
| `Timeout` | `TimeSpan?` | Per-rule execution timeout (null = no timeout) |
| `DependsOnRuleId` | `Guid?` | Foreign key for rule dependency (data-flow) |
| `ParentRuleId` | `Guid?` | Foreign key for parent rule (structural) |
| `ChildRules` | `IList<Rule>` | Child rules evaluated bottom-up before parent |
| `ParentRule` | `Rule?` | Navigation to parent (EF support) |
| `DependsOnRule` | `Rule?` | Navigation to dependency rule (EF support) |
| `Workflow` | `Workflow?` | Navigation to parent workflow (EF support) |
| `Logger` | `ILogger?` | Optional logger for structured execution events |
| `OnRuleExecuting` | `event EventHandler<RuleExecutingEventArgs>?` | Fires before execution; set Cancel to skip |
| `OnRuleExecuted` | `event EventHandler<RuleExecutedEventArgs>?` | Fires after execution with result and timing |

**Note:** `Rule` is `sealed`. Properties become immutable after `Compile()`.

### Methods

#### `Validate()`

Checks rule structure and expression syntax before compilation.

```csharp
rule.Validate(); // Throws on:
                 // - Empty rules (no Expression/Action/children)
                 // - Invalid C# syntax
                 // - Circular child references
```

#### `ValidateAll()`

Validates the rule and returns all errors without throwing.

```csharp
ValidationError[] errors = rule.ValidateAll();
if (errors.Any())
{
    foreach (var error in errors)
        Console.WriteLine($"[{error.ErrorType}] {error.Message}");
}
```

#### `Compile(ExpressionCompiler, RuleParameter[], string[]?)`

Compiles expressions into typed delegates. Locks properties after compilation.

```csharp
var compiler = new ExpressionCompiler();
var parameters = new[] { new RuleParameter("c", typeof(Customer), default) };
rule.Compile(compiler, parameters);
```

#### `Execute(params RuleParameter[])`

Synchronous execution. Bottom-up: children first, then Expression, then Action.

```csharp
var result = rule.Execute(parameters);
// result.Success = true if all children pass AND Expression passes
```

#### `ExecuteAsync(params RuleParameter[])`

Asynchronous execution for rules containing `await`.

```csharp
var result = await rule.ExecuteAsync(parameters);
```

#### `ClearCache()`

Removes all cached results for this rule, forcing the next evaluation to re-execute. Thread-safe.

```csharp
rule.ClearCache();  // Force next Execute/ExecuteAsync to re-run
```

## Workflow

Container for top-level rules. Owns the ExpressionCompiler.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Description` | `string` | Human-readable purpose |
| `IsActive` | `bool` | When false, entire workflow skipped |
| `Rules` | `IList<Rule>` | Top-level rules |

### Methods

#### `Validate()`

Validates all rules and checks workflow consistency.

```csharp
workflow.Validate();
```

#### `ValidateAll()`

Validates the workflow and returns all errors without throwing.

```csharp
ValidationError[] errors = workflow.ValidateAll();
if (errors.Any())
{
    // Handle validation errors
    foreach (var error in errors)
        Console.WriteLine($"[{error.ErrorType}] {error.EntityDescription}: {error.Message}");
}
```

#### `Compile(RuleParameter[], string[]?)`

Compiles all active rules using the shared ExpressionCompiler.

```csharp
workflow.Compile(parameters);
```

**Note:** The value in `RuleParameter` is optional — you can compile with `null` values:

```csharp
// Compile with null values — types are what matter
workflow.Compile(new[]
{
    new RuleParameter("customer", typeof(Customer))  // value defaults to null
});
```

#### `Execute(params RuleParameter[])`

Sequential execution of all active rules.

```csharp
var results = workflow.Execute(parameters);
foreach (var result in results) { ... }
```

#### `ExecuteParallel(params RuleParameter[])`

Parallel execution using `Parallel.For`. Results in rule order.

```csharp
var results = workflow.ExecuteParallel(parameters);
```

#### `ExecuteAsync(params RuleParameter[])`

Async streaming execution. Returns `IAsyncEnumerable<RuleResult>`.

```csharp
await foreach (var result in workflow.ExecuteAsync(parameters)) { ... }
```

#### `ExecuteParallelAsync(params RuleParameter[])`

Concurrent async execution using `Task.WhenAll`. Respects dependency chains.

```csharp
var results = await workflow.ExecuteParallelAsync(parameters);
```

#### `ExecuteBufferedAsync(params RuleParameter[], int)`

Executes rules in buffered chunks, yielding arrays of results. Rules with dependencies are executed in dependency order within each batch. Useful for processing large rule sets in batches.

```csharp
await foreach (var batch in workflow.ExecuteBufferedAsync(parameters, bufferSize: 10))
{
    foreach (var result in batch)
    {
        Console.WriteLine($"Result: {result.Success}");
    }
}
```

## RuleResult

Result of rule execution. Includes full traceability for debugging rule chains.

```csharp
public RuleResult(
    bool success,
    Guid ruleId = default,
    string ruleDescription = "",
    bool isActive = true,
    object? value = null,
    Exception? exception = null,
    IReadOnlyList<RuleResult>? childResults = null)
```

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | True if rule passed |
| `RuleId` | `Guid` | ID of the evaluated rule |
| `RuleDescription` | `string` | Human-readable name |
| `IsActive` | `bool` | Whether rule was active (inactive = skipped) |
| `Value` | `object?` | Return value from Action delegate |
| `Exception` | `Exception?` | Runtime exception, if any |
| `ChildResults` | `IReadOnlyList<RuleResult>` | Nested child evaluation results |
| `FirstFailure` | `RuleResult?` | First failing child (null if all passed) |
| `AllFailures` | `IEnumerable<RuleResult>` | All failing children |

**Example:**
```csharp
var result = rule.Execute(parameters);

if (!result.Success)
{
    // Drill down to find the exact failure
    var culprit = result.FirstFailure;
    Console.WriteLine($"Failed at: {culprit?.RuleDescription}");
}
```

## RuleParameter

Runtime parameter passed to rules.

```csharp
public RuleParameter(string name, Type type, object? value)
```

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Parameter name used in expressions |
| `Type` | `Type` | CLR type for compilation |
| `Value` | `object?` | Runtime value |

## Exceptions

RoslynRules uses a custom exception hierarchy for clear error handling.

| Exception | Thrown When |
|-----------|-------------|
| `RuleValidationException` | Rule has no Expression, Action, or active children |
| `CircularReferenceException` | Circular reference detected in child rules or dependency chain |
| `SyntaxErrorException` | Invalid C# syntax in expression |
| `RuleCompilationException` | Roslyn compilation failure |
| `NotCompiledException` | Execute called before Compile |
| `RuleExecutionException` | Runtime error in compiled expression |
| `RuleTimeoutException` | Rule exceeded configured `Timeout` during execution |
| `WorkflowException` | Workflow has no active rules |
| `DuplicateRuleIdException` | Duplicate rule IDs in workflow |

All exceptions inherit from `RulesException` (which inherits from `Exception`).

## ExpressionCompiler

RoslynRules integrate with `Microsoft.Extensions.Logging` for structured execution events.

### Rule.Logger

```csharp
rule.Logger = loggerFactory.CreateLogger<Rule>();
```

### LogRuleExecuted Extension

```csharp
using RoslynRules.Models;

logger.LogRuleExecuted(new RuleExecutedEvent {
    RuleId = rule.Id,
    RuleDescription = rule.Description,
    IsActive = rule.IsActive,
    Success = result.Success,
    ElapsedMilliseconds = 0.042,
    Exception = null
});
```

**Output levels:**
- `LogDebug` — standard execution (PASS/SKIP)
- `LogInformation` — via `LogRuleExecutedInfo()` for always-visible output
- `LogError` — execution exceptions with stack trace

**Event IDs:**
- `1001` — RuleSkipped
- `1002` — RulePassed
- `1003` — RuleFailed
- `1004` — RuleError

## JSON Serialization

RoslynRules and workflows support JSON serialization for configuration-driven setups.

```csharp
using RoslynRules.Extensions;

// Serialize
var json = JsonRuleLoader.Serialize(workflow);

// Deserialize
var loaded = JsonRuleLoader.Deserialize(json);

// File I/O
JsonRuleLoader.SaveToFile(workflow, "rules.json");
var fromFile = JsonRuleLoader.LoadFromFile("rules.json");
```

**Example JSON:**
```json
{
  "description": "Customer workflow",
  "isActive": true,
  "rules": [
    {
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "isActive": true
    }
  ]
}
```

## ExpressionCompiler

Roslyn-based expression compiler. Results are cached.

```csharp
var compiler = new ExpressionCompiler();
var del = compiler.Compile<Func<Customer, bool>>(
    "customer.Age >= 18",
    new[] { "customer" }
);
```

## Delegate Types

RoslynRules compile to one of these signatures:

| Expression Type | Delegate | Example |
|----------------|----------|---------|
| Sync returning bool | `Func<TParam, bool>` | `Func<Customer, bool>` |
| Sync returning custom | `Func<TParam, TReturn>` | `Func<Customer, Result>` |
| Sync void | `Action<TParam>` | `Action<Customer>` |
| Async returning bool | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| Async returning custom | `Func<TParam, Task<TReturn>>` | `Func<Customer, Task<Result>>` |
| Async void | `Func<TParam, Task>` | `Func<Customer, Task>` |

## Rule Priority

Control execution order with the `Priority` property. Higher values execute first.

### Priority Property

```csharp
public int Priority { get; set; } // Default: 0
```

Higher values execute first. Negative values execute after default (0) priority rules. Immutable after `Compile()`.

```csharp
var rule = new Rule
{
    Description = "Critical check",
    Expression = "customer.IsVIP",
    Priority = 100 // Runs first
};
```

### Execution Order

Workflow and RuleBatch automatically sort active rules by priority (descending) before evaluation:

```csharp
var workflow = new Workflow
{
    Rules =
    {
        new Rule { Expression = "true", Priority = 0 },   // Runs third
        new Rule { Expression = "true", Priority = 10 }, // Runs first
        new Rule { Expression = "true", Priority = 5 }    // Runs second
    }
};
```

Applies to all execution modes: `Execute`, `ExecuteParallel`, `ExecuteAsync`, `ExecuteParallelAsync`.

## Lifecycle Events

Attach event handlers to individual rules for custom logic before and after execution. Events fire per-rule; child rules fire their own events independently.

### OnRuleExecuting

Fires before a rule evaluates. Set `Cancel = true` to skip execution and return a success result.

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

**Properties:**
- `args.Rule` — The rule about to execute
- `args.Parameters` — Runtime parameters passed to the rule
- `args.Cancel` — Set to `true` to skip evaluation
- `args.CancelReason` — Optional reason string (embedded as exception message)

**When cancelled:**
- Rule returns `RuleResult.Success = true` (skipped, not failed)
- `OnRuleExecuted` still fires with cancellation info
- Optional `CancelReason` is embedded as `OperationCanceledException` in the result

### OnRuleExecuted

Fires after a rule completes — success, failure, or cancellation. Does NOT fire when an unhandled exception propagates to the caller.

```csharp
rule.OnRuleExecuted += (sender, args) =>
{
    Console.WriteLine($"Rule {args.Rule.Description}: {(args.Result.Success ? "PASS" : "FAIL")}");
    Console.WriteLine($"Elapsed: {args.Elapsed.TotalMilliseconds:0.000}ms");
    
    if (args.Exception != null)
    {
        Console.WriteLine($"Exception: {args.Exception.Message}");
    }
};
```

**Properties:**
- `args.Rule` — The rule that executed
- `args.Result` — Full `RuleResult` (Success, Value, Exception, ChildResults)
- `args.Elapsed` — Execution time (excluding child rule evaluation)
- `args.Exception` — Exception if execution failed (null otherwise)

### Use Cases

| Use Case | Handler |
|----------|---------|
| Audit logging | `OnRuleExecuted` — write result to audit table |
| Circuit breaker | `OnRuleExecuting` — cancel if downstream service is unhealthy |
| Metrics collection | `OnRuleExecuted` — record timing to Prometheus/StatsD |
| Feature flags | `OnRuleExecuting` — cancel when feature is disabled |
| Debug tracing | Both — log rule evaluation flow |

## Result Caching

Cache rule evaluation results to avoid redundant execution. Opt-in per-rule via `CacheDuration`.

### Enabling Cache

```csharp
var rule = new Rule
{
    Description = "Expensive check",
    Expression = "HeavyComputation(customer)",
    CacheDuration = TimeSpan.FromMinutes(5)
};
```

**Properties:**
- `CacheDuration` (`TimeSpan?`) — Duration to cache results. `null` disables caching.

**Cache key construction:**
- Rule ID + parameter name/value pairs
- Value serialization uses `IFormattable.ToString()` or `object.ToString()`
- Complex objects without meaningful `ToString()` may produce collision-prone keys

### Clearing Cache

```csharp
rule.ClearCache();  // Removes all cached entries for this rule
```

### Cache Behavior

| Aspect | Behavior |
|--------|----------|
| Thread safety | `ConcurrentDictionary` — safe for concurrent access |
| Expiration | Lazy — checked on read; expired entries removed |
| Scope | Per-rule only; child rules have independent caches |
| Exceptions | Not cached; re-evaluated on next call |
| Lifecycle events | `OnRuleExecuting`/`OnRuleExecuted` fire on cache miss only |

### Recommendations

- Enable for idempotent rules whose result is stable during the cache window
- Use short durations (seconds to minutes) for volatile data
- Use longer durations (hours) for reference data
- Avoid caching rules with side effects (external calls that should rerun)

## IRuleEngine

Abstraction implemented by `Workflow` and `RuleBatch`. Enables dependency injection and mocking.

### Interface Methods

| Method | Returns | Purpose |
|--------|---------|---------|
| `Compile(params, namespaces?)` | `void` | One-time compilation |
| `Execute(params)` | `IEnumerable<RuleResult>` | Sequential execution |
| `ExecuteAsync(params, ct)` | `IAsyncEnumerable<RuleResult>` | Streaming async |
| `ExecuteParallel(params)` | `RuleResult[]` | CPU-bound parallel |
| `ExecuteParallelAsync(params, ct)` | `Task<RuleResult[]>` | Async parallel |
| `Validate()` | `void` | Pre-compile validation (throws) |
| `ValidateAll()` | `ValidationError[]` | Pre-compile validation (returns) |

### Dependency Injection

```csharp
using RoslynRules.Abstractions;

// Register Workflow as singleton
services.AddSingleton<IRuleEngine, Workflow>();

// Or use RuleBatch for throughput-focused scenarios
services.AddSingleton<IRuleEngine, RuleBatch>();

// Inject into your service
public class OrderService
{
    private readonly IRuleEngine _rules;
    public OrderService(IRuleEngine rules) => _rules = rules;

    public async Task<bool> ValidateOrderAsync(Order order)
    {
        var parameters = new[] { new RuleParameter("order", typeof(Order), order) };
        _rules.Compile(new[] { new RuleParameter("order", typeof(Order)) });
        var results = await _rules.ExecuteParallelAsync(parameters);
        return results.All(r => r.Success);
    }
}
```

### Mocking with Moq

```csharp
var mockEngine = new Mock<IRuleEngine>();
mockEngine.Setup(x => x.Execute(It.IsAny<RuleParameter[]>()>()))
    .Returns(new[] { new RuleResult { Success = true } });

var service = new OrderService(mockEngine.Object);
```

### Swapping Implementations

```csharp
// Test with lightweight RuleBatch
IRuleEngine engine = new RuleBatch();

// Production with full Workflow
IRuleEngine engine = new Workflow { Description = "Production" };
```

## RuleBatch

Evaluate multiple rules together as a unit. Shared compilation context, single validation pass.

### Methods

#### `AddRule(Rule)` / `AddRules(IEnumerable<Rule>)`

Builder pattern for adding rules. Returns the batch for chaining.

```csharp
var batch = new RuleBatch()
    .AddRule(new Rule { Expression = "customer.Age >= 18" })
    .AddRules(GetRulesFromDatabase());
```

#### `Validate()`

Validates all rules and checks for duplicate IDs within the batch.

```csharp
batch.Validate(); // Throws on duplicates or invalid rules
```

#### `Compile(RuleParameter[], string[]?)`

Compiles all active rules using a shared ExpressionCompiler.

```csharp
batch.Compile(parameters);
```

#### `Evaluate(params RuleParameter[])`

Sequential evaluation. Returns `IEnumerable<RuleResult>`.

```csharp
foreach (var result in batch.Evaluate(parameters)) { ... }
```

#### `EvaluateParallel(params RuleParameter[])`

Parallel evaluation using `Parallel.For`.

```csharp
var results = batch.EvaluateParallel(parameters);
```

#### `EvaluateAsync(params RuleParameter[])`

Async streaming evaluation. Returns `IAsyncEnumerable<RuleResult>`.

```csharp
await foreach (var result in batch.EvaluateAsync(parameters)) { ... }
```

#### `EvaluateParallelAsync(params RuleParameter[])`

Parallel async evaluation using `Task.WhenAll`.

```csharp
var results = await batch.EvaluateParallelAsync(parameters);
```

## RuleContext

Access dependency rule results during execution. Passed to expressions via the `context` parameter when `DependsOnRuleId` is used.

### Methods

#### `GetResult(Guid)`

Gets the full `RuleResult` for a dependency rule.

```csharp
var taxResult = context.GetResult(taxRule.Id);
if (taxResult.Success)
{
    var taxAmount = taxResult.Value;
}
```

#### `GetValue<T>(Guid)`

Gets the typed value from a successful dependency rule. Returns `default(T)` if rule not found or failed — ambiguous for value types.

```csharp
int amount = context.GetValue<int>(taxRule.Id);  // 0 if not found or value was 0
```

#### `TryGetValue<T>(Guid, out T?)`

Safer alternative to `GetValue`. Returns `true` only if rule succeeded and value is of type `T`.

```csharp
if (context.TryGetValue<int>(taxRule.Id, out var amount))
{
    Console.WriteLine($"Tax: {amount}");  // Only runs if rule succeeded
}
else
{
    Console.WriteLine("Tax rule failed or not found");
}
```

## Rule Templates

Create reusable rule templates with placeholders for parameterized rule generation.

### RuleTemplate

```csharp
public class RuleTemplate
{
    public string Description { get; set; }
    public string Expression { get; set; }           // Contains {placeholders}
    public string? Action { get; set; }               // Optional action template
    public Dictionary<string, PlaceholderKind> Placeholders { get; }

    public Rule Instantiate(Dictionary<string, object> values,
                            ExpressionCompiler compiler,
                            RuleParameter[] parameters,
                            string[] assemblies);

    public IReadOnlyList<string> ExtractPlaceholders();
}
```

### PlaceholderKind

| Kind | Use | Input Type | Output |
|------|-----|------------|--------|
| `Type` | CLR type references | `System.Type` | Fully-qualified type name |
| `Identifier` | Parameter/variable names | `string` | Raw identifier (no quotes) |
| `Value` | Literal values | Any | Properly formatted literal |

**Value formatting:**
- `null` → `null`
- `string` → `"value"` (with escaped quotes)
- `bool` → `true`/`false`
- `int`, `long` → numeric literal
- `float` → `123.45f`
- `double` → `123.45d`
- `decimal` → `123.45m`
- `DateTime` → `DateTime.Parse("...")`
- `DateTimeOffset` → `DateTimeOffset.Parse("...")`
- `Guid` → `Guid.Parse("...")`
- `Enum` → `Namespace.EnumType.Value`

### Instantiation

```csharp
var template = new RuleTemplate
{
    Expression = "{entity}.Age >= {minAge}",
    Placeholders =
    {
        ["entity"] = PlaceholderKind.Identifier,
        ["minAge"] = PlaceholderKind.Value
    }
};

var rule = template.Instantiate(
    new Dictionary<string, object>
    {
        ["entity"] = "customer",
        ["minAge"] = 18
    },
    compiler, parameters, assemblies);
```

**Validation:**
- Missing placeholders throw `ArgumentException`
- Empty/null Expression throws `InvalidOperationException`
- Type placeholders reject non-`System.Type` values

### ExtractPlaceholders

Introspect a template to discover required placeholders:

```csharp
var names = template.ExtractPlaceholders();
// Returns: ["entity", "minAge"]
```

## Result Caching

Cache rule evaluation results to avoid redundant execution.

### Enabling Cache

```csharp
var rule = new Rule
{
    Expression = "HeavyComputation(customer)",
    CacheDuration = TimeSpan.FromMinutes(5)
};
```

**Properties:**
- `CacheDuration` (`TimeSpan?`) — Duration to cache results. `null` disables caching.

### Clearing Cache

```csharp
rule.ClearCache();  // Removes all cached entries for this rule
```

### Cache Behavior

| Aspect | Behavior |
|--------|----------|
| Thread safety | `ConcurrentDictionary` — safe for concurrent access |
| Expiration | Lazy — checked on read; expired entries removed |
| Scope | Per-rule only; child rules have independent caches |
| Exceptions | Not cached; re-evaluated on next call |
| Lifecycle events | `OnRuleExecuting`/`OnRuleExecuted` fire on cache miss only |

### Recommendations

- Enable for idempotent rules whose result is stable during the cache window
- Use short durations (seconds to minutes) for volatile data
- Use longer durations (hours) for reference data
- Avoid caching rules with side effects (external calls that should rerun)

