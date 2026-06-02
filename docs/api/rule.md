---
layout: default
title: Rule
parent: API Reference
nav_order: 1
---

[← Back to API Reference](api-reference.md)

# Rule

An individual rule with optional boolean `Expression`, `Action`, and child rules. Evaluated bottom-up: children first, then the parent's `Expression`, then `Action`.

```csharp
public sealed class Rule
```

**Key characteristics:**
- `sealed` — cannot be subclassed
- Properties become **immutable after `Compile()`** — modification throws `RuleCompilationException`
- Supports both **sync** and **async** expressions (auto-detected via `await`)

---

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (auto-generated on construction) |
| `Description` | `string` | Human-readable purpose |
| `IsActive` | `bool` | When `false`, rule is skipped during execution (default: `true`) |
| `Expression` | `string` | C# boolean expression evaluated during execution |
| `Action` | `string` | C# expression executed when rule succeeds |
| `Priority` | `int` | Execution priority — higher values execute first (default: `0`) |
| `Timeout` | `TimeSpan?` | Per-rule execution timeout (`null` = no timeout) |
| `CacheDuration` | `TimeSpan?` | Result caching duration (`null` = disabled) |
| `DependsOnRuleId` | `Guid?` | Foreign key for rule dependency (data-flow chaining) |
| `ParentRuleId` | `Guid?` | Foreign key for parent rule (structural nesting) |
| `ChildRules` | `IList<Rule>` | Child rules evaluated bottom-up before parent |
| `ParentRule` | `Rule?` | Navigation to parent (EF support) |
| `DependsOnRule` | `Rule?` | Navigation to dependency rule (EF support) |
| `Workflow` | `Workflow?` | Navigation to parent workflow (EF support) |
| `Logger` | `ILogger?` | Optional logger for structured execution events |
| `OnRuleExecuting` | `event` | Fires before execution; set `Cancel = true` to skip |
| `OnRuleExecuted` | `event` | Fires after execution with result and timing |

---

## Methods

### `Validate(IEnumerable<Guid>? availableRuleIds)`

Checks rule structure and expression syntax before compilation. Throws on errors.

```csharp
rule.Validate(); // Throws:
                   // - RuleValidationException: empty rule
                   // - SyntaxErrorException: invalid C# syntax
                   // - CircularReferenceException: circular child refs
```

**Parameters:**
- `availableRuleIds` — Optional set of IDs for validating `DependsOnRuleId` references

### `ValidateAll(IEnumerable<Guid>? availableRuleIds)`

Validates the rule and returns all errors without throwing.

```csharp
ValidationError[] errors = rule.ValidateAll();
if (errors.Any())
{
    foreach (var error in errors)
        Console.WriteLine($"[{error.ErrorType}] {error.Message}");
}
```

### `Compile(ExpressionCompiler, RuleParameter[], string[]?)`

Compiles `Expression` and `Action` into typed delegates. **Locks all properties** after compilation.

```csharp
var compiler = new ExpressionCompiler();
// Compile: type only, value can be null/default
var compileParams = new[] { new RuleParameter("c", typeof(Customer)) };
rule.Compile(compiler, compileParams);

// After compile — immutable:
rule.Description = "New name"; // ❌ RuleCompilationException
```

### `Execute(params RuleParameter[])`

Synchronous execution. Bottom-up order: children → Expression → Action.

```csharp
var result = rule.Execute(parameters);
// result.Success = true if all children pass AND Expression passes
```

### `ExecuteAsync(params RuleParameter[])`

Asynchronous execution for rules containing `await`.

```csharp
var result = await rule.ExecuteAsync(parameters);
```

### `ExecuteWithContext(RuleContext?, params RuleParameter[])`

Executes with access to dependency results via `RuleContext`.

```csharp
var context = new RuleContext();
var result = rule.ExecuteWithContext(context, parameters);
// context now contains this rule's result for dependents
```

### `ClearCache()`

Removes all cached results, forcing the next evaluation to re-execute. Thread-safe.

```csharp
rule.ClearCache();  // Force next Execute/ExecuteAsync to re-run
```

### `ValidateSemantics(ExpressionCompiler, RuleParameter[], string[]?)`

Performs **semantic validation** by attempting a dry-run compilation. Catches errors that syntax-only validation misses: undefined variables, missing types, incorrect method signatures.

```csharp
rule.ValidateSemantics(compiler, parameters);
```

---

## Events

### `OnRuleExecuting`

Fires before a rule evaluates. Set `Cancel = true` to skip execution.

```csharp
rule.OnRuleExecuting += (sender, args) =>
{
    Console.WriteLine($"About to execute: {args.Rule.Description}");
    if (shouldSkip) { args.Cancel = true; args.CancelReason = "Skipped"; }
};
```

### `OnRuleExecuted`

Fires after a rule completes — success, failure, or cancellation.

```csharp
rule.OnRuleExecuted += (sender, args) =>
{
    Console.WriteLine($"Rule {args.Rule.Description}: {(args.Result.Success ? "PASS" : "FAIL")}");
    Console.WriteLine($"Elapsed: {args.Elapsed.TotalMilliseconds}ms");
};
```

---

## Child Rules

Child rules are evaluated **bottom-up** before the parent. The parent fails if any child fails.

```csharp
var parent = new Rule
{
    Description = "Valid adult",
    Expression = "customer.IsAdult"
};

parent.ChildRules.Add(new Rule
{
    Description = "Age check",
    Expression = "customer.Age >= 18"
});

parent.ChildRules.Add(new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
});

// parent succeeds only if BOTH children pass
```

---

## Related

- [Workflow](workflow.md) — Container for top-level rules
- [RuleResult](ruleresult.md) — Execution result structure
- [Lifecycle Events](lifecycle-events.md) — Event details
- [Result Caching](result-caching.md) — Caching behavior
