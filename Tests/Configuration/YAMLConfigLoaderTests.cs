using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class YAMLConfigLoaderTests
    {
        private YAMLConfigLoader _loader;
        private string _testConfigPath;
        
        [SetUp]
        public void Setup()
        {
            _loader = new YAMLConfigLoader();
            _testConfigPath = Path.Combine(Application.temporaryCachePath, "test_config.yaml");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
        
        [Test]
        public void LoadConfiguration_ValidFile_ReturnsConfiguration()
        {
            // Arrange
            string yamlContent = @"
corridors:
  generation_distance: 75.0
  cleanup_distance: 150.0

lighting:
  neon_response_distance: 8.0
  transition_duration: 3.0

atmosphere:
  transition_speed: 0.2

rules:
  - name: FastMovementRule
    priority: 2.0
    cooldown: 5.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 10.0
    actions:
      - action: GenerateLayout target: corridor intensity: 1.5
";
            File.WriteAllText(_testConfigPath, yamlContent);
            
            // Act
            var config = _loader.LoadConfiguration(_testConfigPath);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(75.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(150.0f, config.CorridorCleanupDistance);
            Assert.AreEqual(8.0f, config.NeonResponseDistance);
            Assert.AreEqual(3.0f, config.LightingTransitionDuration);
            Assert.AreEqual(0.2f, config.AtmosphereTransitionSpeed);
        }
        
        [Test]
        public void LoadConfiguration_NonExistentFile_ReturnsDefaultConfiguration()
        {
            // Act
            var config = _loader.LoadConfiguration("nonexistent.yaml");
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(50.0f, config.CorridorGenerationDistance); // Default value
            Assert.AreEqual(100.0f, config.CorridorCleanupDistance); // Default value
        }
        
        [Test]
        public void ParseYAML_ValidContent_ParsesCorrectly()
        {
            // Arrange
            string yamlContent = @"
corridors:
  generation_distance: 60.0

lighting:
  neon_response_distance: 7.0
";
            
            // Act
            var config = _loader.ParseYAML(yamlContent);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(60.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(7.0f, config.NeonResponseDistance);
        }
        
        [Test]
        public void ParseYAML_InvalidContent_ReturnsDefaultConfiguration()
        {
            // Arrange
            string invalidYaml = "invalid yaml content {[}";
            
            // Act
            var config = _loader.ParseYAML(invalidYaml);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(50.0f, config.CorridorGenerationDistance); // Default value
        }
        
        [Test]
        public void ParseYAML_EmptyContent_ReturnsDefaultConfiguration()
        {
            // Act
            var config = _loader.ParseYAML("");
            
            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.IsValid());
        }
        
        [Test]
        public void ParseYAML_WithComments_IgnoresComments()
        {
            // Arrange
            string yamlContent = @"
# This is a comment
corridors:
  generation_distance: 45.0  # Another comment
  # cleanup_distance: 90.0  # This line should be ignored
";
            
            // Act
            var config = _loader.ParseYAML(yamlContent);
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(45.0f, config.CorridorGenerationDistance);
            Assert.AreEqual(100.0f, config.CorridorCleanupDistance); // Should be default since commented out
        }
    }
}