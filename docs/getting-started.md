---
layout: default
title: Getting Started
nav_order: 2
---

[← Back to Documentation Index](index.md)

# Getting Started

## Installation

```bash
dotnet add package RoslynRules --version 1.0.0
```

Or reference the project directly:

```xml
<ProjectReference Include="..\RoslynRules\RoslynRules.csproj" />
```

## Your First Rule

### 1. Define a Model

```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsAdult { get; set; }
}
```

### 2. Create a Rule

```csharp
using RoslynRules.Models;

var adultRule = new Rule
{
    Description = "Check if customer is an adult",
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true",
    IsActive = true
};
```

### 3. Create a Workflow

```csharp
var workflow = new Workflow
{
    Description = "Customer validation",
    Rules = new List<Rule> { adultRule }
};
```

### 4. Define Parameters

For **compilation**, only the parameter **type** and **name** are needed. The value can be `null`.

```csharp
var compileParams = new RuleParameter[]
{
    new RuleParameter("customer", typeof(Customer))  // value not needed for Compile
};
```

For **execution**, pass a parameter with a real value:

```csharp
var customer = new Customer { Name = "Alice", Age = 25 };

var executeParams = new RuleParameter[]
{
    new RuleParameter("customer", typeof(Customer), customer)
};
```

### 5. Validate, Compile, Execute

```csharp
// Catch errors before compiling
workflow.Validate();

// Compile once — only types matter
workflow.Compile(compileParams);

// Execute many times with different values
var results = workflow.Execute(executeParams);

foreach (var result in results)
{
    Console.WriteLine($"Success: {result.Success}");
}
```

**Key point:** Compile with null values, execute with real instances. Separate compilation (needs types) from execution (needs values).

## Next Steps

- [Learn about child rules and bottom-up evaluation](api-reference.md#rule)
- [See async expression examples](examples/streaming-and-cancellation.md)
- [Compare performance modes](performance.md)
