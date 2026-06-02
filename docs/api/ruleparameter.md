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

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Parameter name |
| `Type` | `Type` | CLR type |
| `Value` | `object?` | Runtime value |

---

## Compile vs Execute

The **type** matters for compilation; the **value** matters for execution.

```csharp
// Compile with null values
workflow.Compile(new[]
{
    new RuleParameter("customer", typeof(Customer))  // value = null
});

// Execute later with real instances
var customer = new Customer { Name = "Alice", Age = 25 };
var results = workflow.Execute(new[]
{
    new RuleParameter("customer", typeof(Customer), customer)
});
```

**Use cases for null values:**
- Compile at startup, execute later with different data
- Avoid creating dummy objects for compilation
- Separate compilation (needs types) from execution (needs values)

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
