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
| `ChildRules` | `IList<Rule>` | Child rules evaluated bottom-up before parent |
| `ParentRule` | `Rule?` | Navigation to parent (EF support) |
| `Workflow` | `Workflow?` | Navigation to parent workflow (EF support) |

### Methods

#### `Validate()`

Checks rule structure and expression syntax before compilation.

```csharp
rule.Validate(); // Throws on:
                 // - Empty rules (no Expression/Action/children)
                 // - Invalid C# syntax
                 // - Circular child references
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

#### `Compile(RuleParameter[], string[]?)`

Compiles all active rules using the shared ExpressionCompiler.

```csharp
workflow.Compile(parameters);
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

Concurrent async execution using `Task.WhenAll`.

```csharp
var results = await workflow.ExecuteParallelAsync(parameters);
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

## RuleResult

Result of rule execution.

```csharp
public RuleResult(bool success, object? data = null)
```

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | True if rule passed |
| `Data` | `object?` | Optional return data from Action |

## Logging

Rules integrate with `Microsoft.Extensions.Logging` for structured execution events.

### Rule.Logger

```csharp
rule.Logger = loggerFactory.CreateLogger<Rule>();
```

### LogRuleExecuted Extension

```csharp
using Rules.Models;

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

Rules compile to one of these signatures:

| Expression Type | Delegate | Example |
|----------------|----------|---------|
| Sync returning bool | `Func<TParam, bool>` | `Func<Customer, bool>` |
| Sync returning custom | `Func<TParam, TReturn>` | `Func<Customer, Result>` |
| Sync void | `Action<TParam>` | `Action<Customer>` |
| Async returning bool | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| Async returning custom | `Func<TParam, Task<TReturn>>` | `Func<Customer, Task<Result>>` |
| Async void | `Func<TParam, Task>` | `Func<Customer, Task>` |
