# RoslynRules.Json

JSON serialization support for RoslynRules. Save and load workflows and rules from JSON for configuration-driven rule sets.

## Installation

```bash
dotnet add package RoslynRules.Json
```

## Usage

### Serialize

```csharp
using RoslynRules.Json;
using RoslynRules.Models;

var workflow = new Workflow
{
    Description = "Validation rules",
    Rules = new List<Rule>
    {
        new Rule { Description = "Adult check", Expression = "customer.Age >= 18" }
    }
};

var json = JsonRuleLoader.Serialize(workflow);
File.WriteAllText("rules.json", json);
```

### Deserialize

```csharp
var workflow = JsonRuleLoader.DeserializeWorkflow(json);
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
var results = workflow.Execute(new[] { new RuleParameter("customer", typeof(Customer), customer) });
```

### File Helpers

```csharp
JsonRuleLoader.SaveWorkflowToFile(workflow, "rules.json");
var loaded = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
```

### JSON Format

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "description": "Validation rules",
  "version": "1.0.0",
  "isActive": true,
  "rules": [
    {
      "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "isActive": true,
      "priority": 0
    }
  ]
}
```

## Custom Options

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var json = JsonRuleLoader.Serialize(workflow, options);
```
