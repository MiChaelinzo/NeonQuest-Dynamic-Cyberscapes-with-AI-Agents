using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Core;
using NeonQuest.Core.SceneSetup;
using NeonQuest.Configuration;
using NeonQuest.Assets;

namespace Tests.Core
{
    /// <summary>
    /// End-to-end tests for complete NeonQuest system functionality
    /// Tests Requirements: 6.1, 6.3 - Scene setup and asset integration
    /// </summary>
    public class NeonQuestEndToEndTests
    {
        private GameObject testSceneRoot;
        private NeonQuestSceneManager sceneManager;
        private NeonQuestManager neonQuestManager;
        private GameObject[] testPrefabs;

        [SetUp]
        public void SetUp()
        {
            // Create test scene root
            testSceneRoot = new GameObject("EndToEndTestScene");
            
            // Create test prefabs
            CreateTestPrefabs();
            
            // Create scene manager
            var sceneManagerGO = new GameObject("NeonQuestSceneManager");
            sceneManagerGO.transform.SetParent(testSceneRoot.transform);
            sceneManager = sceneManagerGO.AddComponent<NeonQuestSceneManager>();
            
            // Configure scene manager with test prefabs
            ConfigureSceneManager();
        }

        [TearDown]
        public void TearDown()
        {
            if (testSceneRoot != null)
            {
                Object.DestroyImmediate(testSceneRoot);
            }
            
            // Clean up test prefabs
            if (testPrefabs != null)
            {
                foreach (var prefab in testPrefabs)
                {
                    if (prefab != null)
                    {
                        Object.DestroyImmediate(prefab);
                    }
                }
            }
        }

        private void CreateTestPrefabs()
        {
            var prefabList = new List<GameObject>();
            
            // Create test corridor prefab
            var corridor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridor.name = "TestCorridor";
            corridor.transform.localScale = new Vector3(2f, 3f, 10f);
            
            // Add mesh and material components to test preservation
            var corridorRenderer = corridor.GetComponent<Renderer>();
            var corridorMaterial = new Material(Shader.Find("Standard"));
            corridorMaterial.color = Color.gray;
            corridorRenderer.material = corridorMaterial;
            
            prefabList.Add(corridor);
            
            // Create test neon sign prefab
            var neonSign = GameObject.CreatePrimitive(PrimitiveType.Quad);
            neonSign.name = "TestNeonSign";
            neonSign.transform.localScale = new Vector3(2f, 1f, 1f);
            
            var neonRenderer = neonSign.GetComponent<Renderer>();
            var neonMaterial = new Material(Shader.Find("Standard"));
            neonMaterial.color = Color.cyan;
            neonMaterial.SetColor("_EmissionColor", Color.cyan);
            neonRenderer.material = neonMaterial;
            
            // Add light component
            var light = neonSign.AddComponent<Light>();
            light.color = Color.cyan;
            light.intensity = 1f;
            
            prefabList.Add(neonSign);
            
            testPrefabs = prefabList.ToArray();
        }

