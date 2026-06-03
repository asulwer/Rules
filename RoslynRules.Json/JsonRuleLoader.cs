using RoslynRules.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Json;

/// <summary>
/// JSON serialization and deserialization for Rules and Workflows.
/// Uses System.Text.Json with custom converters for trim/AOT-safe
/// serialization without reflection-based ID restoration.
/// </summary>
public static class JsonRuleLoader
{
    /// <summary>
    /// Default JSON options with camelCase naming, indented output,
    /// and custom converters for TimeSpan serialization.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TimeSpanJsonConverter()
        }
    };

    /// <summary>
    /// Serializes a workflow to JSON string.
    /// </summary>
    public static string Serialize(Workflow workflow, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(workflow, options ?? DefaultOptions);

    /// <summary>
    /// Serializes a rule to JSON string.
    /// </summary>
    public static string Serialize(Rule rule, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(rule, options ?? DefaultOptions);

    /// <summary>
    /// Deserializes a workflow from JSON string.
    /// </summary>
    public static Workflow DeserializeWorkflow(string json, JsonSerializerOptions? options = null)
    {
        var workflow = JsonSerializer.Deserialize<Workflow>(json, options ?? DefaultOptions);
        return workflow ?? throw new JsonException("Failed to deserialize workflow from JSON.");
    }

    /// <summary>
    /// Deserializes a rule from JSON string.
    /// </summary>
    public static Rule DeserializeRule(string json, JsonSerializerOptions? options = null)
    {
        var rule = JsonSerializer.Deserialize<Rule>(json, options ?? DefaultOptions);
        return rule ?? throw new JsonException("Failed to deserialize rule from JSON.");
    }

    /// <summary>
    /// Loads a workflow from a JSON file.
    /// </summary>
    public static Workflow LoadWorkflowFromFile(string filePath, JsonSerializerOptions? options = null)
        => DeserializeWorkflow(File.ReadAllText(filePath), options);

    /// <summary>
    /// Loads a rule from a JSON file.
    /// </summary>
    public static Rule LoadRuleFromFile(string filePath, JsonSerializerOptions? options = null)
        => DeserializeRule(File.ReadAllText(filePath), options);

    /// <summary>
    /// Saves a workflow to a JSON file.
    /// </summary>
    public static void SaveWorkflowToFile(Workflow workflow, string filePath, JsonSerializerOptions? options = null)
        => File.WriteAllText(filePath, Serialize(workflow, options));

    /// <summary>
    /// Saves a rule to a JSON file.
    /// </summary>
    public static void SaveRuleToFile(Rule rule, string filePath, JsonSerializerOptions? options = null)
        => File.WriteAllText(filePath, Serialize(rule, options));
}
