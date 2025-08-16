using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Configuration
{
    public class YAMLConfigLoader
    {
        private readonly NeonQuestLogger _logger;
        
        public YAMLConfigLoader()
        {
            _logger = new NeonQuestLogger("YAMLConfigLoader");
        }
        
        public EnvironmentConfiguration LoadConfiguration(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Configuration file not found: {filePath}. Using default configuration.");
                    return CreateDefaultConfiguration();
                }
                
                string yamlContent = File.ReadAllText(filePath);
                return ParseYAML(yamlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load configuration from {filePath}: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }
        
        public EnvironmentConfiguration LoadConfigurationFromString(string yamlContent)
        {
            return ParseYAML(yamlContent);
        }
        
        public EnvironmentConfiguration ParseYAML(string yamlContent)
        {
            var config = new EnvironmentConfiguration();
            var lines = yamlContent.Split('\n');
            
            try
            {
                ParseYAMLLines(lines, config);
                ValidateConfiguration(config);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse YAML configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }
        
        private void ParseYAMLLines(string[] lines, EnvironmentConfiguration config)
        {
            var currentSection = "";
            var currentRule = new GenerationRule();
            var inRuleSection = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                    
                if (IsSection(line))
                {
                    currentSection = GetSectionName(line);
                    inRuleSection = currentSection == "rules";
                    continue;
                }
                
                if (inRuleSection)
                {
                    ParseRuleLine(line, ref currentRule, config);
                }
                else
                {
                    ParseConfigurationLine(line, currentSection, config);
                }
            }
            
            // Add the last rule if it exists
            if (inRuleSection && currentRule.IsValid())
            {
                config.Rules.Add(currentRule);
            }
        }
        
        private bool IsSection(string line)
        {
            return line.EndsWith(":") && !line.Contains(" ");
        }
        
        private string GetSectionName(string line)
        {
            return line.TrimEnd(':');
        }
        
        private void ParseRuleLine(string line, ref GenerationRule currentRule, EnvironmentConfiguration config)
        {
            if (line.StartsWith("- name:"))
            {
                // Save previous rule if valid
                if (currentRule.IsValid())
                {
                    config.Rules.Add(currentRule);
                }
                
                // Start new rule
                currentRule = new GenerationRule();
                currentRule.RuleName = ExtractValue(line);
            }
            else if (line.StartsWith("  priority:"))
            {
                if (float.TryParse(ExtractValue(line), out float priority))
                    currentRule.Priority = priority;
            }
            else if (line.StartsWith("  cooldown:"))
            {
                if (float.TryParse(ExtractValue(line), out float cooldown))
                    currentRule.Cooldown = cooldown;
            }
            else if (line.StartsWith("  conditions:"))
            {
                // Conditions will be parsed in subsequent lines
            }
            else if (line.StartsWith("    - type:"))
            {
                var condition = ParseCondition(line);
                if (condition != null)
                    currentRule.Conditions.Add(condition);
            }
            else if (line.StartsWith("  actions:"))
            {
                // Actions will be parsed in subsequent lines
            }
            else if (line.StartsWith("    - action:"))
            {
                var action = ParseAction(line);
                if (action != null)
                    currentRule.Actions.Add(action);
            }
        }
        
        private void ParseConfigurationLine(string line, string section, EnvironmentConfiguration config)
        {
            var keyValue = line.Split(':');
            if (keyValue.Length != 2) return;
            
            string key = keyValue[0].Trim();
            string value = keyValue[1].Trim();
            
            switch (section)
            {
                case "corridors":
                    ParseCorridorConfig(key, value, config);
                    break;
                case "lighting":
                    ParseLightingConfig(key, value, config);
                    break;
                case "atmosphere":
                    ParseAtmosphereConfig(key, value, config);
                    break;
            }
        }
        
        private void ParseCorridorConfig(string key, string value, EnvironmentConfiguration config)
        {
            switch (key)
            {
                case "generation_distance":
                    if (float.TryParse(value, out float genDist))
                        config.CorridorGenerationDistance = genDist;
                    break;
                case "cleanup_distance":
                    if (float.TryParse(value, out float cleanupDist))
                        config.CorridorCleanupDistance = cleanupDist;
                    break;
            }
        }
        
        private void ParseLightingConfig(string key, string value, EnvironmentConfiguration config)
        {
            switch (key)
            {
                case "neon_response_distance":
                    if (float.TryParse(value, out float responseDistance))
                        config.NeonResponseDistance = responseDistance;
                    break;
                case "transition_duration":
                    if (float.TryParse(value, out float transitionDuration))
                        config.LightingTransitionDuration = transitionDuration;
                    break;
            }
        }
        
        private void ParseAtmosphereConfig(string key, string value, EnvironmentConfiguration config)
        {
            switch (key)
            {
                case "transition_speed":
                    if (float.TryParse(value, out float transitionSpeed))
                        config.AtmosphereTransitionSpeed = transitionSpeed;
                    break;
            }
        }
        
        private TriggerCondition ParseCondition(string line)
        {
            // Simple parsing for condition format: "- type: PlayerSpeed operator: GreaterThan value: 5.0"
            var parts = line.Split(' ');
            if (parts.Length < 6) return null;
            
            var condition = new TriggerCondition();
            
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "type:" && Enum.TryParse<TriggerCondition.ConditionType>(parts[i + 1], out var type))
                    condition.Type = type;
                else if (parts[i] == "operator:" && Enum.TryParse<TriggerCondition.ComparisonOperator>(parts[i + 1], out var op))
                    condition.Operator = op;
                else if (parts[i] == "value:")
                    condition.Value = ParseValue(parts[i + 1]);
            }
            
            return condition.IsValid() ? condition : null;
        }
        
        private GenerationAction ParseAction(string line)
        {
            // Simple parsing for action format: "- action: GenerateLayout target: corridor intensity: 1.0"
            var parts = line.Split(' ');
            if (parts.Length < 4) return null;
            
            var action = new GenerationAction();
            
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "action:" && Enum.TryParse<GenerationAction.ActionType>(parts[i + 1], out var type))
                    action.Type = type;
                else if (parts[i] == "target:")
                    action.Target = parts[i + 1];
                else if (parts[i] == "intensity:" && float.TryParse(parts[i + 1], out float intensity))
                    action.Intensity = intensity;
                else if (parts[i] == "duration:" && float.TryParse(parts[i + 1], out float duration))
                    action.Duration = duration;
            }
            
            return action.IsValid() ? action : null;
        }
        
        private object ParseValue(string valueStr)
        {
            if (float.TryParse(valueStr, out float floatVal))
                return floatVal;
            if (int.TryParse(valueStr, out int intVal))
                return intVal;
            if (bool.TryParse(valueStr, out bool boolVal))
                return boolVal;
            return valueStr;
        }
        
        private string ExtractValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1) return "";
            return line.Substring(colonIndex + 1).Trim();
        }
        
        private void ValidateConfiguration(EnvironmentConfiguration config)
        {
            if (config.CorridorGenerationDistance <= 0)
            {
                _logger.LogWarning("Invalid corridor generation distance. Using default value.");
                config.CorridorGenerationDistance = 50.0f;
            }
            
            if (config.NeonResponseDistance <= 0)
            {
                _logger.LogWarning("Invalid neon response distance. Using default value.");
                config.NeonResponseDistance = 5.0f;
            }
            
            foreach (var rule in config.Rules)
            {
                if (!rule.IsValid())
                {
                    _logger.LogWarning($"Invalid rule found: {rule.RuleName}. Skipping.");
                }
            }
        }
        
        private EnvironmentConfiguration CreateDefaultConfiguration()
        {
            return new EnvironmentConfiguration
            {
                CorridorGenerationDistance = 50.0f,
                CorridorCleanupDistance = 100.0f,
                NeonResponseDistance = 5.0f,
                LightingTransitionDuration = 2.0f,
                AtmosphereTransitionSpeed = 0.1f,
                Rules = new List<GenerationRule>()
            };
        }
    }
}