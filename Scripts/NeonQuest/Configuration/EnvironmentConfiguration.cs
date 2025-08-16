using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonQuest.Configuration
{
    [Serializable]
    public class EnvironmentConfiguration
    {
        // Corridor Configuration
        public float CorridorGenerationDistance { get; set; } = 50.0f;
        public float CorridorCleanupDistance { get; set; } = 100.0f;
        public List<string> VariationSeedFactors { get; set; } = new List<string>();
        
        // Lighting Configuration
        public float NeonResponseDistance { get; set; } = 5.0f;
        public Vector2 BrightnessMultiplierRange { get; set; } = new Vector2(0.5f, 2.0f);
        public float LightingTransitionDuration { get; set; } = 2.0f;
        
        // Atmosphere Configuration
        public Vector2 FogDensityRange { get; set; } = new Vector2(0.1f, 0.8f);
        public Vector2 AmbientVolumeRange { get; set; } = new Vector2(0.3f, 0.9f);
        public float AtmosphereTransitionSpeed { get; set; } = 0.1f;
        
        // Rules
        public List<GenerationRule> Rules { get; set; } = new List<GenerationRule>();
        
        // Performance Settings
        public int MaxActiveSegments { get; set; } = 10;
        public float PerformanceThrottleThreshold { get; set; } = 60.0f;
        public float PerformanceThreshold { get; set; } = 0.8f;
        
        public EnvironmentConfiguration()
        {
            InitializeDefaults();
        }
        
        private void InitializeDefaults()
        {
            VariationSeedFactors.AddRange(new[] { "player_speed", "time_of_day", "zone_type" });
        }
        
        public bool IsValid()
        {
            return CorridorGenerationDistance > 0 &&
                   CorridorCleanupDistance > CorridorGenerationDistance &&
                   NeonResponseDistance > 0 &&
                   LightingTransitionDuration > 0 &&
                   AtmosphereTransitionSpeed > 0;
        }
        
        public GenerationRule GetRuleByName(string ruleName)
        {
            return Rules.Find(r => r.RuleName == ruleName);
        }
        
        public List<GenerationRule> GetRulesByPriority()
        {
            var sortedRules = new List<GenerationRule>(Rules);
            sortedRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return sortedRules;
        }
        
        public void AddRule(GenerationRule rule)
        {
            if (rule != null && rule.IsValid())
            {
                Rules.Add(rule);
            }
        }
        
        public bool RemoveRule(string ruleName)
        {
            return Rules.RemoveAll(r => r.RuleName == ruleName) > 0;
        }
    }
}