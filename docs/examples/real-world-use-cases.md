---
layout: default
title: Real-World Use Cases
parent: Examples
nav_order: 8
---

[← Back to Examples Index](index.md)

# Real-World Use Cases

Practical examples for common business scenarios.

## Table of Contents
- [Form Validation](#form-validation)
- [Transaction Screening](#transaction-screening)
- [Feature Flags](#feature-flags)
- [Compliance Checks](#compliance-checks)
- [Pricing Engine](#pricing-engine)
- [Content Moderation](#content-moderation)

## Form Validation

Multi-field form validation with detailed error messages.

### Model

```csharp
public class RegistrationForm
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public int Age { get; set; }
    public string Country { get; set; } = "";
}

public class ValidationError
{
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
}
```

### Rules

```csharp
var emailRule = new Rule
{
    Description = "Email format",
    Expression = @"System.Text.RegularExpressions.Regex.IsMatch(form.Email, @""^[^@\s]+@[^@\s]+\.[^@\s]+$"")",
    Action = @"errors.Add(new ValidationError { Field = "Email", Message = "Invalid email format" })"
};

var passwordLengthRule = new Rule
{
    Description = "Password length",
    Expression = "form.Password.Length >= 8",
    Action = @"errors.Add(new ValidationError { Field = "Password", Message = "Password must be at least 8 characters" })"
};

var passwordMatchRule = new Rule
{
    Description = "Password match",
    Expression = "form.Password == form.ConfirmPassword",
    Action = @"errors.Add(new ValidationError { Field = "ConfirmPassword", Message = "Passwords do not match" })"
};

var ageRule = new Rule
{
    Description = "Minimum age",
    Expression = "form.Age >= 13",
    Action = @"errors.Add(new ValidationError { Field = "Age", Message = "Must be at least 13 years old" })"
};

var countryRule = new Rule
{
    Description = "Supported country",
    Expression = "new[] { "US", "CA", "UK", "DE" }.Contains(form.Country)",
    Action = @"errors.Add(new ValidationError { Field = "Country", Message = "Country not supported" })"
};

var validationWorkflow = new Workflow
{
    Description = "Registration validation",
    Rules = new List<Rule> { emailRule, passwordLengthRule, passwordMatchRule, ageRule, countryRule }
};
```

### Execution

```csharp
var form = new RegistrationForm
{
    Email = "user@example.com",
    Password = "SecurePass123",
    ConfirmPassword = "SecurePass123",
    Age = 25,
    Country = "US"
};

var errors = new List<ValidationError>();
var parameters = new[]
{
    new RuleParameter("form", typeof(RegistrationForm), form),
    new RuleParameter("errors", typeof(List<ValidationError>), errors)
};

validationWorkflow.Validate();
validationWorkflow.Compile(parameters);

var results = validationWorkflow.Execute(parameters).ToList();

if (results.All(r => r.Success))
{
    Console.WriteLine("Form is valid!");
}
else
{
    foreach (var error in errors)
    {
        Console.WriteLine($"{error.Field}: {error.Message}");
    }
}
```

### Key Points

- Each rule validates one field
- `Action` adds error messages only on failure (inverted logic)
- `errors` list is mutated by rules
- All rules run — not short-circuited — to collect all errors

## Transaction Screening

Multi-stage fraud detection pipeline.

### Model

```csharp
public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string Country { get; set; } = "";
    public bool IsFlagged { get; set; }
    public string RiskLevel { get; set; } = "low";
}
```

### Rules

```csharp
// Stage 1: Basic validation
var validateTransaction = new Rule
{
    Description = "Validate transaction",
    Expression = "transaction.Amount > 0 && !string.IsNullOrEmpty(transaction.MerchantId)",
    IsActive = true
};

// Stage 2: Check amount limits (depends on validation)
var checkAmount = new Rule
{
    Description = "Amount check",
    DependsOnRuleId = validateTransaction.Id,
    Expression = "transaction.Amount < 10000",
    Action = "transaction.RiskLevel = transaction.Amount > 5000 ? "medium" : "low"",
    IsActive = true
};

// Stage 3: Check merchant reputation
var checkMerchant = new Rule
{
    Description = "Merchant check",
    DependsOnRuleId = validateTransaction.Id,
    Expression = "!Blacklist.Contains(transaction.MerchantId)",
    Action = @"transaction.IsFlagged = true",
    IsActive = true
};

// Stage 4: Geography check
var checkGeography = new Rule
{
    Description = "Geography check",
    DependsOnRuleId = checkAmount.Id,
    Expression = @"!new[] { "CN", "KP", "IR" }.Contains(transaction.Country)",
    Action = "transaction.IsFlagged = true",
    IsActive = true
};

// Stage 5: Final risk assessment
var riskAssessment = new Rule
{
    Description = "Risk assessment",
    DependsOnRuleId = checkGeography.Id,
    Expression = "!transaction.IsFlagged",
    Action = @"transaction.RiskLevel = "approved"",
    IsActive = true
};

var fraudWorkflow = new Workflow
{
    Description = "Fraud detection",
    Rules = new List<Rule>
    {
        validateTransaction, checkAmount, checkMerchant, checkGeography, riskAssessment
    }
};
```

### Execution

```csharp
var transaction = new Transaction
{
    Amount = 7500,
    Currency = "USD",
    MerchantId = "MERCH123",
    CustomerId = "CUST456",
    Country = "US"
};

var parameters = new[]
{
    new RuleParameter("transaction", typeof(Transaction), transaction),
    new RuleParameter("Blacklist", typeof(HashSet<string>), new HashSet<string> { "BAD_MERCH" })
};

fraudWorkflow.Validate();
fraudWorkflow.Compile(parameters);

var results = fraudWorkflow.Execute(parameters).ToList();

Console.WriteLine($"Risk Level: {transaction.RiskLevel}");
Console.WriteLine($"Flagged: {transaction.IsFlagged}");

// Output: Risk Level: medium, Flagged: False
```

### Key Points

- Uses `DependsOnRuleId` for staged evaluation
- Earlier stages can flag transaction for later stages
- `Action` mutates the transaction object
- Risk level escalates based on multiple factors

## Feature Flags

Dynamic feature enablement based on user segments.

### Model

```csharp
public class UserContext
{
    public string UserId { get; set; } = "";
    public string Tier { get; set; } = "free"; // free, pro, enterprise
    public string Region { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<string> FeatureFlags { get; set; } = new();
}
```

### Rules

```csharp
var newFeature = new Rule
{
    Description = "New dashboard",
    Expression = @"user.Tier == "pro" || user.Tier == "enterprise"",
    Action = "user.FeatureFlags.Add("new_dashboard")",
    IsActive = true
};

var betaFeature = new Rule
{
    Description = "Beta API",
    Expression = @"user.Tier == "enterprise" && user.CreatedAt < DateTime.Now.AddYears(-1)",
    Action = "user.FeatureFlags.Add("beta_api")",
    IsActive = true
};

var regionalFeature = new Rule
{
    Description = "EU storage",
    Expression = @"user.Region == "EU"",
    Action = "user.FeatureFlags.Add("eu_storage")",
    IsActive = true
};

var featureWorkflow = new Workflow
{
    Description = "Feature flags",
    Rules = new List<Rule> { newFeature, betaFeature, regionalFeature }
};
```

### Execution

```csharp
var user = new UserContext
{
    UserId = "user123",
    Tier = "pro",
    Region = "US",
    CreatedAt = DateTime.Now.AddMonths(-6)
};

var parameters = new[]
{
    new RuleParameter("user", typeof(UserContext), user)
};

featureWorkflow.Compile(parameters);
var results = featureWorkflow.Execute(parameters).ToList();

// user.FeatureFlags now contains: ["new_dashboard"]
// beta_api: not added (not enterprise)
// eu_storage: not added (not EU)
```

### Key Points

- Rules represent feature enablement conditions
- `Action` adds enabled features to user's flag list
- Easy to add new features without code changes
- Can store rules in database for dynamic updates

## Compliance Checks

Regulatory compliance validation with audit trails.

### Model

```csharp
public class ComplianceData
{
    public bool HasConsent { get; set; }
    public DateTime? ConsentDate { get; set; }
    public bool IsMinAge { get; set; }
    public bool DataRetentionExpired { get; set; }
    public List<string> AuditLog { get; set; } = new();
}
```

### Rules

```csharp
var consentRule = new Rule
{
    Description = "GDPR consent",
    Expression = "data.HasConsent && data.ConsentDate.HasValue",
    Action = @"data.AuditLog.Add($"GDPR consent valid: {data.ConsentDate.Value:yyyy-MM-dd}")",
    IsActive = true
};

var ageRule = new Rule
{
    Description = "Age verification",
    Expression = "data.IsMinAge",
    Action = @"data.AuditLog.Add("Age verification passed")",
    IsActive = true
};

var retentionRule = new Rule
{
    Description = "Data retention",
    Expression = "!data.DataRetentionExpired",
    Action = @"data.AuditLog.Add("Data retention valid")",
    IsActive = true
};

var complianceWorkflow = new Workflow
{
    Description = "GDPR compliance",
    Rules = new List<Rule> { consentRule, ageRule, retentionRule }
};
```

### Execution with Audit Trail

```csharp
var data = new ComplianceData
{
    HasConsent = true,
    ConsentDate = DateTime.Now.AddMonths(-3),
    IsMinAge = true,
    DataRetentionExpired = false
};

var parameters = new[] { new RuleParameter("data", typeof(ComplianceData), data) };

complianceWorkflow.Compile(parameters);
var results = complianceWorkflow.Execute(parameters).ToList();

var allPassed = results.All(r => r.Success);
Console.WriteLine($"Compliant: {allPassed}");

// Audit trail
foreach (var entry in data.AuditLog)
{
    Console.WriteLine($"[AUDIT] {entry}");
}
// [AUDIT] GDPR consent valid: 2026-02-28
// [AUDIT] Age verification passed
// [AUDIT] Data retention valid
```

### Key Points

- `Action` records audit entries
- All rules run — not short-circuited — for complete audit
- Results provide clear pass/fail for each regulation
- Audit trail is immutable record of compliance check

## Pricing Engine

Dynamic pricing based on multiple factors.

### Model

```csharp
public class PricingContext
{
    public decimal BasePrice { get; set; }
    public string CustomerTier { get; set; } = "";
    public int Quantity { get; set; }
    public string Region { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal FinalPrice { get; set; }
}
```

### Rules

```csharp
// Calculate base total
var baseTotalRule = new Rule
{
    Description = "Base total",
    Expression = "ctx.BasePrice > 0 && ctx.Quantity > 0",
    Action = "ctx.FinalPrice = ctx.BasePrice * ctx.Quantity",
    IsActive = true
};

// Apply volume discount
var volumeDiscount = new Rule
{
    Description = "Volume discount",
    DependsOnRuleId = baseTotalRule.Id,
    Expression = "ctx.Quantity >= 100",
    Action = "ctx.FinalPrice *= 0.9m", // 10% off
    IsActive = true
};

// Apply tier discount
var tierDiscount = new Rule
{
    Description = "Tier discount",
    DependsOnRuleId = baseTotalRule.Id,
    Expression = @"ctx.CustomerTier == "enterprise"",
    Action = "ctx.FinalPrice *= 0.85m", // 15% off
    IsActive = true
};

// Apply seasonal discount
var seasonalDiscount = new Rule
{
    Description = "Holiday discount",
    DependsOnRuleId = baseTotalRule.Id,
    Expression = "ctx.OrderDate.Month == 11 || ctx.OrderDate.Month == 12",
    Action = "ctx.FinalPrice *= 0.95m", // 5% off
    IsActive = true
};

var pricingWorkflow = new Workflow
{
    Description = "Dynamic pricing",
    Rules = new List<Rule> { baseTotalRule, volumeDiscount, tierDiscount, seasonalDiscount }
};
```

### Execution

```csharp
var ctx = new PricingContext
{
    BasePrice = 50m,
    Quantity = 150,
    CustomerTier = "enterprise",
    Region = "US",
    OrderDate = new DateTime(2026, 11, 15)
};

var parameters = new[] { new RuleParameter("ctx", typeof(PricingContext), ctx) };

pricingWorkflow.Validate();
pricingWorkflow.Compile(parameters);

var results = pricingWorkflow.Execute(parameters).ToList();

// Calculation:
// Base: 50 * 150 = 7500
// Volume: 7500 * 0.9 = 6750
// Tier: 6750 * 0.85 = 5737.50
// Seasonal: 5737.50 * 0.95 = 5450.63

Console.WriteLine($"Final Price: {ctx.FinalPrice:C}");
// Final Price: $5,450.63
```

### Key Points

- Uses `DependsOnRuleId` to ensure base calculation runs first
- Multiple discounts can stack
- Order of discounts matters — use priority or dependencies to control
- `Action` mutates the context object for subsequent rules

## Content Moderation

Automated content filtering with severity levels.

### Model

```csharp
public class ContentItem
{
    public string Text { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Severity { get; set; } = "none"; // none, low, medium, high
    public List<string> Violations { get; set; } = new();
}
```

### Rules

```csharp
var spamCheck = new Rule
{
    Description = "Spam detection",
    Expression = @"!System.Text.RegularExpressions.Regex.IsMatch(content.Text, @""(click here|buy now|limited time)"")",
    Action = @"content.Violations.Add("spam"); content.Severity = "low"",
    IsActive = true
};

var hateSpeechCheck = new Rule
{
    Description = "Hate speech",
    Expression = @"!BannedWords.Any(word => content.Text.Contains(word))",
    Action = @"content.Violations.Add("hate_speech"); content.Severity = "high"",
    IsActive = true
};

var personalInfoCheck = new Rule
{
    Description = "PII detection",
    Expression = @"!System.Text.RegularExpressions.Regex.IsMatch(content.Text, @""\b\d{3}-\d{2}-\d{4}\b"")",
    Action = @"content.Violations.Add("pii"); content.Severity = "medium"",
    IsActive = true
};

var moderationWorkflow = new Workflow
{
    Description = "Content moderation",
    Rules = new List<Rule> { spamCheck, hateSpeechCheck, personalInfoCheck }
};
```

### Execution

```csharp
var content = new ContentItem
{
    Text = "Check out this amazing offer! Buy now for limited time!",
    Tags = new List<string> { "promotional" }
};

var bannedWords = new[] { "hate", "violence", "discrimination" };

var parameters = new[]
{
    new RuleParameter("content", typeof(ContentItem), content),
    new RuleParameter("BannedWords", typeof(string[]), bannedWords)
};

moderationWorkflow.Compile(parameters);
var results = moderationWorkflow.Execute(parameters).ToList();

if (content.Violations.Any())
{
    Console.WriteLine($"Content flagged. Severity: {content.Severity}");
    Console.WriteLine($"Violations: {string.Join(", ", content.Violations)}");
}
else
{
    Console.WriteLine("Content approved");
}

// Output: Content flagged. Severity: low
//         Violations: spam
```

### Key Points

- Multiple checks run independently
- Severity escalates based on the worst violation
- Can use `Action` to auto-tag content for review queues
- Rules can be updated without deploying code

## See Also

- [Rule Action Chaining](rule-action-chaining.md)
- [Testing Framework](testing-framework.md)
- [Performance Tuning](../performance.md)
