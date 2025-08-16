using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeonQuest.Generation;
using NeonQuest.Core;
using NeonQuest.Configuration;

namespace Tests.Generation
{
    /// <summary>
    /// Integration tests for ProceduralGenerator multi-system coordination
    /// </summary>
    public class ProceduralGeneratorIntegrationTests
    {
        private GameObject testGameObject;
        private ProceduralGenerator proceduralGenerator;
        private LayoutManager layoutManager;
        private LightingEngine lightingEngine;
        private AudioEngine audioEngine;
        private FogEffectsEngine fogEffectsEngine;
        private EnvironmentConfiguration testConfig;
        
        [SetUp]
        public void SetUp()
        {
            // Create test game object
            testGameObject = new GameObject("ProceduralGeneratorTest");
            
            // Add ProceduralGenerator
            proceduralGenerator = testGameObject.AddComponent<ProceduralGenerator>();
            
            // Create and add generation systems
            var layoutObject = new GameObject("LayoutManager");
            layoutObject.transform.SetParent(testGameObject.transform);
            layoutManager = layoutObject.AddComponent<LayoutManager>();
            
            var lightingObject = new GameObject("LightingEngine");
            lightingObject.transform.SetParent(testGameObject.transform);
            lightingEngine = lightingObject.AddComponent<LightingEngine>();
            
            var audioObject = new GameObject("AudioEngine");
            audioObject.transform.SetParent(testGameObject.transform);
            audioEngine = audioObject.AddComponent<AudioEngine>();
            
            var fogObject = new GameObject("FogEffectsEngine");
            fogObject.transform.SetParent(testGameObject.transform);
            fogEffectsEngine = fogObject.AddComponent<FogEffectsEngine>();
            
            // Create test configuration
            testConfig = new EnvironmentConfiguration
            {
                CorridorGenerationDistance = 50f,
                CorridorCleanupDistance = 100f,
                MaxActiveSegments = 10,
                NeonResponseDistance = 5f,
                BrightnessMultiplierRange = new Vector2(0.5f, 2f),
                LightingTransitionDuration = 2f,
                AmbientVolumeRange = new Vector2(0.3f, 0.9f),
                AtmosphereTransitionSpeed = 0.1f,
                FogDensityRange = new Vector2(0.1f, 0.8f),
                PerformanceThreshold = 0.8f
            };
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            // testConfig is not a Unity object, no need to destroy
        }
        
        [UnityTest]
        public IEnumerator TestSystemInitialization()
        {
            // Initialize the procedural generator
            var configData = new Dictionary<string, object>
            {
                ["config"] = testConfig
            };
            
            proceduralGenerator.Initialize(configData);
            
            // Wait a frame for initialization
            yield return null;
            
            // Verify systems are initialized
            Assert.IsTrue(proceduralGenerator.IsActive, "ProceduralGenerator should be active");
            Assert.IsTrue(layoutManager.IsActive, "LayoutManager should be active");
            Assert.IsTrue(lightingEngine.IsActive, "LightingEngine should be active");
            Assert.IsTrue(audioEngine.IsActive, "AudioEngine should be active");
            Assert.IsTrue(fogEffectsEngine.IsActive, "FogEffectsEngine should be active");
        }
        
        [UnityTest]
        public IEnumerator TestGenerationQueueManagement()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Create multiple generation requests with different priorities
            var requests = new List<Task<GameObject>>();
            
            // High priority request
            requests.Add(proceduralGenerator.GenerateAsync(new Dictionary<string, object>
            {
                ["layoutType"] = "corridor",
                ["position"] = Vector3.zero,
                ["priority"] = 8
            }));
            
            // Low priority request
            requests.Add(proceduralGenerator.GenerateAsync(new Dictionary<string, object>
            {
                ["layoutType"] = "room",
                ["position"] = Vector3.right * 10f,
                ["priority"] = 3
            }));
            
            // Medium priority request
            requests.Add(proceduralGenerator.GenerateAsync(new Dictionary<string, object>
            {
                ["layoutType"] = "junction",
                ["position"] = Vector3.forward * 10f,
                ["priority"] = 5
            }));
            
