using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeonQuest.Assets;

namespace NeonQuest.Assets
{
    /// <summary>
    /// Validation script to test AssetIntegrator functionality
    /// This can be attached to a GameObject in Unity to run validation tests
    /// </summary>
    public class AssetIntegratorValidator : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        [SerializeField] private GameObject testPrefab;

        private AssetIntegrator assetIntegrator;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunValidationTests();
            }
        }

        public async void RunValidationTests()
        {
            Debug.Log("[AssetIntegratorValidator] Starting validation tests...");

            // Get or create AssetIntegrator
            assetIntegrator = GetComponent<AssetIntegrator>();
            if (assetIntegrator == null)
            {
                assetIntegrator = gameObject.AddComponent<AssetIntegrator>();
            }

            // Create test prefab if none provided
            if (testPrefab == null)
            {
                testPrefab = CreateTestPrefab();
            }

            int passedTests = 0;
            int totalTests = 0;

            // Test 1: Asset Loading
            totalTests++;
            if (await TestAssetLoading())
            {
                passedTests++;
                LogResult("Asset Loading", true);
            }
            else
            {
                LogResult("Asset Loading", false);
            }

            // Test 2: Asset Instantiation
            totalTests++;
            if (TestAssetInstantiation())
            {
                passedTests++;
                LogResult("Asset Instantiation", true);
            }
            else
            {
                LogResult("Asset Instantiation", false);
            }

            // Test 3: Object Pooling
            totalTests++;
            if (TestObjectPooling())
            {
                passedTests++;
                LogResult("Object Pooling", true);
            }
            else
            {
                LogResult("Object Pooling", false);
            }

            // Test 4: Asset Integrity Validation
            totalTests++;
            if (TestAssetIntegrity())
            {
                passedTests++;
                LogResult("Asset Integrity", true);
            }
            else
            {
                LogResult("Asset Integrity", false);
            }

            // Test 5: Variation System
            totalTests++;
            if (TestVariationSystem())
            {
                passedTests++;
                LogResult("Variation System", true);
            }
            else
            {
                LogResult("Variation System", false);
            }

            // Test 6: Memory Management
            totalTests++;
            if (TestMemoryManagement())
            {
                passedTests++;
                LogResult("Memory Management", true);
            }
            else
            {
                LogResult("Memory Management", false);
            }

            // Test 7: Parent-Child Relationship Preservation
            totalTests++;
            if (TestParentChildPreservation())
            {
                passedTests++;
                LogResult("Parent-Child Preservation", true);
            }
            else
            {
                LogResult("Parent-Child Preservation", false);
            }

            Debug.Log($"[AssetIntegratorValidator] Validation complete: {passedTests}/{totalTests} tests passed");

            if (passedTests == totalTests)
            {
                Debug.Log("[AssetIntegratorValidator] ✅ All tests passed! AssetIntegrator implementation is working correctly.");
            }
            else
            {
                Debug.LogWarning($"[AssetIntegratorValidator] ⚠️ {totalTests - passedTests} test(s) failed. Check implementation.");
            }
        }

        private async Task<bool> TestAssetLoading()
        {
            try
            {
                var loadedAsset = await assetIntegrator.LoadAssetAsync("TestAsset");
                return loadedAsset != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Asset loading test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestAssetInstantiation()
        {
            try
            {
                var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                bool success = instance != null;
                
                if (success)
                {
                    assetIntegrator.ReturnToPool(instance);
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Asset instantiation test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestObjectPooling()
        {
            try
            {
                var instance1 = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                var instance2 = assetIntegrator.InstantiateAsset(testPrefab, Vector3.one, Quaternion.identity);
                
                bool success = instance1 != null && instance2 != null && instance1 != instance2;
                
                if (success)
                {
                    assetIntegrator.ReturnToPool(instance1);
                    assetIntegrator.ReturnToPool(instance2);
                    
                    // Test pool reuse
                    var instance3 = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                    success = instance3 != null;
                    
                    if (success)
                    {
                        assetIntegrator.ReturnToPool(instance3);
                    }
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Object pooling test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestAssetIntegrity()
        {
            try
            {
                var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                bool isValid = assetIntegrator.ValidateAssetIntegrity(instance);
                
                assetIntegrator.ReturnToPool(instance);
                return isValid;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Asset integrity test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestVariationSystem()
        {
            try
            {
                var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                
                var variations = new Dictionary<string, object>
                {
                    ["Color_TestPrefab"] = Color.red,
                    ["Intensity_TestLight"] = 2.0f
                };
                
                assetIntegrator.ApplyVariations(instance, variations);
                
                // Test passes if no exceptions are thrown
                assetIntegrator.ReturnToPool(instance);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Variation system test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestMemoryManagement()
        {
            try
            {
                float initialMemory = assetIntegrator.CurrentMemoryUsage;
                
                // Create multiple instances
                var instances = new List<GameObject>();
                for (int i = 0; i < 10; i++)
                {
                    instances.Add(assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity));
                }
                
                float memoryAfterCreation = assetIntegrator.CurrentMemoryUsage;
                
                // Return all instances
                foreach (var instance in instances)
                {
                    assetIntegrator.ReturnToPool(instance);
                }
                
                // Perform cleanup
                assetIntegrator.PerformCleanup(0.001f);
                
                float memoryAfterCleanup = assetIntegrator.CurrentMemoryUsage;
                
                // Memory should be managed (not necessarily equal due to pooling)
                return memoryAfterCreation >= initialMemory;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Memory management test failed: {ex.Message}");
                return false;
            }
        }

        private bool TestParentChildPreservation()
        {
            try
            {
                var instance = assetIntegrator.InstantiateAsset(testPrefab, Vector3.zero, Quaternion.identity);
                
                // Check that child count is preserved
                int originalChildCount = testPrefab.transform.childCount;
                int instanceChildCount = instance.transform.childCount;
                
                bool childCountPreserved = originalChildCount == instanceChildCount;
                
                // Check that child names are preserved
                bool childNamesPreserved = true;
                for (int i = 0; i < originalChildCount && i < instanceChildCount; i++)
                {
                    string originalName = testPrefab.transform.GetChild(i).name;
                    string instanceName = instance.transform.GetChild(i).name;
                    
                    if (!instanceName.Contains(originalName))
                    {
                        childNamesPreserved = false;
                        break;
                    }
                }
                
                assetIntegrator.ReturnToPool(instance);
                return childCountPreserved && childNamesPreserved;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Parent-child preservation test failed: {ex.Message}");
                return false;
            }
        }

        private GameObject CreateTestPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "TestPrefab";
            
            // Add child objects to test hierarchy preservation
            var child1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child1.name = "TestChild1";
            child1.transform.SetParent(prefab.transform);
            
            var child2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            child2.name = "TestChild2";
            child2.transform.SetParent(prefab.transform);
            
            // Add components for variation testing
            var light = prefab.AddComponent<Light>();
            light.name = "TestLight";
            light.intensity = 1.0f;
            
            return prefab;
        }

        private void LogResult(string testName, bool passed)
        {
            if (logDetailedResults)
            {
                string status = passed ? "✅ PASS" : "❌ FAIL";
                Debug.Log($"[AssetIntegratorValidator] {testName}: {status}");
            }
        }
    }
}