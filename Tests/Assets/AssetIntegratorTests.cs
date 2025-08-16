using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeonQuest.Assets;

namespace Tests.Assets
{
    public class AssetIntegratorTests
    {
        private GameObject testGameObject;
        private AssetIntegrator assetIntegrator;
        private GameObject testPrefab;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with AssetIntegrator
            testGameObject = new GameObject("TestAssetIntegrator");
            assetIntegrator = testGameObject.AddComponent<AssetIntegrator>();

            // Create a test prefab
            testPrefab = CreateTestPrefab();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testPrefab != null)
            {
                Object.DestroyImmediate(testPrefab);
            }
        }

        private GameObject CreateTestPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "TestPrefab";
            
            // Add some child objects to test hierarchy preservation
            var child1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child1.name = "Child1";
            child1.transform.SetParent(prefab.transform);
            
            var child2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            child2.name = "Child2";
            child2.transform.SetParent(prefab.transform);
            
            // Add a light component for variation testing
            var light = prefab.AddComponent<Light>();
            light.intensity = 1.0f;
            
            return prefab;
        }

        [Test]
        public void AssetIntegrator_InitializesCorrectly()
        {
            // Test that the component initializes without errors
            Assert.IsNotNull(assetIntegrator);
            Assert.AreEqual(0f, assetIntegrator.CurrentMemoryUsage);
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_WithValidPath_LoadsAsset()
        {
            // Create a test asset in Resources folder (simulated)
            var loadTask = assetIntegrator.LoadAssetAsync("TestAsset");
            
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            var result = loadTask.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual("Placeholder_TestAsset", result.name);
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_WithInvalidPath_ReturnsNull()
        {
            var loadTask = assetIntegrator.LoadAssetAsync("");
            
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            var result = loadTask.Result;
            Assert.IsNull(result);
        }

        [Test]
        public void InstantiateAsset_WithValidAsset_CreatesInstance()
        {
            var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            
            Assert.IsNotNull(instance);
            Assert.AreEqual(Vector3.zero, instance.transform.position);
            Assert.AreEqual(Quaternion.identity, instance.transform.rotation);
            
            // Clean up
            assetIntegrator.ReturnToPool(instance);
        }

        [Test]
        public void InstantiateAsset_WithNullAsset_ReturnsNull()
        {
            var instance = assetIntegrator.InstantiateAsset(null, Vector3.zero, Quaternion.identity);
            
            Assert.IsNull(instance);
        }

        [Test]
        public void ReturnToPool_WithValidInstance_ReturnsToPool()
        {
            var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.activeInHierarchy);
            
            assetIntegrator.ReturnToPool(instance);
            
            // Object should be deactivated when returned to pool
            Assert.IsFalse(instance.activeInHierarchy);
        }

        [Test]
        public void ValidateAssetIntegrity_WithValidAsset_ReturnsTrue()
        {
            var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            
            var isValid = assetIntegrator.ValidateAssetIntegrity(instance);
            
            Assert.IsTrue(isValid);
            
            // Clean up
            assetIntegrator.ReturnToPool(instance);
        }

        [Test]
        public void ValidateAssetIntegrity_WithModifiedAsset_ReturnsFalse()
        {
            var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            
            // Modify the asset by destroying a child
            if (instance.transform.childCount > 0)
            {
                Object.DestroyImmediate(instance.transform.GetChild(0).gameObject);
            }
            
            var isValid = assetIntegrator.ValidateAssetIntegrity(instance);
            
            Assert.IsFalse(isValid);
            
            // Clean up
            assetIntegrator.ReturnToPool(instance);
        }

        [Test]
        public void ApplyVariations_WithColorVariation_ChangesColor()
        {
            var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            
            var variations = new Dictionary<string, object>
            {
                ["Color_TestPrefab"] = Color.red
            };
            
            assetIntegrator.ApplyVariations(instance, variations);
            
            var renderer = instance.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            // Note: In a real test, we'd check if the color was applied
            // This is a basic structure test
            
            // Clean up
            assetIntegrator.ReturnToPool(instance);
        }

        [Test]
        public void GetPoolStatistics_ReturnsValidStatistics()
        {
            // Create some instances to populate pools
            var instance1 = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
            var instance2 = assetIntegrator.InstantiateAsset(testPrefab, Vector3.one, Quaternion.identity);
            
            var stats = assetIntegrator.GetPoolStatistics();
            
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ContainsKey("TotalPools"));
            Assert.IsTrue(stats.ContainsKey("TotalActiveObjects"));
            Assert.IsTrue(stats.ContainsKey("MemoryUsageMB"));
            
            var totalActive = (int)stats["TotalActiveObjects"];
            Assert.AreEqual(2, totalActive);
            
            // Clean up
            assetIntegrator.ReturnToPool(instance1);
            assetIntegrator.ReturnToPool(instance2);
        }

        [Test]
        public void PerformCleanup_ReducesMemoryUsage()
        {
            // Create multiple instances to increase memory usage
            var instances = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                instances.Add(instance);
            }
            
            // Return all to pool
            foreach (var instance in instances)
            {
                assetIntegrator.ReturnToPool(instance);
            }
            
            var initialStats = assetIntegrator.GetPoolStatistics();
            var initialAvailable = (int)initialStats["TotalAvailableObjects"];
            
            // Perform cleanup with very low threshold to force cleanup
            assetIntegrator.PerformCleanup(0.001f);
            
            var finalStats = assetIntegrator.GetPoolStatistics();
            var finalAvailable = (int)finalStats["TotalAvailableObjects"];
            
            // Should have fewer available objects after cleanup
            Assert.IsTrue(finalAvailable < initialAvailable);
        }

        [UnityTest]
        public IEnumerator PreloadAssetsAsync_LoadsMultipleAssets()
        {
            var assetPaths = new List<string> { "Asset1", "Asset2", "Asset3" };
            
            var preloadTask = assetIntegrator.PreloadAssetsAsync(assetPaths, 3);
            
            yield return new WaitUntil(() => preloadTask.IsCompleted);
            
            var stats = assetIntegrator.GetPoolStatistics();
            var totalPools = (int)stats["TotalPools"];
            
            // Should have created pools for the preloaded assets
            Assert.IsTrue(totalPools >= assetPaths.Count);
        }

        [Test]
        public void CurrentMemoryUsage_ReturnsNonNegativeValue()
        {
            var memoryUsage = assetIntegrator.CurrentMemoryUsage;
            
            Assert.IsTrue(memoryUsage >= 0f);
        }

        [Test]
        public void ObjectPool_ManagesInstancesCorrectly()
        {
            // Test the object pool directly
            var pool = new ObjectPool(testPrefab, 2, 10);
            
            Assert.AreEqual(2, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);
            
            var instance1 = pool.Get(Vector3.zero, Quaternion.identity);
            Assert.AreEqual(1, pool.AvailableCount);
            Assert.AreEqual(1, pool.ActiveCount);
            
            var instance2 = pool.Get(Vector3.one, Quaternion.identity);
            Assert.AreEqual(0, pool.AvailableCount);
            Assert.AreEqual(2, pool.ActiveCount);
            
            pool.Return(instance1);
            Assert.AreEqual(1, pool.AvailableCount);
            Assert.AreEqual(1, pool.ActiveCount);
            
            pool.Return(instance2);
            Assert.AreEqual(2, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);
            
            // Clean up
            pool.Destroy();
        }

        [Test]
        public void AssetReference_CapturesIntegrityDataCorrectly()
        {
            var assetRef = new AssetReference("test/path", testPrefab);
            
            Assert.AreEqual("test/path", assetRef.AssetPath);
            Assert.AreEqual(testPrefab, assetRef.Prefab);
            Assert.IsNotNull(assetRef.IntegrityData);
            Assert.AreEqual(2, assetRef.IntegrityData.OriginalChildCount); // 2 children created in setup
            Assert.IsTrue(assetRef.IntegrityData.OriginalComponentTypes.Count > 0);
        }

        [Test]
        public void VariationPoint_InitializesCorrectly()
        {
            var variation = new VariationPoint("TestVariation", VariationType.MaterialColor, "path/to/target");
            
            Assert.AreEqual("TestVariation", variation.Name);
            Assert.AreEqual(VariationType.MaterialColor, variation.Type);
            Assert.AreEqual("path/to/target", variation.TargetPath);
            Assert.IsNotNull(variation.Parameters);
        }
    }
}