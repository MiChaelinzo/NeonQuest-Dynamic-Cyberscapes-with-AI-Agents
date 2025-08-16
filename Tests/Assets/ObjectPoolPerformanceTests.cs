using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NeonQuest.Assets;

namespace Tests.Assets
{
    /// <summary>
    /// Performance tests for ObjectPool effectiveness and memory management
    /// </summary>
    public class ObjectPoolPerformanceTests
    {
        private GameObject testPrefab;
        private ObjectPool objectPool;
        private const int PERFORMANCE_TEST_ITERATIONS = 1000;
        private const int MEMORY_TEST_OBJECTS = 100;

        [SetUp]
        public void SetUp()
        {
            testPrefab = CreatePerformanceTestPrefab();
            objectPool = new ObjectPool(testPrefab, 10, 200);
        }

        [TearDown]
        public void TearDown()
        {
            objectPool?.Destroy();
            if (testPrefab != null)
            {
                Object.DestroyImmediate(testPrefab);
            }
        }

        private GameObject CreatePerformanceTestPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "PerformanceTestPrefab";
            
            // Add components to make it more realistic
            prefab.AddComponent<Rigidbody>();
            prefab.AddComponent<MeshRenderer>();
            prefab.AddComponent<MeshFilter>();
            
            // Add child objects to simulate complex prefabs
            for (int i = 0; i < 3; i++)
            {
                var child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                child.name = $"Child_{i}";
                child.transform.SetParent(prefab.transform);
            }
            
            return prefab;
        }

        [Test]
        public void ObjectPool_GetPerformance_IsFasterThanInstantiate()
        {
            // Warm up the pool
            var warmupObjects = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                warmupObjects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
            }
            foreach (var obj in warmupObjects)
            {
                objectPool.Return(obj);
            }

            // Test pool performance
            var stopwatch = Stopwatch.StartNew();
            var pooledObjects = new List<GameObject>();
            
            for (int i = 0; i < PERFORMANCE_TEST_ITERATIONS; i++)
            {
                var obj = objectPool.Get(Vector3.zero, Quaternion.identity);
                pooledObjects.Add(obj);
            }
            
            stopwatch.Stop();
            long poolTime = stopwatch.ElapsedMilliseconds;
            
            // Return objects to pool
            foreach (var obj in pooledObjects)
            {
                objectPool.Return(obj);
            }

            // Test direct instantiation performance
            stopwatch.Restart();
            var instantiatedObjects = new List<GameObject>();
            
            for (int i = 0; i < PERFORMANCE_TEST_ITERATIONS; i++)
            {
                var obj = Object.Instantiate(testPrefab);
                instantiatedObjects.Add(obj);
            }
            
            stopwatch.Stop();
            long instantiateTime = stopwatch.ElapsedMilliseconds;
            
            // Cleanup instantiated objects
            foreach (var obj in instantiatedObjects)
            {
                Object.DestroyImmediate(obj);
            }

            UnityEngine.Debug.Log($"Pool time: {poolTime}ms, Instantiate time: {instantiateTime}ms");
            
