using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class LightingEnginePerformanceTests
    {
        private GameObject testScene;
        private LightingEngine lightingEngine;
        private List<GameObject> testLights;
        private EnvironmentConfiguration testConfig;

        [SetUp]
        public void SetUp()
        {
            testScene = new GameObject("PerformanceTestScene");
            
            var lightingEngineObject = new GameObject("LightingEngine");
            lightingEngine = lightingEngineObject.AddComponent<LightingEngine>();
            lightingEngineObject.transform.SetParent(testScene.transform);
            
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.NeonResponseDistance = 10f;
            testConfig.BrightnessMultiplierRange = new Vector2(0.5f, 2.5f);
            testConfig.LightingTransitionDuration = 1f;
            
            testLights = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testScene != null)
                Object.DestroyImmediate(testScene);
            if (testConfig != null)
                Object.DestroyImmediate(testConfig);
            
            foreach (var light in testLights)
            {
                if (light != null)
                    Object.DestroyImmediate(light);
            }
            testLights.Clear();
        }

        private void CreateLightArray(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var lightObject = new GameObject($"PerfTestLight_{i}");
                lightObject.transform.position = Random.insideUnitSphere * 100f;
                lightObject.transform.SetParent(testScene.transform);
                
                var light = lightObject.AddComponent<Light>();
                light.intensity = Random.Range(0.8f, 2.0f);
                light.color = Color.HSVToRGB(Random.Range(0.5f, 0.8f), 0.9f, 1f);
                light.range = Random.Range(8f, 20f);
                
                lightObject.tag = "NeonLight";
                testLights.Add(lightObject);
            }
        }

        [Test]
        [Performance]
        public void Performance_LightRegistration_ScalesLinearlyWithCount()
        {
            // Arrange
            var testCounts = new int[] { 10, 25, 50, 100 };
            var registrationTimes = new List<float>();

            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            foreach (int count in testCounts)
            {
                // Create lights for this test
                CreateLightArray(count);

                // Measure registration time
                var stopwatch = Stopwatch.StartNew();
                
                foreach (var lightObj in testLights)
                {
                    lightingEngine.GenerateAsync(new Dictionary<string, object> 
                    { 
                        { "lightObject", lightObj } 
                    }).Wait();
                }
                
                stopwatch.Stop();
                registrationTimes.Add(stopwatch.ElapsedMilliseconds);

                // Cleanup for next iteration
                foreach (var light in testLights)
                {
                    Object.DestroyImmediate(light);
                }
                testLights.Clear();
            }

            // Assert linear scaling (each doubling should take roughly double the time)
            for (int i = 1; i < registrationTimes.Count; i++)
            {
                float ratio = registrationTimes[i] / registrationTimes[i - 1];
                float expectedRatio = (float)testCounts[i] / testCounts[i - 1];
                
                // Allow 50% variance for linear scaling
                Assert.Less(ratio, expectedRatio * 1.5f, 
                    $"Registration time scaling too high: {ratio} vs expected {expectedRatio}");
            }
        }

        [UnityTest]
        [Performance]
        public IEnumerator Performance_UpdateCycle_MaintainsTargetFramerate()
        {
            // Arrange - Create moderate number of lights
            CreateLightArray(30);
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            var frameTimes = new List<float>();
            int frameCount = 0;
            float testDuration = 3f;
            float startTime = Time.time;

            // Act - Run update cycles while measuring performance
            while (Time.time - startTime < testDuration)
            {
                float frameStart = Time.realtimeSinceStartup;
                
                // Simulate active lighting scenario
                Vector3 playerPos = new Vector3(
                    Mathf.Sin(Time.time * 2f) * 20f,
                    0f,
                    Mathf.Cos(Time.time * 2f) * 20f
                );
                
                var environmentState = new Dictionary<string, object>
                {
                    { "playerPosition", playerPos },
                    { "playerSpeed", Random.Range(3f, 12f) }
                };

                lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
                
                float frameTime = Time.realtimeSinceStartup - frameStart;
                frameTimes.Add(frameTime);
                frameCount++;
                
                yield return null;
            }

            // Assert performance targets
            float averageFrameTime = frameTimes.Average();
            float maxFrameTime = frameTimes.Max();
            float targetFrameTime = 1f / 60f; // 60 FPS target

            Assert.Less(averageFrameTime, targetFrameTime * 0.5f, 
                $"Average frame time too high: {averageFrameTime * 1000f}ms (target: {targetFrameTime * 500f}ms)");
            Assert.Less(maxFrameTime, targetFrameTime, 
                $"Max frame time exceeds 60 FPS: {maxFrameTime * 1000f}ms");
            
            UnityEngine.Debug.Log($"Performance Test Results: Avg: {averageFrameTime * 1000f:F2}ms, Max: {maxFrameTime * 1000f:F2}ms, Frames: {frameCount}");
        }

        [Test]
        [Performance]
        public void Performance_TransitionProcessing_ScalesWithActiveTransitions()
        {
            // Arrange
            CreateLightArray(50);
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            var processingTimes = new List<float>();
            var transitionCounts = new int[] { 5, 15, 30, 45 };

            foreach (int targetTransitions in transitionCounts)
            {
                // Create specific number of transitions
                for (int i = 0; i < targetTransitions && i < testLights.Count; i++)
                {
                    lightingEngine.TriggerPulseEffect(testLights[i].transform.position, 1f, 2f);
                }

                // Measure processing time
                var stopwatch = Stopwatch.StartNew();
                
                // Process transitions for a fixed number of iterations
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    var environmentState = new Dictionary<string, object>
                    {
                        { "playerPosition", Vector3.zero },
                        { "playerSpeed", 5f }
                    };
                    lightingEngine.UpdateGeneration(0.016f, environmentState);
                }
                
                stopwatch.Stop();
                processingTimes.Add(stopwatch.ElapsedMilliseconds);

                // Clear transitions for next test
                System.Threading.Thread.Sleep(1000); // Let transitions complete
            }

            // Assert reasonable scaling
            float maxProcessingTime = processingTimes.Max();
            Assert.Less(maxProcessingTime, 50f, $"Transition processing too slow: {maxProcessingTime}ms");
            
            // Verify scaling is not exponential
            for (int i = 1; i < processingTimes.Count; i++)
            {
                float ratio = processingTimes[i] / processingTimes[0];
                float transitionRatio = (float)transitionCounts[i] / transitionCounts[0];
                
                Assert.Less(ratio, transitionRatio * 2f, 
                    $"Processing time scaling too aggressive: {ratio} vs transition ratio {transitionRatio}");
            }
        }

        [Test]
        [Performance]
        public void Performance_MemoryUsage_StaysWithinBounds()
        {
            // Arrange
            CreateLightArray(100);
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            long initialMemory = System.GC.GetTotalMemory(true);

            // Act - Register all lights and create many transitions
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            // Create many transitions
            for (int i = 0; i < 50; i++)
            {
                Vector3 randomPos = Random.insideUnitSphere * 50f;
                lightingEngine.TriggerPulseEffect(randomPos, 20f, Random.Range(1.5f, 3f));
            }

            long peakMemory = System.GC.GetTotalMemory(false);
            long memoryIncrease = peakMemory - initialMemory;

            // Assert memory usage is reasonable
            float memoryIncreaseMB = memoryIncrease / (1024f * 1024f);
            Assert.Less(memoryIncreaseMB, 10f, $"Memory usage too high: {memoryIncreaseMB:F2}MB increase");

            // Test cleanup effectiveness
            lightingEngine.CleanupDistantContent(1f, new Vector3(1000f, 0f, 0f));
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            long afterCleanupMemory = System.GC.GetTotalMemory(true);
            long cleanupReduction = peakMemory - afterCleanupMemory;
            
            Assert.Greater(cleanupReduction, 0, "Cleanup should reduce memory usage");
        }

        [UnityTest]
        [Performance]
        public IEnumerator Performance_QualityScaling_AdjustsPerformanceAppropriately()
        {
            // Arrange
            CreateLightArray(60);
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            var qualityLevels = new float[] { 1.0f, 0.75f, 0.5f, 0.25f };
            var performanceResults = new List<float>();

            foreach (float qualityLevel in qualityLevels)
            {
                // Set quality level
                lightingEngine.SetQualityLevel(qualityLevel);
                
                var frameTimes = new List<float>();
                int testFrames = 30;

                // Measure performance at this quality level
                for (int frame = 0; frame < testFrames; frame++)
                {
                    float frameStart = Time.realtimeSinceStartup;
                    
                    var environmentState = new Dictionary<string, object>
                    {
                        { "playerPosition", Random.insideUnitSphere * 30f },
                        { "playerSpeed", Random.Range(5f, 15f) }
                    };

                    lightingEngine.UpdateGeneration(0.016f, environmentState);
                    
                    float frameTime = Time.realtimeSinceStartup - frameStart;
                    frameTimes.Add(frameTime);
                    
                    yield return null;
                }

                float averageFrameTime = frameTimes.Average();
                performanceResults.Add(averageFrameTime);
            }

            // Assert that lower quality levels perform better
            for (int i = 1; i < performanceResults.Count; i++)
            {
                Assert.LessOrEqual(performanceResults[i], performanceResults[i - 1] * 1.1f, 
                    $"Quality level {qualityLevels[i]} should perform better than {qualityLevels[i - 1]}");
            }

            // Verify performance cost tracking reflects quality changes
            lightingEngine.SetQualityLevel(0.25f);
            float lowQualityCost = lightingEngine.CurrentPerformanceCost;
            
            lightingEngine.SetQualityLevel(1.0f);
            float highQualityCost = lightingEngine.CurrentPerformanceCost;
            
            Assert.GreaterOrEqual(highQualityCost, lowQualityCost, 
                "Higher quality should report higher performance cost");
        }

        [Test]
        [Performance]
        public void Performance_ProximityDetection_OptimizedForManyLights()
        {
            // Arrange - Create many lights in a large area
            testLights.Clear();
            for (int i = 0; i < 200; i++)
            {
                var lightObject = new GameObject($"ProximityTestLight_{i}");
                lightObject.transform.position = Random.insideUnitSphere * 200f;
                
                var light = lightObject.AddComponent<Light>();
                light.intensity = 1f;
                light.color = Color.cyan;
                
                lightObject.tag = "NeonLight";
                testLights.Add(lightObject);
            }

            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            // Act - Measure proximity detection performance
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 100; iteration++)
            {
                Vector3 playerPos = Random.insideUnitSphere * 100f;
                var environmentState = new Dictionary<string, object>
                {
                    { "playerPosition", playerPos },
                    { "playerSpeed", 8f }
                };

                lightingEngine.UpdateGeneration(0.016f, environmentState);
            }
            
            stopwatch.Stop();

            // Assert performance is acceptable
            float averageIterationTime = stopwatch.ElapsedMilliseconds / 100f;
            Assert.Less(averageIterationTime, 5f, 
                $"Proximity detection too slow with many lights: {averageIterationTime}ms per iteration");
            
            UnityEngine.Debug.Log($"Proximity Detection Performance: {averageIterationTime:F2}ms per iteration with {testLights.Count} lights");
        }
    }
}