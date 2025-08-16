using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class HotReloadIntegrationTests
    {
        private GameObject _testGameObject;
        private ConfigurationManager _configManager;
        private string _testConfigPath;
        
        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestHotReloadManager");
            _configManager = _testGameObject.AddComponent<ConfigurationManager>();
            _testConfigPath = Path.Combine(Application.temporaryCachePath, "hot_reload_test.yaml");
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
        
        [UnityTest]
        public IEnumerator HotReload_FileModification_TriggersReload()
        {
            // Arrange
            string initialConfig = @"
corridors:
  generation_distance: 40.0
  cleanup_distance: 80.0

rules:
  - name: InitialRule
    priority: 1.0
    cooldown: 2.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 3.0
    actions:
      - action: GenerateLayout target: initial intensity: 1.0
";
            
            File.WriteAllText(_testConfigPath, initialConfig);
            _configManager.LoadConfiguration(_testConfigPath);
            _configManager.SetHotReloadEnabled(true);
            
            bool reloadTriggered = false;
            _configManager.ConfigurationReloaded += (config) => reloadTriggered = true;
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Modify the configuration file
            string modifiedConfig = @"
corridors:
  generation_distance: 60.0
  cleanup_distance: 120.0

rules:
  - name: ModifiedRule
    priority: 2.0
    cooldown: 3.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 5.0
    actions:
      - action: GenerateLayout target: modified intensity: 1.5
";
            
            File.WriteAllText(_testConfigPath, modifiedConfig);
            
            // Wait for hot-reload to trigger
            yield return new WaitForSeconds(2.0f);
            
            // Assert
            Assert.IsTrue(reloadTriggered, "Hot-reload should have been triggered");
            Assert.AreEqual(60.0f, _configManager.CurrentConfiguration.CorridorGenerationDistance);
            Assert.AreEqual(120.0f, _configManager.CurrentConfiguration.CorridorCleanupDistance);
            
            var rule = _configManager.CurrentConfiguration.GetRuleByName("ModifiedRule");
            Assert.IsNotNull(rule, "Modified rule should be loaded");
            Assert.AreEqual(2.0f, rule.Priority);
        }
        
        [UnityTest]
        public IEnumerator HotReload_InvalidConfiguration_KeepsCurrentConfig()
        {
            // Arrange
            string validConfig = @"
corridors:
  generation_distance: 50.0

rules:
  - name: ValidRule
    priority: 1.0
    cooldown: 1.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 2.0
    actions:
      - action: GenerateLayout target: valid intensity: 1.0
";
            
            File.WriteAllText(_testConfigPath, validConfig);
            _configManager.LoadConfiguration(_testConfigPath);
            _configManager.SetHotReloadEnabled(true);
            
            float originalDistance = _configManager.CurrentConfiguration.CorridorGenerationDistance;
            string originalRuleName = _configManager.CurrentConfiguration.Rules[0].RuleName;
            
            bool errorOccurred = false;
            _configManager.ConfigurationError += (error) => errorOccurred = true;
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Write invalid configuration
            File.WriteAllText(_testConfigPath, "invalid: yaml: content: {[}");
            
            // Wait for hot-reload attempt
            yield return new WaitForSeconds(2.0f);
            
            // Assert
            Assert.IsTrue(errorOccurred, "Configuration error should have been triggered");
            Assert.AreEqual(originalDistance, _configManager.CurrentConfiguration.CorridorGenerationDistance);
            Assert.AreEqual(originalRuleName, _configManager.CurrentConfiguration.Rules[0].RuleName);
        }
        
        [UnityTest]
        public IEnumerator HotReload_DisabledHotReload_DoesNotReload()
        {
            // Arrange
            string initialConfig = @"
corridors:
  generation_distance: 30.0
";
            
            File.WriteAllText(_testConfigPath, initialConfig);
            _configManager.LoadConfiguration(_testConfigPath);
            _configManager.SetHotReloadEnabled(false);
            
            bool reloadTriggered = false;
            _configManager.ConfigurationReloaded += (config) => reloadTriggered = true;
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Modify the file
            string modifiedConfig = @"
corridors:
  generation_distance: 90.0
";
            File.WriteAllText(_testConfigPath, modifiedConfig);
            
            // Wait to see if reload triggers (it shouldn't)
            yield return new WaitForSeconds(2.0f);
            
            // Assert
            Assert.IsFalse(reloadTriggered, "Hot-reload should not have been triggered");
            Assert.AreEqual(30.0f, _configManager.CurrentConfiguration.CorridorGenerationDistance);
        }
        
        [UnityTest]
        public IEnumerator HotReload_RulesEngineIntegration_UpdatesRules()
        {
            // Arrange
            string initialConfig = @"
rules:
  - name: SlowRule
    priority: 1.0
    cooldown: 1.0
    conditions:
      - type: PlayerSpeed operator: LessThan value: 2.0
    actions:
      - action: ChangeFogDensity target: ambient intensity: 0.5
";
            
            File.WriteAllText(_testConfigPath, initialConfig);
            _configManager.LoadConfiguration(_testConfigPath);
            _configManager.SetHotReloadEnabled(true);
            
            // Test initial rule evaluation
            var context = _configManager.RulesEngine.CreateContext(Vector3.zero, 1.0f, 0.0f);
            var initialActions = _configManager.RulesEngine.EvaluateRules(context);
            
            Assert.AreEqual(1, initialActions.Count);
            Assert.AreEqual(GenerationAction.ActionType.ChangeFogDensity, initialActions[0].Type);
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Modify rules
            string modifiedConfig = @"
rules:
  - name: FastRule
    priority: 2.0
    cooldown: 2.0
    conditions:
      - type: PlayerSpeed operator: GreaterThan value: 8.0
    actions:
      - action: AdjustLighting target: neon intensity: 2.0
";
            
            File.WriteAllText(_testConfigPath, modifiedConfig);
            
            // Wait for hot-reload
            yield return new WaitForSeconds(2.0f);
            
            // Assert - Test new rule evaluation
            var fastContext = _configManager.RulesEngine.CreateContext(Vector3.zero, 10.0f, 0.0f);
            var newActions = _configManager.RulesEngine.EvaluateRules(fastContext);
            
            Assert.AreEqual(1, newActions.Count);
            Assert.AreEqual(GenerationAction.ActionType.AdjustLighting, newActions[0].Type);
            Assert.AreEqual("neon", newActions[0].Target);
            
            // Test that old rule no longer triggers
            var slowContext = _configManager.RulesEngine.CreateContext(Vector3.zero, 1.0f, 0.0f);
            var oldActions = _configManager.RulesEngine.EvaluateRules(slowContext);
            
            Assert.AreEqual(0, oldActions.Count, "Old rule should no longer be active");
        }
    }
}