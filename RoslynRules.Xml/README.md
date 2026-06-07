# RoslynRules.Xml

XML serialization support for RoslynRules workflows and rules.

## Installation

`ash
dotnet add package RoslynRules.Xml
`

## Usage

### Serialize a Workflow

`csharp
using RoslynRules.Xml;
using RoslynRules.Models;

var workflow = new Workflow
{
    Description = "My Workflow",
    Rules =
    {
        new Rule
        {
            Description = "Age Check",
            Expression = "customer.Age >= 18",
            IsActive = true
        }
    }
};

var xml = XmlRuleLoader.Serialize(workflow);
`

### Deserialize a Workflow

`csharp
var workflow = XmlRuleLoader.DeserializeWorkflow(xmlString);
`

### Save/Load from Files

`csharp
// Save
XmlRuleLoader.SaveWorkflowToFile(workflow, "workflow.xml");

// Load
var loaded = XmlRuleLoader.LoadWorkflowFromFile("workflow.xml");
`

### Serialize a Single Rule

`csharp
var rule = new Rule
{
    Description = "Simple Rule",
    Expression = "true"
};

var xml = XmlRuleLoader.Serialize(rule);
var loaded = XmlRuleLoader.DeserializeRule(xml);
`

## XML Format

`xml
<Workflow Id="guid" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true">
  <Description>My Workflow</Description>
  <Rules>
    <Rule Id="guid" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true" Priority="0">
      <Description>Age Check</Description>
      <Expression>customer.Age >= 18</Expression>
      <Action>customer.IsAdult = true</Action>
      <Timeout>10</Timeout>
      <CacheDuration>300</CacheDuration>
      <ChildRules>
        <!-- Nested child rules -->
      </ChildRules>
    </Rule>
  </Rules>
</Workflow>
`

## AOT Compatibility

This extension uses System.Xml.Linq (XDocument) which is compatible with AOT/trimming. No reflection-based serialization is used.