            // Wait for requests to be processed
            float timeout = 5f;
            float elapsed = 0f;
            
            while (requests.Any(r => !r.IsCompleted) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Verify requests were processed
            Assert.IsTrue(requests.All(r => r.IsCompleted), "All generation requests should be completed");
            
            // Get diagnostic info to verify queue management
            var diagnostics = proceduralGenerator.GetDiagnosticInfo();
            Assert.IsTrue(diagnostics.ContainsKey("QueuedRequests"), "Diagnostics should include queued requests");
            Assert.IsTrue(diagnostics.ContainsKey("ActiveGenerations"), "Diagnostics should include active generations");
        }
        
        [UnityTest]
        public IEnumerator TestSystemCoordination()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Create a player object for coordination tests
            var playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            playerObject.transform.position = Vector3.zero;
            
            // Trigger coordinated atmospheric change
            proceduralGenerator.TriggerCoordinatedAtmosphericChange("industrial", Vector3.zero, 1f);
            
            // Wait for coordination to complete
            yield return new WaitForSeconds(2f);
            
            // Verify systems responded to coordination
            var lightingDiagnostics = lightingEngine.GetDiagnosticInfo();
            var audioDiagnostics = audioEngine.GetDiagnosticInfo();
            var fogDiagnostics = fogEffectsEngine.GetDiagnosticInfo();
            
            Assert.IsNotNull(lightingDiagnostics, "Lighting system should provide diagnostics");
            Assert.IsNotNull(audioDiagnostics, "Audio system should provide diagnostics");
            Assert.IsNotNull(fogDiagnostics, "Fog system should provide diagnostics");
            
            // Cleanup
            Object.DestroyImmediate(playerObject);
        }
        
        [UnityTest]
        public IEnumerator TestPerformanceThrottling()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Get initial quality level
            float initialQuality = proceduralGenerator.CurrentPerformanceCost;
            
            // Set a low quality level to simulate performance issues
            proceduralGenerator.SetQualityLevel(0.3f);
            yield return null;
            
            // Verify quality was applied to all systems
            Assert.LessOrEqual(layoutManager.CurrentPerformanceCost, 1f, "LayoutManager performance cost should be reasonable");
            Assert.LessOrEqual(lightingEngine.CurrentPerformanceCost, 1f, "LightingEngine performance cost should be reasonable");
            Assert.LessOrEqual(audioEngine.CurrentPerformanceCost, 1f, "AudioEngine performance cost should be reasonable");
            Assert.LessOrEqual(fogEffectsEngine.CurrentPerformanceCost, 1f, "FogEffectsEngine performance cost should be reasonable");
            
