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

## Custom Options

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var json = JsonRuleLoader.Serialize(workflow, options);
```
