[← Back to Documentation Index](index.md)

# Getting Started

## Installation

```bash
dotnet add package Rules --version 1.0.0
```

Or reference the project directly:

```xml
<ProjectReference Include="..\Rules\Rules.csproj" />
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
using Rules.Models;

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

```csharp
var customer = new Customer { Name = "Alice", Age = 25 };

var parameters = new RuleParameter[]
{
    new RuleParameter("customer", typeof(Customer), customer)
};
```

### 5. Validate, Compile, Execute

```csharp
// Catch errors before compiling
workflow.Validate();

// Compile once
workflow.Compile(parameters);

// Execute many times
var results = workflow.Execute(parameters);

foreach (var result in results)
{
    Console.WriteLine($"Success: {result.Success}");
}
```

## Next Steps

- [Learn about child rules and bottom-up evaluation](api-reference.md#rule)
- [See async expression examples](examples/async-rules.md)
- [Compare performance modes](performance.md)
