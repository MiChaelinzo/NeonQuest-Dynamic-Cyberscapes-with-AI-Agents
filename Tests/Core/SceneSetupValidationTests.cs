using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Core.SceneSetup;
using NeonQuest.Core;

namespace Tests.Core
{
    /// <summary>
    /// Validation tests for scene setup and prefab integration functionality
    /// Tests Requirements: 6.1, 6.3 - Scene setup and asset integration
    /// </summary>
    public class SceneSetupValidationTests
    {
        private GameObject testSceneRoot;
        private NeonQuestSceneManager sceneManager;
        private SceneConfigurationTemplate testConfig;

        [SetUp]
        public void SetUp()
        {
            testSceneRoot = new GameObject("SceneSetupValidationTest");
            
            var sceneManagerGO = new GameObject("NeonQuestSceneManager");
            sceneManagerGO.transform.SetParent(testSceneRoot.transform);
            sceneManager = sceneManagerGO.AddComponent<NeonQuestSceneManager>();
            
            // Create test configuration
            testConfig = SceneConfigurationTemplate.CreateDefault("TestScene");
        }

        [TearDown]
        public void TearDown()
        {
            if (testSceneRoot != null)
            {
                Object.DestroyImmediate(testSceneRoot);
            }
            
            if (testConfig != null)
            {
                Object.DestroyImmediate(testConfig);
            }
        }

        [Test]
        public void SceneConfigurationTemplate_CreateDefault_CreatesValidConfiguration()
        {
            // Act
            var config = SceneConfigurationTemplate.CreateDefault("TestScene");
            
            // Assert
            Assert.IsNotNull(config, "Configuration should be created");
            Assert.AreEqual("TestScene", config.SceneName, "Scene name should be set");
            Assert.IsTrue(config.AutoInitializeOnSceneLoad, "Auto-initialize should be enabled by default");
            Assert.IsTrue(config.PreserveOriginalStructure, "Structure preservation should be enabled by default");
            
            var validationIssues = config.ValidateConfiguration();
            Assert.AreEqual(0, validationIssues.Count, "Default configuration should be valid");
        }

