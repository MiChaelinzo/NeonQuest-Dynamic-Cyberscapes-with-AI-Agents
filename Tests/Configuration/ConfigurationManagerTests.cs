using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class ConfigurationManagerTests
    {
        private GameObject _testGameObject;
        private ConfigurationManager _configManager;
        private string _testConfigPath;
        
        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestConfigurationManager");
            _configManager = _testGameObject.AddComponent<ConfigurationManager>();
            _testConfigPath = Path.Combine(Application.temporaryCachePath, "test_config.yaml");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
        
        [Test]
        public void LoadConfiguration_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            string yamlContent = @"
corridors:
  generation_distance: 60.0
  cleanup_distance: 120.0

lighting:
  neon_response_distance: 6.0
  transition_duration: 2.5

rules:
  - name: TestRule
    priority: 1.0
    cooldown: 3.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 5.0
    actions:
      - action: GenerateLayout target: corridor intensity: 1.0
";
            File.WriteAllText(_testConfigPath, yamlContent);
            
            bool configLoaded = false;
            _configManager.ConfigurationLoaded += (config) => configLoaded = true;
            
            // Act
            _configManager.LoadConfiguration(_testConfigPath);
            
            // Assert
            Assert.IsTrue(configLoaded);
            Assert.IsNotNull(_configManager.CurrentConfiguration);
            Assert.AreEqual(60.0f, _configManager.CurrentConfiguration.CorridorGenerationDistance);
            Assert.AreEqual(6.0f, _configManager.CurrentConfiguration.NeonResponseDistance);
            Assert.AreEqual(1, _configManager.CurrentConfiguration.Rules.Count);
        }
        
        [Test]
        public void LoadConfiguration_NonExistentFile_UsesFallback()
        {
            // Arrange
            bool configLoaded = false;
            _configManager.ConfigurationLoaded += (config) => configLoaded = true;
            
            // Act
            _configManager.LoadConfiguration("nonexistent.yaml");
            
            // Assert
            Assert.IsTrue(configLoaded);
            Assert.IsNotNull(_configManager.CurrentConfiguration);
            Assert.IsTrue(_configManager.CurrentConfiguration.IsValid());
        }
        
        [Test]
        public void LoadConfiguration_InvalidFile_UsesFallback()
        {
            // Arrange
            File.WriteAllText(_testConfigPath, "invalid yaml content {[}");
            
            bool configLoaded = false;
            bool errorOccurred = false;
            _configManager.ConfigurationLoaded += (config) => configLoaded = true;
            _configManager.ConfigurationError += (error) => errorOccurred = true;
            
            // Act
            _configManager.LoadConfiguration(_testConfigPath);
            
            // Assert
            Assert.IsTrue(configLoaded);
            Assert.IsNotNull(_configManager.CurrentConfiguration);
            Assert.IsTrue(_configManager.CurrentConfiguration.IsValid());
        }
        
        [UnityTest]
        public IEnumerator ReloadConfiguration_ValidFile_ReloadsSuccessfully()
        {
            // Arrange
            string initialContent = @"
corridors:
  generation_distance: 50.0
";
            File.WriteAllText(_testConfigPath, initialContent);
            _configManager.LoadConfiguration(_testConfigPath);
            
            bool configReloaded = false;
            _configManager.ConfigurationReloaded += (config) => configReloaded = true;
            
            // Modify the file
            string modifiedContent = @"
corridors:
  generation_distance: 75.0
";
            File.WriteAllText(_testConfigPath, modifiedContent);
            
            // Act
            _configManager.ReloadConfiguration();
            
            // Wait for reload to complete
            yield return new WaitForSeconds(1.0f);
            
            // Assert
            Assert.IsTrue(configReloaded);
            Assert.AreEqual(75.0f, _configManager.CurrentConfiguration.CorridorGenerationDistance);
        }
        
        [UnityTest]
        public IEnumerator ReloadConfiguration_InvalidFile_KeepsCurrentConfiguration()
        {
            // Arrange
            string initialContent = @"
corridors:
  generation_distance: 50.0
";
            File.WriteAllText(_testConfigPath, initialContent);
            _configManager.LoadConfiguration(_testConfigPath);
            
            float initialDistance = _configManager.CurrentConfiguration.CorridorGenerationDistance;
            
            bool errorOccurred = false;
            _configManager.ConfigurationError += (error) => errorOccurred = true;
            
            // Modify the file with invalid content
            File.WriteAllText(_testConfigPath, "invalid yaml {[}");
            
            // Act
            _configManager.ReloadConfiguration();
            
            // Wait for reload attempt to complete
            yield return new WaitForSeconds(1.0f);
            
            // Assert
            Assert.IsTrue(errorOccurred);
            Assert.AreEqual(initialDistance, _configManager.CurrentConfiguration.CorridorGenerationDistance);
        }
        
        [Test]
        public void SetHotReloadEnabled_True_EnablesHotReload()
        {
            // Act
            _configManager.SetHotReloadEnabled(true);
            
            // Assert
            Assert.IsTrue(_configManager.IsHotReloadEnabled);
        }
        
        [Test]
        public void SetHotReloadEnabled_False_DisablesHotReload()
        {
            // Arrange
            _configManager.SetHotReloadEnabled(true);
            
            // Act
            _configManager.SetHotReloadEnabled(false);
            
            // Assert
            Assert.IsFalse(_configManager.IsHotReloadEnabled);
        }
        
        [Test]
        public void SetConfigurationFilePath_ValidPath_UpdatesPath()
        {
            // Arrange
            string newPath = "new/config/path.yaml";
            
            // Act
            _configManager.SetConfigurationFilePath(newPath);
            
            // Assert
            Assert.AreEqual(newPath, _configManager.ConfigurationFilePath);
        }
        
        [Test]
        public void RulesEngine_AfterConfigurationLoad_IsNotNull()
        {
            // Arrange
            string yamlContent = @"
rules:
  - name: TestRule
    priority: 1.0
    cooldown: 0.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 1.0
    actions:
      - action: GenerateLayout target: test intensity: 1.0
";
            File.WriteAllText(_testConfigPath, yamlContent);
            
            // Act
            _configManager.LoadConfiguration(_testConfigPath);
            
            // Assert
            Assert.IsNotNull(_configManager.RulesEngine);
            Assert.IsNotNull(_configManager.RulesEngine.Configuration);
        }
    }
}