---
layout: default
title: JSON Serialization
parent: API Reference
nav_order: 8
---

[← Back to API Reference](api-reference.md)

# JSON Serialization

Save and load rules/workflows from JSON for configuration-driven setups.

```csharp
using RoslynRules.Extensions;
```

---

## Methods

### `Serialize(Workflow|Rule, JsonSerializerOptions?)`

Serializes to JSON string.

```csharp
var json = JsonRuleLoader.Serialize(workflow);
File.WriteAllText("rules.json", json);
```

### `DeserializeWorkflow(string)` / `DeserializeRule(string)`

Deserializes from JSON string.

```csharp
var workflow = JsonRuleLoader.DeserializeWorkflow(json);
workflow.Validate();
workflow.Compile(parameters);
```

### `LoadWorkflowFromFile(string)` / `LoadRuleFromFile(string)`

Loads from file path.

```csharp
var workflow = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
```

### `SaveWorkflowToFile(Workflow, string)` / `SaveRuleToFile(Rule, string)`

Saves to file path.

```csharp
JsonRuleLoader.SaveWorkflowToFile(workflow, "rules.json");
```

---

## JSON Format

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
      "priority": 100,
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

---

## Options

Default options: camelCase naming, indented, nulls ignored, `JsonStringEnumConverter`, custom `TimeSpan` converter.

```csharp
var options = new JsonSerializerOptions(JsonRuleLoader.DefaultOptions)
{
    WriteIndented = false  // Compact output
};
```

---

## Related

- [Workflow](workflow.md) — Serializable container
- [Rule](rule.md) — Serializable rule