            // Pool should be significantly faster (at least 2x faster)
            Assert.IsTrue(poolTime < instantiateTime, 
                $"Pool performance ({poolTime}ms) should be better than instantiation ({instantiateTime}ms)");
        }

        [Test]
        public void ObjectPool_MemoryUsage_IsTrackedAccurately()
        {
            long initialMemory = objectPool.GetMemoryUsage();
            Assert.AreEqual(0, initialMemory); // Empty pool should have no memory usage for instances

            // Create objects and check memory tracking
            var objects = new List<GameObject>();
            for (int i = 0; i < MEMORY_TEST_OBJECTS; i++)
            {
                objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
            }

            long memoryWithObjects = objectPool.GetMemoryUsage();
            Assert.IsTrue(memoryWithObjects > initialMemory, 
                "Memory usage should increase with active objects");

            // Return objects and verify memory is still tracked
            foreach (var obj in objects)
            {
                objectPool.Return(obj);
            }

            long memoryAfterReturn = objectPool.GetMemoryUsage();
            Assert.AreEqual(memoryWithObjects, memoryAfterReturn, 
                "Memory usage should remain the same after returning to pool");
        }

        [Test]
        public void ObjectPool_AutomaticCleanup_ReducesMemoryUsage()
        {
            // Fill the pool beyond initial size
            var objects = new List<GameObject>();
            for (int i = 0; i < 50; i++)
            {
                objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
            }

            // Return all objects to create large available pool
            foreach (var obj in objects)
            {
                objectPool.Return(obj);
            }

            int availableBeforeCleanup = objectPool.AvailableCount;
            Assert.IsTrue(availableBeforeCleanup > 10, "Should have many available objects");

            // Perform cleanup
            int cleanedUp = objectPool.Cleanup(5);
            
            int availableAfterCleanup = objectPool.AvailableCount;
            
            Assert.IsTrue(cleanedUp > 0, "Should have cleaned up some objects");
            Assert.IsTrue(availableAfterCleanup < availableBeforeCleanup, 
                "Available count should be reduced after cleanup");
            Assert.AreEqual(5, availableAfterCleanup, 
                "Should have exactly 5 objects remaining after cleanup");
        }

        [Test]
        public void ObjectPool_MemoryThresholdCleanup_WorksCorrectly()
        {
            // Create many objects to increase memory usage
            var objects = new List<GameObject>();
            for (int i = 0; i < MEMORY_TEST_OBJECTS; i++)
            {
                objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
            }

            // Return all to pool
            foreach (var obj in objects)
            {
                objectPool.Return(obj);
            }

            long memoryBefore = objectPool.GetMemoryUsage();
            int availableBefore = objectPool.AvailableCount;

            // Cleanup with aggressive threshold
            int cleaned = objectPool.Cleanup(10);

            long memoryAfter = objectPool.GetMemoryUsage();
            int availableAfter = objectPool.AvailableCount;

            Assert.IsTrue(cleaned > 0, "Should have cleaned up objects");
            Assert.IsTrue(memoryAfter < memoryBefore, "Memory usage should be reduced");
            Assert.IsTrue(availableAfter < availableBefore, "Available objects should be reduced");
        }

        [Test]
        public void ObjectPool_ConcurrentAccess_HandlesMultipleRequests()
        {
            var objects = new List<GameObject>();
            
            // Simulate concurrent access by rapidly getting and returning objects
            for (int iteration = 0; iteration < 10; iteration++)
            {
                // Get multiple objects
                for (int i = 0; i < 20; i++)
                {
                    objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
                }
                
                // Return half of them
                for (int i = 0; i < 10; i++)
                {
                    objectPool.Return(objects[i]);
                    objects.RemoveAt(i);
                }
            }

            // Verify pool is still in valid state
            Assert.IsTrue(objectPool.ActiveCount > 0, "Should have active objects");
            Assert.IsTrue(objectPool.TotalCount > 0, "Should have total objects");

            // Return remaining objects
            foreach (var obj in objects)
            {
                objectPool.Return(obj);
            }

            Assert.AreEqual(0, objectPool.ActiveCount, "All objects should be returned");
        }

        [Test]
        public void ObjectPool_LargeScale_MaintainsPerformance()
        {
            const int LARGE_SCALE_TEST = 5000;
            var stopwatch = Stopwatch.StartNew();
            var objects = new List<GameObject>();

            // Test large scale object creation
            for (int i = 0; i < LARGE_SCALE_TEST; i++)
            {
                objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
            }

            stopwatch.Stop();
            long creationTime = stopwatch.ElapsedMilliseconds;

            // Test large scale object return
            stopwatch.Restart();
            foreach (var obj in objects)
            {
                objectPool.Return(obj);
            }
            stopwatch.Stop();
            long returnTime = stopwatch.ElapsedMilliseconds;

            UnityEngine.Debug.Log($"Large scale test - Creation: {creationTime}ms, Return: {returnTime}ms");

            // Performance should be reasonable (less than 1 second for 5000 operations)
            Assert.IsTrue(creationTime < 1000, 
                $"Creation time ({creationTime}ms) should be under 1 second for {LARGE_SCALE_TEST} objects");
            Assert.IsTrue(returnTime < 1000, 
                $"Return time ({returnTime}ms) should be under 1 second for {LARGE_SCALE_TEST} objects");
        }

        [Test]
        public void ObjectPool_PooledObjectComponent_TracksPoolCorrectly()
        {
            var obj = objectPool.Get(Vector3.zero, Quaternion.identity);
            var pooledComponent = obj.GetComponent<PooledObject>();
            
            Assert.IsNotNull(pooledComponent, "Pooled objects should have PooledObject component");
            Assert.AreEqual(objectPool, pooledComponent.ParentPool, "Should reference correct pool");

            // Test convenience return method
            pooledComponent.ReturnToPool();
            
            Assert.IsFalse(obj.activeInHierarchy, "Object should be deactivated when returned");
            Assert.AreEqual(0, objectPool.ActiveCount, "Pool should show no active objects");
        }

        [Test]
        public void ObjectPool_MaxPoolSize_IsRespected()
        {
            var smallPool = new ObjectPool(testPrefab, 2, 5); // Max size of 5
            var objects = new List<GameObject>();

            // Try to create more objects than max pool size
            for (int i = 0; i < 10; i++)
            {
                objects.Add(smallPool.Get(Vector3.zero, Quaternion.identity));
            }

            // Return all objects
            foreach (var obj in objects)
            {
                smallPool.Return(obj);
            }

            // Pool should not exceed max size
            Assert.IsTrue(smallPool.TotalCount <= 5, 
                $"Pool size ({smallPool.TotalCount}) should not exceed max size (5)");

            smallPool.Destroy();
        }

        [UnityTest]
        public IEnumerator ObjectPool_FrameRateImpact_IsMinimal()
        {
            float initialFrameRate = 1f / Time.deltaTime;
            
            // Perform intensive pooling operations over multiple frames
            for (int frame = 0; frame < 10; frame++)
            {
                var objects = new List<GameObject>();
                
                // Create many objects in one frame
                for (int i = 0; i < 100; i++)
                {
                    objects.Add(objectPool.Get(Vector3.zero, Quaternion.identity));
                }
                
                // Return them all
                foreach (var obj in objects)
                {
                    objectPool.Return(obj);
                }
                
                yield return null; // Wait one frame
            }

            float finalFrameRate = 1f / Time.deltaTime;
            
            // Frame rate should not drop significantly (allow 20% drop)
            float frameRateRatio = finalFrameRate / initialFrameRate;
            Assert.IsTrue(frameRateRatio > 0.8f, 
                $"Frame rate impact too high. Initial: {initialFrameRate:F1}, Final: {finalFrameRate:F1}");
        }
    }
}