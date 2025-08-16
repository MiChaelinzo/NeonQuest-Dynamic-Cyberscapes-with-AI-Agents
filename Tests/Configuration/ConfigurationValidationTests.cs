using NUnit.Framework;
using NeonQuest.Configuration;
using UnityEngine;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class ConfigurationValidationTests
    {
        [Test]
        public void ValidateConfiguration_ValidConfig_ReturnsTrue()
        {
            // Arrange
            var config = CreateValidConfiguration();
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }
        
        [Test]
        public void ValidateConfiguration_NullConfig_ReturnsError()
        {
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(null);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Contains("Configuration is null", result.Errors);
        }
        
        [Test]
        public void ValidateConfiguration_InvalidCorridorDistance_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.CorridorGenerationDistance = -5.0f;
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("generation distance must be greater than 0")));
        }
        
        [Test]
        public void ValidateConfiguration_CleanupDistanceTooSmall_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.CorridorGenerationDistance = 50.0f;
            config.CorridorCleanupDistance = 40.0f; // Less than generation distance
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("cleanup distance must be greater than generation distance")));
        }
        
        [Test]
        public void ValidateConfiguration_CleanupDistanceCloseToGeneration_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.CorridorGenerationDistance = 50.0f;
            config.CorridorCleanupDistance = 60.0f; // Only 1.2x generation distance
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid); // Still valid, but has warning
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("should be at least 1.5x generation distance")));
        }
        
        [Test]
        public void ValidateConfiguration_InvalidVariationFactor_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.VariationSeedFactors.Add("invalid_factor");
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid); // Still valid, but has warning
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Unknown variation seed factor: 'invalid_factor'")));
        }
        
        [Test]
        public void ValidateConfiguration_InvalidBrightnessRange_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.BrightnessMultiplierRange = new Vector2(2.0f, 1.0f); // Min > Max
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("minimum must be less than maximum")));
        }
        
        [Test]
        public void ValidateConfiguration_NegativeBrightness_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.BrightnessMultiplierRange = new Vector2(-0.5f, 2.0f);
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("must be non-negative")));
        }
        
        [Test]
        public void ValidateConfiguration_HighFogDensity_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.FogDensityRange = new Vector2(0.5f, 1.5f); // Max > 1.0
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("may severely impact visibility")));
        }
        
        [Test]
        public void ValidateConfiguration_TooManyActiveSegments_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.MaxActiveSegments = 25; // > 20
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("may impact performance")));
        }
        
        [Test]
        public void ValidateConfiguration_LowPerformanceThreshold_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.PerformanceThrottleThreshold = 25.0f; // < 30
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("may result in poor user experience")));
        }
        
        [Test]
        public void ValidateConfiguration_NoRules_ReturnsWarning()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.Rules.Clear();
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("environment will be static")));
        }
        
        [Test]
        public void ValidateConfiguration_DuplicateRuleNames_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            var duplicateRule = new GenerationRule("TestRule");
            duplicateRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f));
            duplicateRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.GenerateLayout));
            config.Rules.Add(duplicateRule);
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Duplicate rule name: 'TestRule'")));
        }
        
        [Test]
        public void ValidateConfiguration_RuleWithoutConditions_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            var invalidRule = new GenerationRule("InvalidRule");
            invalidRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.GenerateLayout));
            // No conditions added
            config.Rules.Add(invalidRule);
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Must have at least one condition")));
        }
        
        [Test]
        public void ValidateConfiguration_RuleWithoutActions_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            var invalidRule = new GenerationRule("InvalidRule");
            invalidRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f));
            // No actions added
            config.Rules.Add(invalidRule);
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Must have at least one action")));
        }
        
        [Test]
        public void ValidateConfiguration_NegativeRulePriority_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.Rules[0].Priority = -1.0f;
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Priority cannot be negative")));
        }
        
        [Test]
        public void ValidateConfiguration_NegativeGameTimeCondition_ReturnsError()
        {
            // Arrange
            var config = CreateValidConfiguration();
            var rule = config.Rules[0];
            rule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.GameTime,
                TriggerCondition.ComparisonOperator.GreaterThan,
                -10.0f)); // Negative game time
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Game time cannot be negative")));
        }
        
        [Test]
        public void FormatValidationResult_WithErrorsAndWarnings_FormatsCorrectly()
        {
            // Arrange
            var result = new ConfigurationValidator.ValidationResult();
            result.AddError("Test error");
            result.AddWarning("Test warning");
            
            // Act
            var formatted = ConfigurationValidator.FormatValidationResult(result);
            
            // Assert
            Assert.IsTrue(formatted.Contains("✗ Configuration validation failed"));
            Assert.IsTrue(formatted.Contains("Errors:"));
            Assert.IsTrue(formatted.Contains("• Test error"));
            Assert.IsTrue(formatted.Contains("Warnings:"));
            Assert.IsTrue(formatted.Contains("• Test warning"));
        }
        
        [Test]
        public void FormatValidationResult_ValidConfig_FormatsCorrectly()
        {
            // Arrange
            var result = new ConfigurationValidator.ValidationResult { IsValid = true };
            
            // Act
            var formatted = ConfigurationValidator.FormatValidationResult(result);
            
            // Assert
            Assert.IsTrue(formatted.Contains("✓ Configuration validation passed"));
        }
        
        private EnvironmentConfiguration CreateValidConfiguration()
        {
            var config = new EnvironmentConfiguration();
            
            // Add a valid rule
            var rule = new GenerationRule("TestRule");
            rule.Priority = 1.0f;
            rule.Cooldown = 5.0f;
            rule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f));
            rule.Actions.Add(new GenerationAction(GenerationAction.ActionType.GenerateLayout, "corridor"));
            
            config.Rules.Add(rule);
            
            return config;
        }
    }
}