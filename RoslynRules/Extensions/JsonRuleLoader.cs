using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Extensions
{
    /// <summary>
    /// JSON serialization and deserialization for Rules and Workflows.
    /// Enables rule definitions to be stored in JSON configuration files,
    /// loaded at runtime without code changes.
    /// </summary>
    public static class JsonRuleLoader
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Serializes a workflow to JSON string.
        /// </summary>
        /// <param name="workflow">The workflow to serialize.</param>
        /// <returns>JSON string.</returns>
        public static string Serialize(Workflow workflow)
        {
            var dto = new WorkflowDto
            {
                Id = workflow.Id,
                Description = workflow.Description,
                IsActive = workflow.IsActive,
                Rules = workflow.Rules.Select(ToRuleDto).ToList()
            };
            return JsonSerializer.Serialize(dto, Options);
        }

        /// <summary>
        /// Deserializes a workflow from JSON string.
        /// </summary>
        /// <param name="json">JSON string.</param>
        /// <returns>Reconstructed workflow.</returns>
        public static Workflow Deserialize(string json)
        {
            var dto = JsonSerializer.Deserialize<WorkflowDto>(json, Options);
            if (dto == null)
                throw new JsonException("Failed to deserialize workflow from JSON.");

            var restored = new Workflow
            {
                Id = dto.Id,
                Description = dto.Description,
                IsActive = dto.IsActive,
                Rules = dto.Rules?.Select(ToRule).ToList() ?? new List<Rule>()
            };

            return restored;
        }

        /// <summary>
        /// Loads a workflow from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>Reconstructed workflow.</returns>
        public static Workflow LoadFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }

        /// <summary>
        /// Saves a workflow to a JSON file.
        /// </summary>
        /// <param name="workflow">The workflow to save.</param>
        /// <param name="filePath">Output file path.</param>
        public static void SaveToFile(Workflow workflow, string filePath)
        {
            var json = Serialize(workflow);
            File.WriteAllText(filePath, json);
        }

        // ==================== DTOs ====================

        private class WorkflowDto
        {
            public Guid Id { get; set; }
            public string Description { get; set; } = "";
            public bool IsActive { get; set; } = true;
            public List<RuleDto>? Rules { get; set; }
        }

        private class RuleDto
        {
            public Guid Id { get; set; }
            public string Description { get; set; } = "";
            public bool IsActive { get; set; } = true;
            public int Priority { get; set; } = 0;
            public string Expression { get; set; } = "";
            public string Action { get; set; } = "";
            public double? TimeoutSeconds { get; set; }
            public double? CacheDurationSeconds { get; set; }
            public Guid? DependsOnRuleId { get; set; }
            public Guid? ParentRuleId { get; set; }
            public List<RuleDto>? ChildRules { get; set; }
        }

        private static RuleDto ToRuleDto(Rule rule)
        {
            return new RuleDto
            {
                Id = rule.Id,
                Description = rule.Description,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                Expression = rule.Expression,
                Action = rule.Action,
                TimeoutSeconds = rule.Timeout?.TotalSeconds,
                CacheDurationSeconds = rule.CacheDuration?.TotalSeconds,
                DependsOnRuleId = rule.DependsOnRuleId,
                ParentRuleId = rule.ParentRuleId,
                ChildRules = rule.ChildRules?.Any() == true
                    ? rule.ChildRules.Select(ToRuleDto).ToList()
                    : null
            };
        }

        private static Rule ToRule(RuleDto dto)
        {
            var rule = new Rule
            {
                Id = dto.Id,
                Description = dto.Description,
                IsActive = dto.IsActive,
                Priority = dto.Priority,
                Expression = dto.Expression,
                Action = dto.Action,
                Timeout = dto.TimeoutSeconds.HasValue ? TimeSpan.FromSeconds(dto.TimeoutSeconds.Value) : null,
                CacheDuration = dto.CacheDurationSeconds.HasValue ? TimeSpan.FromSeconds(dto.CacheDurationSeconds.Value) : null,
                DependsOnRuleId = dto.DependsOnRuleId,
                ParentRuleId = dto.ParentRuleId
            };

            if (dto.ChildRules?.Any() == true)
            {
                foreach (var childDto in dto.ChildRules)
                {
                    var child = ToRule(childDto);
                    rule.ChildRules.Add(child);
                }
            }

            return rule;
        }
    }
}