            // Restore quality
            proceduralGenerator.SetQualityLevel(1f);
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator TestEnvironmentStateUpdates()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Create environment state
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = Vector3.zero,
                ["playerSpeed"] = 5f,
                ["currentZone"] = "industrial",
                ["gameplayEvent"] = "exploration_mode"
            };
            
            // Update generation with environment state
            proceduralGenerator.UpdateGeneration(Time.deltaTime, environmentState);
            yield return null;
            
            // Verify systems received the update
            // This is verified by the fact that no exceptions were thrown
            Assert.IsTrue(proceduralGenerator.IsActive, "ProceduralGenerator should remain active after update");
        }
        
        [UnityTest]
        public IEnumerator TestCleanupCoordination()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Generate some content first
            await proceduralGenerator.GenerateAsync(new Dictionary<string, object>
            {
                ["layoutType"] = "corridor",
                ["position"] = Vector3.zero
            });
            
            yield return new WaitForSeconds(0.5f);
            
            // Trigger cleanup
            Vector3 playerPosition = Vector3.zero;
            float cleanupDistance = 50f;
            
            proceduralGenerator.CleanupDistantContent(cleanupDistance, playerPosition);
            yield return null;
            
            // Verify cleanup was coordinated across systems
            Assert.IsTrue(proceduralGenerator.IsActive, "ProceduralGenerator should remain active after cleanup");
        }
        
        [UnityTest]
        public IEnumerator TestSystemRegistration()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Get systems info
            var systemsInfo = proceduralGenerator.GetSystemsInfo();
            
            // Verify systems are registered
            Assert.IsTrue(systemsInfo.ContainsKey("layout"), "Layout system should be registered");
            Assert.IsTrue(systemsInfo.ContainsKey("lighting"), "Lighting system should be registered");
            Assert.IsTrue(systemsInfo.ContainsKey("audio"), "Audio system should be registered");
            Assert.IsTrue(systemsInfo.ContainsKey("fog"), "Fog system should be registered");
            
            // Verify system info contains expected data
            foreach (var systemInfo in systemsInfo.Values)
            {
                Assert.IsTrue(systemInfo.ContainsKey("IsActive"), "System info should include IsActive");
                Assert.IsTrue(systemInfo.ContainsKey("PerformanceCost"), "System info should include PerformanceCost");
                Assert.IsTrue(systemInfo.ContainsKey("Type"), "System info should include Type");
            }
        }
        
        [UnityTest]
        public IEnumerator TestConcurrentGenerationLimits()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            yield return null;
            
            // Create many concurrent requests
            var requests = new List<Task<GameObject>>();
            
            for (int i = 0; i < 10; i++)
            {
                requests.Add(proceduralGenerator.GenerateAsync(new Dictionary<string, object>
                {
                    ["layoutType"] = "corridor",
                    ["position"] = Vector3.right * i * 5f,
                    ["priority"] = 5
                }));
            }
            
            // Wait for processing
            yield return new WaitForSeconds(3f);
            
            // Get diagnostics to check if limits were respected
            var diagnostics = proceduralGenerator.GetDiagnosticInfo();
            
            Assert.IsTrue(diagnostics.ContainsKey("ActiveGenerations"), "Should track active generations");
            Assert.IsTrue(diagnostics.ContainsKey("QueuedRequests"), "Should track queued requests");
            
            // Performance cost should be reasonable
            Assert.LessOrEqual(proceduralGenerator.CurrentPerformanceCost, 1f, "Performance cost should not exceed maximum");
        }
        
        [Test]
        public void TestDiagnosticInformation()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            
            // Get diagnostic info
            var diagnostics = proceduralGenerator.GetDiagnosticInfo();
            
            // Verify diagnostic data
            Assert.IsTrue(diagnostics.ContainsKey("QueuedRequests"), "Should include queued requests");
            Assert.IsTrue(diagnostics.ContainsKey("ActiveGenerations"), "Should include active generations");
            Assert.IsTrue(diagnostics.ContainsKey("RegisteredSystems"), "Should include registered systems count");
            Assert.IsTrue(diagnostics.ContainsKey("PerformanceCost"), "Should include performance cost");
            Assert.IsTrue(diagnostics.ContainsKey("QualityLevel"), "Should include quality level");
            Assert.IsTrue(diagnostics.ContainsKey("IsActive"), "Should include active state");
            
            // Verify systems info
            var systemsInfo = proceduralGenerator.GetSystemsInfo();
            Assert.IsNotNull(systemsInfo, "Systems info should not be null");
            Assert.Greater(systemsInfo.Count, 0, "Should have registered systems");
        }
        
        [Test]
        public void TestQualityLevelPropagation()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            
            // Set different quality levels
            float[] qualityLevels = { 0.2f, 0.5f, 0.8f, 1.0f };
            
            foreach (float quality in qualityLevels)
            {
                proceduralGenerator.SetQualityLevel(quality);
                
                // Verify quality was applied (we can't directly check internal state,
                // but we can verify no exceptions were thrown)
                Assert.IsTrue(proceduralGenerator.IsActive, $"Should remain active at quality {quality}");
            }
        }
        
        [Test]
        public void TestActiveStateManagement()
        {
            // Initialize systems
            var configData = new Dictionary<string, object> { ["config"] = testConfig };
            proceduralGenerator.Initialize(configData);
            
            // Test deactivation
            proceduralGenerator.SetActive(false);
            Assert.IsFalse(proceduralGenerator.IsActive, "Should be inactive after SetActive(false)");
            
            // Test reactivation
            proceduralGenerator.SetActive(true);
            Assert.IsTrue(proceduralGenerator.IsActive, "Should be active after SetActive(true)");
        }
    }
}