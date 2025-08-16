using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeonQuest.Configuration
{
    public class ConfigurationValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            
            public void AddError(string error)
            {
                Errors.Add(error);
                IsValid = false;
            }
            
            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
            }
        }
        
        private static readonly HashSet<string> ValidVariationFactors = new HashSet<string>
        {
            "player_speed", "time_of_day", "zone_type", "movement_pattern", "game_time",
            "machinery_noise", "industrial_density", "music_beat", "crowd_density",
            "echo_intensity", "darkness_level", "security_level"
        };
        
        private static readonly HashSet<string> ValidConditionTypes = new HashSet<string>
        {
            "PlayerSpeed", "GameTime", "ZoneType", "DwellTime", "MovementPattern",
            "PlayerPosition", "MusicBeat", "ZoneChange"
        };
        
        private static readonly HashSet<string> ValidOperators = new HashSet<string>
        {
            "Equals", "NotEquals", "GreaterThan", "LessThan", 
            "GreaterThanOrEqual", "LessThanOrEqual", "Contains", "NotContains"
        };
        
        private static readonly HashSet<string> ValidActionTypes = new HashSet<string>
        {
            "GenerateLayout", "AdjustLighting", "ChangeFogDensity", "ModifyAudio",
            "TransitionAudioZone", "TriggerEffect", "CreateSpatialAudio"
        };
        
        public static ValidationResult ValidateConfiguration(EnvironmentConfiguration config)
        {
            var result = new ValidationResult { IsValid = true };
            
            if (config == null)
            {
                result.AddError("Configuration is null");
                return result;
            }
            
            ValidateCorridorSettings(config, result);
            ValidateLightingSettings(config, result);
            ValidateAtmosphereSettings(config, result);
            ValidatePerformanceSettings(config, result);
            ValidateRules(config, result);
            
            return result;
        }
        
        private static void ValidateCorridorSettings(EnvironmentConfiguration config, ValidationResult result)
        {
            if (config.CorridorGenerationDistance <= 0)
            {
                result.AddError("Corridor generation distance must be greater than 0");
            }
            
            if (config.CorridorCleanupDistance <= config.CorridorGenerationDistance)
            {
                result.AddError("Corridor cleanup distance must be greater than generation distance");
            }
            
            if (config.CorridorCleanupDistance < config.CorridorGenerationDistance * 1.5f)
            {
                result.AddWarning("Cleanup distance should be at least 1.5x generation distance to avoid pop-in effects");
            }
            
            foreach (var factor in config.VariationSeedFactors)
            {
                if (!ValidVariationFactors.Contains(factor))
                {
                    result.AddWarning($"Unknown variation seed factor: '{factor}'. Valid options: {string.Join(", ", ValidVariationFactors)}");
                }
            }
        }
        
        private static void ValidateLightingSettings(EnvironmentConfiguration config, ValidationResult result)
        {
            if (config.NeonResponseDistance <= 0)
            {
                result.AddError("Neon response distance must be greater than 0");
            }
            
            if (config.BrightnessMultiplierRange.x < 0 || config.BrightnessMultiplierRange.y < 0)
            {
                result.AddError("Brightness multiplier range values must be non-negative");
            }
            
            if (config.BrightnessMultiplierRange.x >= config.BrightnessMultiplierRange.y)
            {
                result.AddError("Brightness multiplier range minimum must be less than maximum");
            }
            
            if (config.LightingTransitionDuration <= 0)
            {
                result.AddError("Lighting transition duration must be greater than 0");
            }
            
            if (config.LightingTransitionDuration > 10.0f)
            {
                result.AddWarning("Lighting transition duration over 10 seconds may feel sluggish");
            }
        }
        
        private static void ValidateAtmosphereSettings(EnvironmentConfiguration config, ValidationResult result)
        {
            if (config.FogDensityRange.x < 0 || config.FogDensityRange.y < 0)
            {
                result.AddError("Fog density range values must be non-negative");
            }
            
            if (config.FogDensityRange.x >= config.FogDensityRange.y)
            {
                result.AddError("Fog density range minimum must be less than maximum");
            }
            
            if (config.FogDensityRange.y > 1.0f)
            {
                result.AddWarning("Fog density values above 1.0 may severely impact visibility");
            }
            
            if (config.AmbientVolumeRange.x < 0 || config.AmbientVolumeRange.y < 0)
            {
                result.AddError("Ambient volume range values must be non-negative");
            }
            
            if (config.AmbientVolumeRange.y > 1.0f)
            {
                result.AddWarning("Ambient volume values above 1.0 may cause audio distortion");
            }
            
            if (config.AtmosphereTransitionSpeed <= 0)
            {
                result.AddError("Atmosphere transition speed must be greater than 0");
            }
        }
        
        private static void ValidatePerformanceSettings(EnvironmentConfiguration config, ValidationResult result)
        {
            if (config.MaxActiveSegments <= 0)
            {
                result.AddError("Max active segments must be greater than 0");
            }
            
            if (config.MaxActiveSegments > 20)
            {
                result.AddWarning("More than 20 active segments may impact performance on lower-end hardware");
            }
            
            if (config.PerformanceThrottleThreshold <= 0)
            {
                result.AddError("Performance throttle threshold must be greater than 0");
            }
            
            if (config.PerformanceThrottleThreshold < 30.0f)
            {
                result.AddWarning("Performance throttle threshold below 30 FPS may result in poor user experience");
            }
        }
        
        private static void ValidateRules(EnvironmentConfiguration config, ValidationResult result)
        {
            if (config.Rules == null)
            {
                result.AddWarning("No generation rules defined - environment will be static");
                return;
            }
            
            var ruleNames = new HashSet<string>();
            
            foreach (var rule in config.Rules)
            {
                ValidateRule(rule, result, ruleNames);
            }
            
            if (config.Rules.Count == 0)
            {
                result.AddWarning("No generation rules defined - environment will be static");
            }
        }
        
        private static void ValidateRule(GenerationRule rule, ValidationResult result, HashSet<string> ruleNames)
        {
            if (string.IsNullOrEmpty(rule.RuleName))
            {
                result.AddError("Rule name cannot be empty");
                return;
            }
            
            if (ruleNames.Contains(rule.RuleName))
            {
                result.AddError($"Duplicate rule name: '{rule.RuleName}'");
            }
            else
            {
                ruleNames.Add(rule.RuleName);
            }
            
            if (rule.Priority < 0)
            {
                result.AddError($"Rule '{rule.RuleName}': Priority cannot be negative");
            }
            
            if (rule.Cooldown < 0)
            {
                result.AddError($"Rule '{rule.RuleName}': Cooldown cannot be negative");
            }
            
            if (rule.Conditions == null || rule.Conditions.Count == 0)
            {
                result.AddError($"Rule '{rule.RuleName}': Must have at least one condition");
            }
            else
            {
                foreach (var condition in rule.Conditions)
                {
                    ValidateCondition(condition, rule.RuleName, result);
                }
            }
            
            if (rule.Actions == null || rule.Actions.Count == 0)
            {
                result.AddError($"Rule '{rule.RuleName}': Must have at least one action");
            }
            else
            {
                foreach (var action in rule.Actions)
                {
                    ValidateAction(action, rule.RuleName, result);
                }
            }
        }
        
        private static void ValidateCondition(TriggerCondition condition, string ruleName, ValidationResult result)
        {
            if (!ValidConditionTypes.Contains(condition.Type.ToString()))
            {
                result.AddError($"Rule '{ruleName}': Invalid condition type '{condition.Type}'");
            }
            
            if (!ValidOperators.Contains(condition.Operator.ToString()))
            {
                result.AddError($"Rule '{ruleName}': Invalid operator '{condition.Operator}'");
            }
            
            if (condition.Value == null)
            {
                result.AddError($"Rule '{ruleName}': Condition value cannot be null");
            }
            
            // Type-specific validation
            switch (condition.Type)
            {
                case TriggerCondition.ConditionType.PlayerSpeed:
                    if (condition.Value is float speed && speed < 0)
                    {
                        result.AddWarning($"Rule '{ruleName}': Negative player speed condition may never trigger");
                    }
                    break;
                    
                case TriggerCondition.ConditionType.GameTime:
                    if (condition.Value is float time && time < 0)
                    {
                        result.AddError($"Rule '{ruleName}': Game time cannot be negative");
                    }
                    break;
                    
                case TriggerCondition.ConditionType.DwellTime:
                    if (condition.Value is float dwell && dwell < 0)
                    {
                        result.AddError($"Rule '{ruleName}': Dwell time cannot be negative");
                    }
                    break;
            }
        }
        
        private static void ValidateAction(GenerationAction action, string ruleName, ValidationResult result)
        {
            if (!ValidActionTypes.Contains(action.Type.ToString()))
            {
                result.AddError($"Rule '{ruleName}': Invalid action type '{action.Type}'");
            }
            
            if (string.IsNullOrEmpty(action.Target))
            {
                result.AddWarning($"Rule '{ruleName}': Action '{action.Type}' has no target specified");
            }
            
            // Validate common parameters
            if (action.HasParameter("intensity"))
            {
                var intensity = action.GetParameter<float>("intensity");
                if (intensity < 0)
                {
                    result.AddWarning($"Rule '{ruleName}': Negative intensity values may cause unexpected behavior");
                }
                if (intensity > 5.0f)
                {
                    result.AddWarning($"Rule '{ruleName}': Very high intensity values (>{intensity}) may cause performance issues");
                }
            }
            
            if (action.HasParameter("duration"))
            {
                var duration = action.GetParameter<float>("duration");
                if (duration <= 0)
                {
                    result.AddError($"Rule '{ruleName}': Duration must be greater than 0");
                }
            }
        }
        
        public static string FormatValidationResult(ValidationResult result)
        {
            var output = new List<string>();
            
            if (result.IsValid)
            {
                output.Add("✓ Configuration validation passed");
            }
            else
            {
                output.Add("✗ Configuration validation failed");
            }
            
            if (result.Errors.Count > 0)
            {
                output.Add("\nErrors:");
                foreach (var error in result.Errors)
                {
                    output.Add($"  • {error}");
                }
            }
            
            if (result.Warnings.Count > 0)
            {
                output.Add("\nWarnings:");
                foreach (var warning in result.Warnings)
                {
                    output.Add($"  • {warning}");
                }
            }
            
            return string.Join("\n", output);
        }
    }
}