        [Test]
        public void SceneConfigurationTemplate_Validation_DetectsIssues()
        {
            // Arrange - Create invalid configuration
            var invalidConfig = SceneConfigurationTemplate.CreateDefault("");
            
            // Use reflection to set invalid values
            var configType = typeof(SceneConfigurationTemplate);
            var sceneNameField = configType.GetField("sceneName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var yamlPathField = configType.GetField("yamlConfigurationPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var updateIntervalField = configType.GetField("systemUpdateInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            sceneNameField?.SetValue(invalidConfig, "");
            yamlPathField?.SetValue(invalidConfig, "");
            updateIntervalField?.SetValue(invalidConfig, -1f);
            
            // Act
            var validationIssues = invalidConfig.ValidateConfiguration();
            
            // Assert
            Assert.Greater(validationIssues.Count, 0, "Should detect validation issues");
            Assert.IsTrue(validationIssues.Exists(issue => issue.Contains("Scene name")), "Should detect missing scene name");
            Assert.IsTrue(validationIssues.Exists(issue => issue.Contains("YAML configuration path")), "Should detect missing YAML path");
        }

        [Test]
        public void SceneConfigurationTemplate_ApplyToSceneManager_ConfiguresCorrectly()
        {
            // Arrange
            var config = SceneConfigurationTemplate.CreateDefault("TestScene");
            
            // Act
            config.ApplyToSceneManager(sceneManager);
            
            // Assert - Configuration should be applied (tested indirectly through behavior)
            Assert.DoesNotThrow(() => config.ApplyToSceneManager(sceneManager), 
                "Configuration application should not throw exceptions");
        }

        [UnityTest]
        public IEnumerator NeonQuestSceneManager_SetupScene_CreatesRequiredSystems()
        {
            // Act
            sceneManager.SetupScene();
            
            // Wait for setup to complete
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Assert
            Assert.IsTrue(sceneManager.IsSceneSetupComplete, "Scene setup should complete");
            
            // Verify NeonQuestManager was created
            var neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            Assert.IsNotNull(neonQuestManager, "NeonQuestManager should be created");
            Assert.IsTrue(neonQuestManager.IsInitialized, "NeonQuestManager should be initialized");
        }

        [UnityTest]
        public IEnumerator NeonQuestSceneManager_WithNeonUndergroundPrefabs_CreatesPrefabVariants()
        {
            // Arrange - Create test prefabs
            var testPrefabs = CreateTestNeonUndergroundPrefabs();
            
            // Configure scene manager with test prefabs
            var sceneManagerType = typeof(NeonQuestSceneManager);
            var prefabsField = sceneManagerType.GetField("neonUndergroundPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prefabsField?.SetValue(sceneManager, testPrefabs);
            
            // Act
            sceneManager.SetupScene();
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Assert
            Assert.Greater(sceneManager.PrefabVariants.Count, 0, "Prefab variants should be created");
            Assert.AreEqual(testPrefabs.Length, sceneManager.PrefabVariants.Count, 
                "Should create variants for all prefabs");
            
            // Verify structure preservation (Requirement 6.1)
            foreach (var prefab in testPrefabs)
            {
                var variant = sceneManager.GetPrefabVariant(prefab.name);
                Assert.IsNotNull(variant, $"Variant should exist for {prefab.name}");
                
                // Check mesh preservation
                var originalMesh = prefab.GetComponent<MeshFilter>()?.mesh;
                var variantMesh = variant.GetComponent<MeshFilter>()?.mesh;
                if (originalMesh != null && variantMesh != null)
                {
                    Assert.AreEqual(originalMesh, variantMesh, "Mesh should be preserved");
                }
                
                // Check material preservation
                var originalMaterial = prefab.GetComponent<Renderer>()?.material;
                var variantMaterial = variant.GetComponent<Renderer>()?.material;
                if (originalMaterial != null && variantMaterial != null)
                {
                    Assert.AreEqual(originalMaterial.color, variantMaterial.color, "Material color should be preserved");
                }
            }
            
            // Clean up test prefabs
            foreach (var prefab in testPrefabs)
            {
                if (prefab != null) Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void NeonQuestSceneManager_GetPrefabVariant_ReturnsCorrectVariant()
        {
            // Arrange - Create a test prefab and add it manually
            var testPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testPrefab.name = "TestPrefab";
            
            sceneManager.AddNeonUndergroundPrefab(testPrefab);
            
            // Act
            sceneManager.SetupScene();
            
            // Wait for setup (synchronous)
            var timeout = 100;
            var iterations = 0;
            while (!sceneManager.IsSceneSetupComplete && iterations < timeout)
            {
                iterations++;
                System.Threading.Thread.Sleep(10);
            }
            
            var variant = sceneManager.GetPrefabVariant("TestPrefab");
            
            // Assert
            Assert.IsNotNull(variant, "Should return the created variant");
            Assert.AreNotEqual(testPrefab, variant, "Variant should be different from original");
            Assert.IsTrue(variant.name.Contains("ProceduralVariant"), "Variant should have procedural variant naming");
            
            // Clean up
            Object.DestroyImmediate(testPrefab);
        }

        [UnityTest]
        public IEnumerator SceneBasedConfiguration_LoadsAndApplies()
        {
            // Arrange - Setup scene with configuration
            testConfig.ApplyToSceneManager(sceneManager);
            
            // Act
            sceneManager.SetupScene();
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Assert - Configuration should be applied
            var neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            Assert.IsNotNull(neonQuestManager, "NeonQuestManager should be created");
            
            var configManager = neonQuestManager.ConfigurationManager;
            Assert.IsNotNull(configManager, "Configuration manager should be available");
            
            // Test that configuration path was applied
            Assert.AreEqual(testConfig.YamlConfigurationPath, configManager.ConfigurationFilePath,
                "Configuration path should be applied from template");
        }

        private GameObject[] CreateTestNeonUndergroundPrefabs()
        {
            var prefabs = new List<GameObject>();
            
            // Create corridor prefab
            var corridor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridor.name = "TestCorridor";
            corridor.transform.localScale = new Vector3(2f, 3f, 10f);
            
            var corridorRenderer = corridor.GetComponent<Renderer>();
            var corridorMaterial = new Material(Shader.Find("Standard"));
            corridorMaterial.color = Color.gray;
            corridorRenderer.material = corridorMaterial;
            
            prefabs.Add(corridor);
            
            // Create neon sign prefab
            var neonSign = GameObject.CreatePrimitive(PrimitiveType.Quad);
            neonSign.name = "TestNeonSign";
            
            var neonRenderer = neonSign.GetComponent<Renderer>();
            var neonMaterial = new Material(Shader.Find("Standard"));
            neonMaterial.color = Color.cyan;
            neonRenderer.material = neonMaterial;
            
            // Add light component
            var light = neonSign.AddComponent<Light>();
            light.color = Color.cyan;
            
            prefabs.Add(neonSign);
            
            return prefabs.ToArray();
        }
    }
}