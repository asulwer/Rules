# RoslynRules.Xml

XML serialization support for RoslynRules. Save and load workflows and rules from XML for configuration-driven rule sets.

## Installation

```bash
dotnet add package RoslynRules.Xml
```

## Usage

### Serialize

```csharp
using RoslynRules.Xml;
using RoslynRules.Models;

var workflow = new Workflow
{
    Description = "Validation rules",
    Rules = new List<Rule>
    {
        new Rule { Description = "Adult check", Expression = "customer.Age >= 18" }
    }
};

var xml = XmlRuleLoader.Serialize(workflow);
File.WriteAllText("rules.xml", xml);
```

### Deserialize

```csharp
var workflow = XmlRuleLoader.DeserializeWorkflow(xml);
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
var results = workflow.Execute(new[] { new RuleParameter("customer", typeof(Customer), customer) });
```

### File Helpers

```csharp
XmlRuleLoader.SaveWorkflowToFile(workflow, "rules.xml");
var loaded = XmlRuleLoader.LoadWorkflowFromFile("rules.xml");
```

### XML Format

```xml
<Workflow Id="550e8400-e29b-41d4-a716-446655440000" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true">
  <Description>Validation rules</Description>
  <Rules>
    <Rule Id="6ba7b810-9dad-11d1-80b4-00c04fd430c8" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true" Priority="0">
      <Description>Adult check</Description>
      <Expression>customer.Age >= 18</Expression>
    </Rule>
  </Rules>
</Workflow>
```

## AOT Compatibility

This extension uses System.Xml.Linq (XDocument) which is compatible with AOT/trimming. No reflection-based serialization is used.
