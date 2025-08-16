using NUnit.Framework;
using System.IO;
using UnityEngine;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class ConfigurationExamplesTests
    {
        private YAMLConfigLoader configLoader;
        private string examplesPath;
        
        [SetUp]
        public void SetUp()
        {
            configLoader = new YAMLConfigLoader();
            examplesPath = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples");
        }
        
        [Test]
        public void DefaultEnvironmentConfig_LoadsSuccessfully()
        {
            // Arrange
            var configPath = Path.Combine(examplesPath, "default_environment.yaml");
            
            // Act
            var config = configLoader.LoadConfiguration(configPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
            Assert.Greater(config.Rules.Count, 0);
        }
        
        [Test]
        public void IndustrialDistrictConfig_LoadsSuccessfully()
        {
            // Arrange
            var configPath = Path.Combine(examplesPath, "industrial_district.yaml");
            
            // Act
            var config = configLoader.LoadConfiguration(configPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
            Assert.AreEqual(60.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(120.0f, config.CorridorCleanupDistance);
            Assert.Contains("machinery_noise", config.VariationSeedFactors);
        }
        
        [Test]
        public void NeonNightclubConfig_LoadsSuccessfully()
        {
            // Arrange
            var configPath = Path.Combine(examplesPath, "neon_nightclub.yaml");
            
            // Act
            var config = configLoader.LoadConfiguration(configPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
            Assert.AreEqual(40.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(3.0f, config.NeonResponseDistance);
            Assert.Contains("music_beat", config.VariationSeedFactors);
        }
        
        [Test]
        public void AbandonedSubwayConfig_LoadsSuccessfully()
        {
            // Arrange
            var configPath = Path.Combine(examplesPath, "abandoned_subway.yaml");
            
            // Act
            var config = configLoader.LoadConfiguration(configPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
            Assert.AreEqual(80.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(160.0f, config.CorridorCleanupDistance);
            Assert.Contains("echo_intensity", config.VariationSeedFactors);
        }
        
        [Test]
        public void CorporateTowerConfig_LoadsSuccessfully()
        {
            // Arrange
            var configPath = Path.Combine(examplesPath, "corporate_tower.yaml");
            
            // Act
            var config = configLoader.LoadConfiguration(configPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
            Assert.AreEqual(45.0f, config.CorridorGenerationDistance);
            Assert.Contains("security_level", config.VariationSeedFactors);
        }
        
        [Test]
        public void AllExampleConfigs_HaveUniqueRuleNames()
        {
            // Arrange
            var configFiles = new[]
            {
                "default_environment.yaml",
                "industrial_district.yaml",
                "neon_nightclub.yaml",
                "abandoned_subway.yaml",
                "corporate_tower.yaml"
            };
            
            foreach (var configFile in configFiles)
            {
                // Act
                var configPath = Path.Combine(examplesPath, configFile);
                var config = configLoader.LoadConfiguration(configPath);
                
                // Assert
                Assert.IsNotNull(config, $"Failed to load {configFile}");
                
                var ruleNames = new System.Collections.Generic.HashSet<string>();
                foreach (var rule in config.Rules)
                {
                    Assert.IsFalse(ruleNames.Contains(rule.RuleName), 
                        $"Duplicate rule name '{rule.RuleName}' in {configFile}");
                    ruleNames.Add(rule.RuleName);
                }
            }
        }
        
        [Test]
        public void AllExampleConfigs_PassValidation()
        {
            // Arrange
            var configFiles = new[]
            {
                "default_environment.yaml",
                "industrial_district.yaml", 
                "neon_nightclub.yaml",
                "abandoned_subway.yaml",
                "corporate_tower.yaml"
            };
            
            foreach (var configFile in configFiles)
            {
                // Act
                var configPath = Path.Combine(examplesPath, configFile);
                var config = configLoader.LoadConfiguration(configPath);
                var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                
                // Assert
                Assert.IsTrue(validationResult.IsValid, 
                    $"Validation failed for {configFile}:\n{ConfigurationValidator.FormatValidationResult(validationResult)}");
            }
        }
        
        [Test]
        public void ExampleConfigs_HaveAppropriatePerformanceSettings()
        {
            // Arrange
            var configFiles = new[]
            {
                ("industrial_district.yaml", 8),    // Complex industrial scenes
                ("neon_nightclub.yaml", 12),        // High-frequency lighting changes
                ("abandoned_subway.yaml", 6),       // Atmospheric effects focus
                ("corporate_tower.yaml", 15),       // Clean, optimized environment
                ("default_environment.yaml", 10)    // Balanced default
            };
            
            foreach (var (configFile, expectedMaxSegments) in configFiles)
            {
                // Act
                var configPath = Path.Combine(examplesPath, configFile);
                var config = configLoader.LoadConfiguration(configPath);
                
                // Assert
                Assert.AreEqual(expectedMaxSegments, config.MaxActiveSegments,
                    $"Unexpected max active segments in {configFile}");
                Assert.GreaterOrEqual(config.PerformanceThrottleThreshold, 50.0f,
                    $"Performance threshold too low in {configFile}");
            }
        }
        
        [Test]
        public void ExampleConfigs_HaveEnvironmentSpecificRules()
        {
            // Test that each environment has rules specific to its theme
            
            // Industrial District should have machinery-related rules
            var industrialConfig = configLoader.LoadConfiguration(
                Path.Combine(examplesPath, "industrial_district.yaml"));
            Assert.IsTrue(industrialConfig.Rules.Exists(r => r.RuleName.Contains("Machinery")),
                "Industrial district should have machinery-related rules");
            
            // Nightclub should have beat/music-related rules
            var nightclubConfig = configLoader.LoadConfiguration(
                Path.Combine(examplesPath, "neon_nightclub.yaml"));
            Assert.IsTrue(nightclubConfig.Rules.Exists(r => r.RuleName.Contains("Beat") || r.RuleName.Contains("Music")),
                "Nightclub should have beat/music-related rules");
            
            // Subway should have echo/footstep rules
            var subwayConfig = configLoader.LoadConfiguration(
                Path.Combine(examplesPath, "abandoned_subway.yaml"));
            Assert.IsTrue(subwayConfig.Rules.Exists(r => r.RuleName.Contains("Echo") || r.RuleName.Contains("Footstep")),
                "Subway should have echo/footstep-related rules");
            
            // Corporate should have motion sensor rules
            var corporateConfig = configLoader.LoadConfiguration(
                Path.Combine(examplesPath, "corporate_tower.yaml"));
            Assert.IsTrue(corporateConfig.Rules.Exists(r => r.RuleName.Contains("Sensor") || r.RuleName.Contains("Motion")),
                "Corporate tower should have sensor-related rules");
        }
    }
}