        private void ConfigureSceneManager()
        {
            // Use reflection to set private fields for testing
            var sceneManagerType = typeof(NeonQuestSceneManager);
            
            // Set test prefabs
            var prefabsField = sceneManagerType.GetField("neonUndergroundPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prefabsField?.SetValue(sceneManager, testPrefabs);
            
            // Set asset parent
            var assetParentField = sceneManagerType.GetField("assetParent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            assetParentField?.SetValue(sceneManager, testSceneRoot.transform);
            
            // Enable structure preservation
            var preserveField = sceneManagerType.GetField("preserveOriginalPrefabStructure", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            preserveField?.SetValue(sceneManager, true);
        }

        [UnityTest]
        public IEnumerator CompleteSystemIntegration_WithNeonUndergroundAssets_WorksEndToEnd()
        {
            // Act - Setup the complete scene
            sceneManager.SetupScene();
            
            // Wait for setup to complete
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!sceneManager.IsSceneSetupComplete && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            // Assert - Scene setup completed
            Assert.IsTrue(sceneManager.IsSceneSetupComplete, "Scene setup should complete within timeout");
            
            // Verify NeonQuestManager was created and initialized
            neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            Assert.IsNotNull(neonQuestManager, "NeonQuestManager should be created");
            Assert.IsTrue(neonQuestManager.IsInitialized, "NeonQuestManager should be initialized");
            
            // Verify prefab variants were created
            Assert.Greater(sceneManager.PrefabVariants.Count, 0, "Prefab variants should be created");
            Assert.AreEqual(testPrefabs.Length, sceneManager.PrefabVariants.Count, 
                "Should create variants for all test prefabs");
        }

        [UnityTest]
        public IEnumerator NeonUndergroundAssetIntegration_PreservesOriginalStructure()
        {
            // Arrange - Setup scene
            sceneManager.SetupScene();
            
            // Wait for setup
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Act - Get created variants
            var corridorVariant = sceneManager.GetPrefabVariant("TestCorridor");
            var neonVariant = sceneManager.GetPrefabVariant("TestNeonSign");
            
            // Assert - Original structure is preserved (Requirement 6.1)
            Assert.IsNotNull(corridorVariant, "Corridor variant should be created");
            Assert.IsNotNull(neonVariant, "Neon sign variant should be created");
            
            // Verify mesh preservation
            var originalCorridorMesh = testPrefabs[0].GetComponent<MeshFilter>().mesh;
            var variantCorridorMesh = corridorVariant.GetComponent<MeshFilter>().mesh;
            Assert.AreEqual(originalCorridorMesh, variantCorridorMesh, "Mesh should be preserved");
            
            // Verify material preservation
            var originalMaterial = testPrefabs[0].GetComponent<Renderer>().material;
            var variantMaterial = corridorVariant.GetComponent<Renderer>().material;
            Assert.AreEqual(originalMaterial.color, variantMaterial.color, "Material color should be preserved");
            
            // Verify component preservation (light on neon sign)
            var originalLight = testPrefabs[1].GetComponent<Light>();
            var variantLight = neonVariant.GetComponent<Light>();
            Assert.IsNotNull(variantLight, "Light component should be preserved");
            Assert.AreEqual(originalLight.color, variantLight.color, "Light color should be preserved");
        }

        [UnityTest]
        public IEnumerator AssetIntegrator_Integration_RegistersAssetsCorrectly()
        {
            // Arrange - Setup scene
            sceneManager.SetupScene();
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Act - Get asset integrator
            neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            var assetIntegrator = neonQuestManager.AssetIntegrator;
            
            // Assert - Asset integrator is available and functional
            Assert.IsNotNull(assetIntegrator, "Asset integrator should be available");
            
            // Verify assets are registered (this tests the integration)
            var instantiatedAssets = sceneManager.InstantiatedAssets;
            Assert.Greater(instantiatedAssets.Count, 0, "Assets should be instantiated");
            Assert.AreEqual(testPrefabs.Length, instantiatedAssets.Count, 
                "Should instantiate all test prefabs");
        }

        [UnityTest]
        public IEnumerator SceneBasedConfiguration_LoadsAndAppliesCorrectly()
        {
            // Arrange - Setup scene with configuration
            sceneManager.SetupScene();
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Act - Get configuration manager
            neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            var configManager = neonQuestManager.ConfigurationManager;
            
            // Assert - Configuration system is working
            Assert.IsNotNull(configManager, "Configuration manager should be available");
            
            // Test configuration update
            var testConfig = new Dictionary<string, object>
            {
                ["corridorGenerationDistance"] = 100.0f,
                ["neonResponseDistance"] = 8.0f
            };
            
            Assert.DoesNotThrow(() => neonQuestManager.UpdateSystemConfiguration(testConfig),
                "Configuration update should work without errors");
        }

        [UnityTest]
        public IEnumerator SystemCoordination_WithCompleteSetup_WorksCorrectly()
        {
            // Arrange - Setup complete scene
            sceneManager.SetupScene();
            yield return new WaitUntil(() => sceneManager.IsSceneSetupComplete);
            
            // Act - Let systems run for a brief period
            yield return new WaitForSeconds(0.5f);
            
            // Assert - All systems are coordinated and working
            neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            Assert.IsTrue(neonQuestManager.AllSystemsReady, "All systems should be ready and coordinated");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["allSystemsReady"], "Health status should indicate all systems ready");
            Assert.IsTrue((bool)healthStatus["updateLoopActive"], "Update loop should be active");
            
            // Verify no system errors
            Assert.IsFalse(healthStatus.ContainsKey("systemErrors") && 
                ((Dictionary<string, string>)healthStatus["systemErrors"]).Count > 0,
                "Should not have system errors in end-to-end test");
        }

        [Test]
        public void PrefabVariantCreation_WithMultipleAssets_CreatesAllVariants()
        {
            // Act
            sceneManager.SetupScene();
            
            // Wait for setup (synchronous test)
            var timeout = 100; // iterations
            var iterations = 0;
            while (!sceneManager.IsSceneSetupComplete && iterations < timeout)
            {
                iterations++;
                System.Threading.Thread.Sleep(10);
            }
            
            // Assert
            Assert.IsTrue(sceneManager.IsSceneSetupComplete, "Scene setup should complete");
            Assert.AreEqual(testPrefabs.Length, sceneManager.PrefabVariants.Count,
                "Should create variants for all prefabs");
            
            // Verify each variant exists
            foreach (var prefab in testPrefabs)
            {
                var variant = sceneManager.GetPrefabVariant(prefab.name);
                Assert.IsNotNull(variant, $"Variant should exist for {prefab.name}");
                Assert.AreNotEqual(prefab, variant, "Variant should be different from original");
            }
        }

        [Test]
        public void SceneManager_WithMissingComponents_CreatesDefaults()
        {
            // Arrange - Remove any existing NeonQuestManager
            var existingManager = Object.FindObjectOfType<NeonQuestManager>();
            if (existingManager != null)
            {
                Object.DestroyImmediate(existingManager.gameObject);
            }
            
            // Act
            sceneManager.SetupScene();
            
            // Wait for setup
            var timeout = 100;
            var iterations = 0;
            while (!sceneManager.IsSceneSetupComplete && iterations < timeout)
            {
                iterations++;
                System.Threading.Thread.Sleep(10);
            }
            
            // Assert - Default systems should be created
            neonQuestManager = Object.FindObjectOfType<NeonQuestManager>();
            Assert.IsNotNull(neonQuestManager, "Default NeonQuestManager should be created");
            Assert.IsTrue(neonQuestManager.IsInitialized, "Default manager should be initialized");
        }
    }
}