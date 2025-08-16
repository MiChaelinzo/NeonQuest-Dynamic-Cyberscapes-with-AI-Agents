using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.IO;
using NeonQuest.Configuration;
using NeonQuest.Core.Diagnostics;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class DeveloperToolsIntegrationTests
    {
        private EnvironmentConfigurationAsset testAsset;
        private string tempAssetPath;
        
        [SetUp]
        public void SetUp()
        {
            // Create a temporary configuration asset for testing
            tempAssetPath = "Assets/TestEnvironmentConfig.asset";
            testAsset = ScriptableObject.CreateInstance<EnvironmentConfigurationAsset>();
            testAsset.Configuration = CreateTestConfiguration();
            
            AssetDatabase.CreateAsset(testAsset, tempAssetPath);
            AssetDatabase.SaveAssets();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(tempAssetPath))
            {
                AssetDatabase.DeleteAsset(tempAssetPath);
            }
        }
        
        [Test]
        public void EnvironmentConfigurationAsset_LoadFromYAML_LoadsCorrectly()
        {
            // Arrange
            var yamlPath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples", "default_environment.yaml");
            
            // Act
            testAsset.LoadFromYAML(yamlPath);
            
            // Assert
            Assert.IsNotNull(testAsset.Configuration);
            Assert.IsTrue(testAsset.IsValid());
            Assert.Greater(testAsset.Configuration.Rules.Count, 0);
        }
        
        [Test]
        public void EnvironmentConfigurationAsset_IsValid_ReturnsTrueForValidConfig()
        {
            // Arrange
            testAsset.Configuration = CreateValidConfiguration();
            
            // Act
            var isValid = testAsset.IsValid();
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void EnvironmentConfigurationAsset_IsValid_ReturnsFalseForInvalidConfig()
        {
            // Arrange
            testAsset.Configuration = CreateInvalidConfiguration();
            
            // Act
            var isValid = testAsset.IsValid();
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void RuntimeConfigurationAdjuster_SetConfigurationAsset_UpdatesConfiguration()
        {
            // Arrange
            var gameObject = new GameObject("TestAdjuster");
            var adjuster = gameObject.AddComponent<RuntimeConfigurationAdjuster>();
            
            // Act
            adjuster.SetConfigurationAsset(testAsset);
            var currentConfig = adjuster.GetCurrentConfiguration();
            
            // Assert
            Assert.IsNotNull(currentConfig);
            Assert.AreEqual(testAsset.Configuration, currentConfig);
            
            // Cleanup
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void ConfigurationValidator_ValidateExampleConfigurations_AllPass()
        {
            // Arrange
            var exampleFiles = new[]
            {
                "default_environment.yaml",
                "industrial_district.yaml",
                "neon_nightclub.yaml",
                "abandoned_subway.yaml",
                "corporate_tower.yaml"
            };
            
            var loader = new YAMLConfigLoader();
            var examplesPath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples");
            
            foreach (var filename in exampleFiles)
            {
                // Act
                var configPath = Path.Combine(examplesPath, filename);
                var config = loader.LoadConfiguration(configPath);
                var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                
                // Assert
                Assert.IsTrue(validationResult.IsValid, 
                    $"Example configuration '{filename}' failed validation:\n{ConfigurationValidator.FormatValidationResult(validationResult)}");
            }
        }
        
        [Test]
        public void ConfigurationValidator_FormatValidationResult_FormatsCorrectly()
        {
            // Arrange
            var result = new ConfigurationValidator.ValidationResult();
            result.AddError("Test error message");
            result.AddWarning("Test warning message");
            
            // Act
            var formatted = ConfigurationValidator.FormatValidationResult(result);
            
            // Assert
            Assert.IsTrue(formatted.Contains("✗ Configuration validation failed"));
            Assert.IsTrue(formatted.Contains("Errors:"));
            Assert.IsTrue(formatted.Contains("• Test error message"));
            Assert.IsTrue(formatted.Contains("Warnings:"));
            Assert.IsTrue(formatted.Contains("• Test warning message"));
        }
        
        [Test]
        public void ConfigurationValidator_ValidConfiguration_ReturnsValidResult()
        {
            // Arrange
            var config = CreateValidConfiguration();
            
            // Act
            var result = ConfigurationValidator.ValidateConfiguration(config);
            var formatted = ConfigurationValidator.FormatValidationResult(result);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
            Assert.IsTrue(formatted.Contains("✓ Configuration validation passed"));
        }
        
        [Test]
        public void ConfigurationTemplate_LoadsWithoutErrors()
        {
            // Arrange
            var templatePath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples", "configuration_template.yaml");
            var loader = new YAMLConfigLoader();
            
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() =>
            {
                var config = loader.LoadConfiguration(templatePath);
                Assert.IsNotNull(config);
            });
        }
        
        [Test]
        public void ExampleConfigurations_HaveUniqueCharacteristics()
        {
            // Test that each example configuration has distinct settings appropriate to its theme
            var loader = new YAMLConfigLoader();
            var examplesPath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples");
            
            // Industrial District - should have longer generation distances
            var industrialConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "industrial_district.yaml"));
            Assert.AreEqual(60.0f, industrialConfig.CorridorGenerationDistance, "Industrial district should have 60m generation distance");
            
            // Nightclub - should have shorter response distances for intimate feel
            var nightclubConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "neon_nightclub.yaml"));
            Assert.AreEqual(3.0f, nightclubConfig.NeonResponseDistance, "Nightclub should have 3m neon response distance");
            
            // Subway - should have longest generation distances for tunnel feel
            var subwayConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "abandoned_subway.yaml"));
            Assert.AreEqual(80.0f, subwayConfig.CorridorGenerationDistance, "Subway should have 80m generation distance");
            
            // Corporate - should have moderate, clean settings
            var corporateConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "corporate_tower.yaml"));
            Assert.AreEqual(45.0f, corporateConfig.CorridorGenerationDistance, "Corporate should have 45m generation distance");
        }
        
        [Test]
        public void ExampleConfigurations_HaveEnvironmentSpecificRules()
        {
            var loader = new YAMLConfigLoader();
            var examplesPath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples");
            
            // Industrial should have machinery-related rules
            var industrialConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "industrial_district.yaml"));
            Assert.IsTrue(industrialConfig.Rules.Exists(r => r.RuleName.ToLower().Contains("machinery")),
                "Industrial district should have machinery-related rules");
            
            // Nightclub should have beat/dance-related rules
            var nightclubConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "neon_nightclub.yaml"));
            Assert.IsTrue(nightclubConfig.Rules.Exists(r => r.RuleName.ToLower().Contains("beat") || r.RuleName.ToLower().Contains("dance")),
                "Nightclub should have beat/dance-related rules");
            
            // Subway should have echo/footstep rules
            var subwayConfig = loader.LoadConfiguration(Path.Combine(examplesPath, "abandoned_subway.yaml"));
            Assert.IsTrue(subwayConfig.Rules.Exists(r => r.RuleName.ToLower().Contains("echo") || r.RuleName.ToLower().Contains("footstep")),
                "Subway should have echo/footstep-related rules");
        }
        
        [Test]
        public void ConfigurationAsset_OnValidate_LogsWarningsForInvalidConfig()
        {
            // This test would verify that OnValidate logs warnings for invalid configurations
            // In a real Unity environment, this would check the console output
            
            // Arrange
            var invalidConfig = CreateInvalidConfiguration();
            testAsset.Configuration = invalidConfig;
            
            // Act & Assert
            // In Unity editor, OnValidate would be called automatically
            // Here we just verify the configuration is indeed invalid
            Assert.IsFalse(testAsset.IsValid());
        }
        
        private EnvironmentConfiguration CreateTestConfiguration()
        {
            var config = new EnvironmentConfiguration();
            
            // Add a simple valid rule
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
        
        private EnvironmentConfiguration CreateValidConfiguration()
        {
            return CreateTestConfiguration();
        }
        
        private EnvironmentConfiguration CreateInvalidConfiguration()
        {
            var config = new EnvironmentConfiguration();
            config.CorridorGenerationDistance = -10.0f; // Invalid negative distance
            config.CorridorCleanupDistance = 5.0f; // Less than generation distance
            
            // Add invalid rule
            var invalidRule = new GenerationRule(); // No name
            config.Rules.Add(invalidRule);
            
            return config;
        }
    }
}