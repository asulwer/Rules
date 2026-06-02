using RoslynRules.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Extensions
{
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
        /// <param name="workflow">The workflow to serialize.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>JSON string.</returns>
        public static string Serialize(Workflow workflow, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(workflow, options ?? DefaultOptions);
        }

        /// <summary>
        /// Serializes a rule to JSON string.
        /// </summary>
        /// <param name="rule">The rule to serialize.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>JSON string.</returns>
        public static string Serialize(Rule rule, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(rule, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes a workflow from JSON string.
        /// </summary>
        /// <param name="json">JSON string.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>Reconstructed workflow.</returns>
        /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
        public static Workflow DeserializeWorkflow(string json, JsonSerializerOptions? options = null)
        {
            var workflow = JsonSerializer.Deserialize<Workflow>(json, options ?? DefaultOptions);
            if (workflow == null)
                throw new JsonException("Failed to deserialize workflow from JSON.");
            return workflow;
        }

        /// <summary>
        /// Deserializes a rule from JSON string.
        /// </summary>
        /// <param name="json">JSON string.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>Reconstructed rule.</returns>
        /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
        public static Rule DeserializeRule(string json, JsonSerializerOptions? options = null)
        {
            var rule = JsonSerializer.Deserialize<Rule>(json, options ?? DefaultOptions);
            if (rule == null)
                throw new JsonException("Failed to deserialize rule from JSON.");
            return rule;
        }

        /// <summary>
        /// Loads a workflow from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>Reconstructed workflow.</returns>
        public static Workflow LoadWorkflowFromFile(string filePath, JsonSerializerOptions? options = null)
        {
            var json = File.ReadAllText(filePath);
            return DeserializeWorkflow(json, options);
        }

        /// <summary>
        /// Loads a rule from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        /// <returns>Reconstructed rule.</returns>
        public static Rule LoadRuleFromFile(string filePath, JsonSerializerOptions? options = null)
        {
            var json = File.ReadAllText(filePath);
            return DeserializeRule(json, options);
        }

        /// <summary>
        /// Saves a workflow to a JSON file.
        /// </summary>
        /// <param name="workflow">The workflow to save.</param>
        /// <param name="filePath">Output file path.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        public static void SaveWorkflowToFile(Workflow workflow, string filePath, JsonSerializerOptions? options = null)
        {
            var json = Serialize(workflow, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Saves a rule to a JSON file.
        /// </summary>
        /// <param name="rule">The rule to save.</param>
        /// <param name="filePath">Output file path.</param>
        /// <param name="options">Optional serializer options. Uses DefaultOptions if null.</param>
        public static void SaveRuleToFile(Rule rule, string filePath, JsonSerializerOptions? options = null)
        {
            var json = Serialize(rule, options);
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Custom JSON converter for TimeSpan that serializes as total seconds
    /// for human-readable JSON output.
    /// </summary>
    internal sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                var seconds = reader.GetDouble();
                return TimeSpan.FromSeconds(seconds);
            }

            throw new JsonException($"Expected number or null for TimeSpan, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value.TotalSeconds);
            else
                writer.WriteNullValue();
        }
    }
}
