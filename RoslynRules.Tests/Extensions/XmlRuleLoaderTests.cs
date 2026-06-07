using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Xml;
using System;
using Xunit;

namespace RoslynRules.Tests.Extensions;

/// <summary>
/// Tests for XmlRuleLoader serialization and deserialization.
/// </summary>
public class XmlRuleLoaderTests
{
    [Fact]
    public void Serialize_Deserialize_PreservesCacheDuration()
    {
        var workflow = new Workflow
        {
            Description = "Test workflow",
            Rules =
            {
                new Rule
                {
                    Description = "Cached rule",
                    Expression = "true",
                    IsActive = true,
                    CacheDuration = TimeSpan.FromMinutes(5)
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesTimeout()
    {
        var workflow = new Workflow
        {
            Description = "Test workflow",
            Rules =
            {
                new Rule
                {
                    Description = "Timed rule",
                    Expression = "true",
                    IsActive = true,
                    Timeout = TimeSpan.FromSeconds(10)
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesRuleProperties()
    {
        var workflow = new Workflow
        {
            Description = "Test workflow",
            IsActive = true,
            Rules =
            {
                new Rule
                {
                    Description = "Full rule",
                    Expression = "customer.Age >= 18",
                    Action = "customer.IsAdult = true",
                    IsActive = true,
                    Priority = 10,
                    CacheDuration = TimeSpan.FromMinutes(5),
                    Timeout = TimeSpan.FromSeconds(10)
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Description.Should().Be("Test workflow");
        restored.IsActive.Should().BeTrue();
        restored.Rules.Should().HaveCount(1);

        var rule = restored.Rules[0];
        rule.Description.Should().Be("Full rule");
        rule.Expression.Should().Be("customer.Age >= 18");
        rule.Action.Should().Be("customer.IsAdult = true");
        rule.IsActive.Should().BeTrue();
        rule.Priority.Should().Be(10);
        rule.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        rule.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Serialize_Deserialize_NullCacheDuration_RoundTrips()
    {
        var workflow = new Workflow
        {
            Rules =
            {
                new Rule
                {
                    Description = "No cache",
                    Expression = "true"
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules[0].CacheDuration.Should().BeNull();
    }

    [Fact]
    public void Serialize_Deserialize_ChildRules_PreserveCacheDuration()
    {
        var parent = new Rule
        {
            Description = "Parent",
            Expression = "true"
        };

        var child = new Rule
        {
            Description = "Child",
            Expression = "true",
            CacheDuration = TimeSpan.FromSeconds(30)
        };

        parent.ChildRules.Add(child);

        var workflow = new Workflow
        {
            Rules = { parent }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules[0].ChildRules.Should().HaveCount(1);
        restored.Rules[0].ChildRules[0].CacheDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesVersion()
    {
        var workflow = new Workflow
        {
            Version = new RuleVersion(2, 1, 0),
            Rules =
            {
                new Rule
                {
                    Version = new RuleVersion(1, 5, 3),
                    Description = "Versioned rule",
                    Expression = "true"
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Version.Should().Be(new RuleVersion(2, 1, 0));
        restored.Rules[0].Version.Should().Be(new RuleVersion(1, 5, 3));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesDependencies()
    {
        var depId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var workflow = new Workflow
        {
            Rules =
            {
                new Rule { Id = depId, Description = "Dep", Expression = "true", IsActive = true },
                new Rule { Id = ruleId, Description = "Dependent", Expression = "true", IsActive = true, DependsOnRuleId = depId }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules.Should().HaveCount(2);
        restored.Rules[1].DependsOnRuleId.Should().Be(depId);
    }

    [Fact]
    public void Serialize_SingleRule_RoundTrips()
    {
        var rule = new Rule
        {
            Description = "Standalone rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 5
        };

        var xml = XmlRuleLoader.Serialize(rule);
        var restored = XmlRuleLoader.DeserializeRule(xml);

        restored.Description.Should().Be("Standalone rule");
        restored.Expression.Should().Be("x > 0");
        restored.Action.Should().Be("result = x");
        restored.Priority.Should().Be(5);
    }

    [Fact]
    public void SaveAndLoad_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "File test",
                Rules = { new Rule { Description = "R1", Expression = "true" } }
            };

            XmlRuleLoader.SaveWorkflowToFile(workflow, path);
            var loaded = XmlRuleLoader.LoadWorkflowFromFile(path);

            loaded.Description.Should().Be("File test");
            loaded.Rules.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
