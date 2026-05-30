# Examples

## Basic Boolean Rule

```csharp
var rule = new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18",
    IsActive = true
};
```

## Rule with Action

```csharp
var rule = new Rule
{
    Description = "Mark adult",
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true",
    IsActive = true
};
```

## Parent with Child Rules

Children evaluated bottom-up. Parent only runs if all children pass.

```csharp
var parent = new Rule
{
    Description = "Full validation",
    Expression = "customer.IsValid"
};

var child1 = new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
};

var child2 = new Rule
{
    Description = "Age check",
    Expression = "customer.Age > 0"
};

parent.ChildRules.Add(child1);
parent.ChildRules.Add(child2);
```

## Async Rule

Auto-detected by `await` keyword. Compiled to `Func<T, Task<bool>>`.

```csharp
var rule = new Rule
{
    Description = "Check price from API",
    Expression = "await PriceService.GetAsync(customer.ProductId) > 100"
};

// Compile normally
workflow.Compile(parameters);

// Execute async
var results = await workflow.ExecuteParallelAsync(parameters);
```

## Returning Multiple Values

Wrap outputs in a struct:

```csharp
public struct ValidationResult
{
    public bool IsAdult { get; set; }
    public bool IsPremium { get; set; }
}

var rule = new Rule
{
    Expression = "new ValidationResult { 
        IsAdult = customer.Age >= 18,
        IsPremium = customer.Age >= 65
    }"
};

// Access result.Data as ValidationResult
```

## Workflow with Multiple Rules

```csharp
var workflow = new Workflow
{
    Description = "Customer processing",
    Rules = new List<Rule>
    {
        new Rule { Expression = "customer.Age >= 18", Description = "Adult" },
        new Rule { Expression = "customer.Name.StartsWith(\"A\")", Description = "Starts with A" },
        new Rule { 
            Expression = "customer.IsActive",
            Action = "customer.Processed = true",
            Description = "Process active"
        }
    }
};

workflow.Validate();
workflow.Compile(parameters);

// All rules evaluated, results in order
var results = workflow.ExecuteParallel(parameters);
```

## Validation Before Compile

```csharp
try
{
    workflow.Validate();
    workflow.Compile(parameters);
}
catch (InvalidOperationException ex)
{
    // Catches:
    // - Empty rules
    // - Syntax errors in expressions
    // - Circular child references
    // - Duplicate rule IDs
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```
