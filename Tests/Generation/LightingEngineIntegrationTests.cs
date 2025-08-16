using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Generation;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class LightingEngineIntegrationTests
    {
        private GameObject testScene;
        private LightingEngine lightingEngine;
        private PlayerMovementTracker playerTracker;
        private List<GameObject> testLights;
        private EnvironmentConfiguration testConfig;
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            // Create test scene
            testScene = new GameObject("TestScene");
            
            // Create player object with movement tracker
            playerObject = new GameObject("Player");
            playerTracker = playerObject.AddComponent<PlayerMovementTracker>();
            
            // Create lighting engine
            var lightingEngineObject = new GameObject("LightingEngine");
            lightingEngine = lightingEngineObject.AddComponent<LightingEngine>();
            lightingEngineObject.transform.SetParent(testScene.transform);
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.NeonResponseDistance = 8f;
            testConfig.BrightnessMultiplierRange = new Vector2(0.3f, 3.0f);
            testConfig.LightingTransitionDuration = 0.5f;
            
            // Create multiple test lights
            testLights = new List<GameObject>();
            CreateTestLightGrid();
        }

        [TearDown]
        public void TearDown()
        {
            if (testScene != null)
                Object.DestroyImmediate(testScene);
            if (playerObject != null)
                Object.DestroyImmediate(playerObject);
            if (testConfig != null)
                Object.DestroyImmediate(testConfig);
            
            foreach (var light in testLights)
            {
                if (light != null)
                    Object.DestroyImmediate(light);
            }
            testLights.Clear();
        }

        private void CreateTestLightGrid()
        {
            // Create a 3x3 grid of neon lights
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var lightObject = new GameObject($"NeonLight_{x}_{z}");
                    lightObject.transform.position = new Vector3(x * 10f, 2f, z * 10f);
                    lightObject.transform.SetParent(testScene.transform);
                    
                    var light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.intensity = 1.5f;
                    light.color = new Color(0.2f, 0.8f, 1.0f); // Cyan neon color
                    light.range = 15f;
                    
                    lightObject.tag = "NeonLight";
                    testLights.Add(lightObject);
                }
            }
        }

        [Test]
        public void Integration_InitializeWithMultipleLights_RegistersAllNeonLights()
        {
            // Arrange
            var configData = new Dictionary<string, object> { { "config", testConfig } };

            // Act
            lightingEngine.Initialize(configData);

            // Register all lights
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            // Assert
            var trackedLights = lightingEngine.GetTrackedLights();
            Assert.AreEqual(testLights.Count, trackedLights.Count);
            
            foreach (var neonLight in trackedLights.Values)
            {
                Assert.IsNotNull(neonLight.Light);
                Assert.AreEqual(1.5f, neonLight.OriginalIntensity, 0.01f);
                Assert.IsFalse(neonLight.IsResponding);
            }
        }

        [UnityTest]
        public IEnumerator Integration_PlayerMovementThroughLightGrid_TriggersSequentialResponses()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            // Simulate player movement from (-15, 0, 0) to (15, 0, 0)
            var positions = new List<Vector3>();
            for (float x = -15f; x <= 15f; x += 2.5f)
            {
                positions.Add(new Vector3(x, 0f, 0f));
            }

            var responseHistory = new List<int>();

            // Act & Assert
            foreach (var position in positions)
            {
                playerObject.transform.position = position;
                
                var environmentState = new Dictionary<string, object>
                {
                    { "playerPosition", position },
                    { "playerSpeed", 5f }
                };

                lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
                
                // Wait for lighting updates
                yield return new WaitForSeconds(0.15f);
                
                // Count responding lights
                var respondingCount = lightingEngine.GetTrackedLights().Values.Count(nl => nl.IsResponding);
                responseHistory.Add(respondingCount);
            }

            // Verify that lights responded as player moved through the grid
            Assert.IsTrue(responseHistory.Any(count => count > 0), "At least some lights should have responded");
            Assert.IsTrue(responseHistory.Max() <= 4, "No more than 4 lights should respond simultaneously");
        }

        [UnityTest]
        public IEnumerator Integration_HighSpeedMovement_CreatesCoordinatedSurgeEffects()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            // Act - Simulate high-speed movement through center
            var environmentState = new Dictionary<string, object>
            {
                { "playerPosition", Vector3.zero },
                { "playerSpeed", 15f } // High speed to trigger surge
            };

            lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for surge effects to be created
            yield return new WaitForSeconds(0.1f);

            // Assert
            var activeTransitions = lightingEngine.GetActiveTransitions();
            var surgeTransitions = activeTransitions.Values.Where(t => t.TransitionType == LightingTransitionType.Surge).ToList();
            
            Assert.IsTrue(surgeTransitions.Count > 0, "High-speed movement should create surge effects");
            Assert.IsTrue(surgeTransitions.Count <= 4, "Surge should affect nearby lights only");
            
            // Verify surge intensities are higher than original
            foreach (var transition in surgeTransitions)
            {
                Assert.Greater(transition.TargetIntensity, 1.5f, "Surge should increase light intensity");
            }
        }

        [UnityTest]
        public IEnumerator Integration_SmoothTransitions_MaintainConsistentTiming()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var centerLight = testLights[4]; // Center light in 3x3 grid
            
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", centerLight } 
            }).Wait();

            var originalIntensity = centerLight.GetComponent<Light>().intensity;
            var intensityHistory = new List<float>();
            var timeStamps = new List<float>();

            // Act - Trigger proximity response
            var environmentState = new Dictionary<string, object>
            {
                { "playerPosition", centerLight.transform.position + Vector3.right * 3f },
                { "playerSpeed", 2f }
            };

            lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Record intensity changes over time
            float startTime = Time.time;
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(0.05f);
                intensityHistory.Add(centerLight.GetComponent<Light>().intensity);
                timeStamps.Add(Time.time - startTime);
            }

            // Assert smooth transition characteristics
            Assert.IsTrue(intensityHistory.Count > 10, "Should have recorded multiple intensity values");
            
            // Check for smooth progression (no sudden jumps)
            for (int i = 1; i < intensityHistory.Count; i++)
            {
                float intensityChange = Mathf.Abs(intensityHistory[i] - intensityHistory[i - 1]);
                Assert.Less(intensityChange, 0.5f, $"Intensity change too abrupt at step {i}: {intensityChange}");
            }
            
            // Verify transition completed within expected timeframe
            float finalIntensity = intensityHistory.Last();
            Assert.Greater(finalIntensity, originalIntensity, "Light should be brighter due to proximity");
        }

        [UnityTest]
        public IEnumerator Integration_PerformanceUnderLoad_MaintainsStableFramerate()
        {
            // Arrange - Create many lights to stress test
            var additionalLights = new List<GameObject>();
            for (int i = 0; i < 30; i++)
            {
                var lightObject = new GameObject($"StressLight_{i}");
                lightObject.transform.position = Random.insideUnitSphere * 50f;
                
                var light = lightObject.AddComponent<Light>();
                light.intensity = Random.Range(0.5f, 2.0f);
                light.color = Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 1f);
                
                lightObject.tag = "NeonLight";
                additionalLights.Add(lightObject);
            }

            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            // Register all lights
            foreach (var lightObj in testLights.Concat(additionalLights))
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            var frameTimeHistory = new List<float>();
            float testDuration = 2f;
            float startTime = Time.time;

            // Act - Simulate active gameplay with many lighting effects
            while (Time.time - startTime < testDuration)
            {
                // Move player randomly to trigger various effects
                Vector3 randomPosition = Random.insideUnitSphere * 20f;
                float randomSpeed = Random.Range(1f, 20f);
                
                var environmentState = new Dictionary<string, object>
                {
                    { "playerPosition", randomPosition },
                    { "playerSpeed", randomSpeed }
                };

                float frameStart = Time.realtimeSinceStartup;
                lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
                float frameTime = Time.realtimeSinceStartup - frameStart;
                
                frameTimeHistory.Add(frameTime);
                
                yield return null;
            }

            // Cleanup additional lights
            foreach (var light in additionalLights)
            {
                if (light != null)
                    Object.DestroyImmediate(light);
            }

            // Assert performance characteristics
            float averageFrameTime = frameTimeHistory.Average();
            float maxFrameTime = frameTimeHistory.Max();
            
            Assert.Less(averageFrameTime, 0.016f, $"Average frame time too high: {averageFrameTime * 1000f}ms");
            Assert.Less(maxFrameTime, 0.033f, $"Max frame time too high: {maxFrameTime * 1000f}ms");
            
            // Verify performance cost tracking
            Assert.LessOrEqual(lightingEngine.CurrentPerformanceCost, 1f, "Performance cost should be normalized");
        }

        [UnityTest]
        public IEnumerator Integration_CleanupAndMemoryManagement_HandlesLightDestruction()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            foreach (var lightObj in testLights)
            {
                lightingEngine.GenerateAsync(new Dictionary<string, object> 
                { 
                    { "lightObject", lightObj } 
                }).Wait();
            }

            int initialTrackedCount = lightingEngine.GetTrackedLights().Count;
            Assert.AreEqual(testLights.Count, initialTrackedCount);

            // Act - Destroy some lights and trigger cleanup
            for (int i = 0; i < 3; i++)
            {
                Object.DestroyImmediate(testLights[i]);
            }

            // Trigger cleanup by moving player far away
            lightingEngine.CleanupDistantContent(5f, new Vector3(1000f, 0f, 0f));
            
            yield return null; // Wait a frame

            // Assert
            var remainingTracked = lightingEngine.GetTrackedLights().Count;
            Assert.Less(remainingTracked, initialTrackedCount, "Destroyed lights should be removed from tracking");
            
            // Verify no null references in tracked lights
            foreach (var neonLight in lightingEngine.GetTrackedLights().Values)
            {
                Assert.IsNotNull(neonLight.Light, "Tracked lights should not contain null references");
            }
        }
    }
}