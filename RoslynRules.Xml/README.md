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

## AOT Compatibility

This extension uses System.Xml.Linq (XDocument) which is compatible with AOT/trimming. No reflection-based serialization is used.
