---
layout: default
title: RuleParameter
parent: API Reference
nav_order: 4
---

[← Back to API Reference](api-reference.md)

# RuleParameter

Defines a parameter for rule expression compilation and execution.

```csharp
public class RuleParameter
```

---

## Constructor

```csharp
public RuleParameter(string name, Type type, object? value = null)
```

| Parameter | Description |
|-----------|-------------|
| `name` | Parameter name used in expressions |
| `type` | CLR type for compilation |
| `value` | Optional runtime value (can be `null` for compile-only) |

---

## Factory Methods

### `ForCompile(string name, Type type)`

Creates a parameter for **compilation only** — no value needed.

```csharp
var compileParam = RuleParameter.ForCompile("customer", typeof(Customer));
workflow.Compile(new[] { compileParam });
```

### `ForExecute(string name, Type type, object value)`

Creates a parameter for **execution** — requires a real value.

```csharp
var customer = new Customer { Name = "Alice", Age = 25 };
var executeParam = RuleParameter.ForExecute("customer", typeof(Customer), customer);
var results = workflow.Execute(new[] { executeParam });
```

---

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Parameter name |
| `Type` | `Type` | CLR type |
| `Value` | `object?` | Runtime value |
| `HasValue` | `bool` | True if a runtime value is set |

---

## Compile vs Execute

| Phase | What Matters | Factory Method |
|-------|-----------|----------------|
| **Compile** | Name + Type only (value can be null) | `ForCompile(name, type)` |
| **Execute** | Name + Type + Value (all required) | `ForExecute(name, type, value)` |

```csharp
// Compile at startup — types only
var compileParams = new[]
{
    RuleParameter.ForCompile("customer", typeof(Customer))
};
workflow.Compile(compileParams);

// Execute later with real data
var executeParams = new[]
{
    RuleParameter.ForExecute("customer", typeof(Customer), customer)
};
var results = workflow.Execute(executeParams);
```

**Why separate them?**
- Compile once at startup (no data needed)
- Execute many times with different data
- Prevents accidental name/type mismatches between compile and execute

---

## Single Parameter Limitation

RoslynRules supports exactly **one parameter**. Wrap multiple values in a struct or class.

```csharp
// ❌ Not supported
new RuleParameter("a", typeof(int), a),
new RuleParameter("b", typeof(int), b)

// ✅ Wrap in a struct
public record Input(int A, int B);
new RuleParameter("input", typeof(Input), new Input(a, b))
// Expression: "input.A > input.B"
```

---

## Related

- [Rule](rule.md) — Accepts RuleParameter
- [Delegate Types](delegate-types.md) — Supported signatures
