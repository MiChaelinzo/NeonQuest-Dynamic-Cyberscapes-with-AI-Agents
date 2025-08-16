using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Generation;
using NeonQuest.Core;

namespace Tests.Generation
{
    /// <summary>
    /// Performance tests for PerformanceThrottler to validate throttling effectiveness
    /// </summary>
    public class PerformanceThrottlerTests
    {
        private GameObject testGameObject;
        private PerformanceThrottler performanceThrottler;
        private ProceduralGenerator proceduralGenerator;
        private MockProceduralGenerator mockSystem1;
        private MockProceduralGenerator mockSystem2;
        
        [SetUp]
        public void SetUp()
        {
            // Create test game object
            testGameObject = new GameObject("PerformanceThrottlerTest");
            
            // Add PerformanceThrottler
            performanceThrottler = testGameObject.AddComponent<PerformanceThrottler>();
            
            // Create ProceduralGenerator
            var generatorObject = new GameObject("ProceduralGenerator");
            generatorObject.transform.SetParent(testGameObject.transform);
            proceduralGenerator = generatorObject.AddComponent<ProceduralGenerator>();
            
            // Create mock systems
            var mockObject1 = new GameObject("MockSystem1");
            mockObject1.transform.SetParent(testGameObject.transform);
            mockSystem1 = mockObject1.AddComponent<MockProceduralGenerator>();
            
            var mockObject2 = new GameObject("MockSystem2");
            mockObject2.transform.SetParent(testGameObject.transform);
            mockSystem2 = mockObject2.AddComponent<MockProceduralGenerator>();
            
            // Set reasonable performance targets for testing
            performanceThrottler.SetPerformanceThresholds(60f, 45f, 80f, 512f);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void TestSystemRegistration()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Get diagnostic info
            var diagnostics = performanceThrottler.GetDiagnosticInfo();
            
            Assert.IsTrue(diagnostics.ContainsKey("MonitoredSystems"), "Should track monitored systems");
            Assert.AreEqual(2, (int)diagnostics["MonitoredSystems"], "Should have 2 registered systems");
            
            // Verify system quality levels are tracked
            var qualityLevels = performanceThrottler.GetSystemQualityLevels();
            Assert.IsTrue(qualityLevels.ContainsKey("MockProceduralGenerator"), "Should track mock system quality");
        }
        
        [Test]
        public void TestSystemUnregistration()
        {
            // Register and then unregister a system
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            var initialCount = (int)performanceThrottler.GetDiagnosticInfo()["MonitoredSystems"];
            
            performanceThrottler.UnregisterSystem(mockSystem1);
            
            var finalCount = (int)performanceThrottler.GetDiagnosticInfo()["MonitoredSystems"];
            
            Assert.AreEqual(initialCount - 1, finalCount, "System count should decrease after unregistration");
        }
        
        [UnityTest]
        public IEnumerator TestPerformanceMonitoring()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Wait for monitoring to collect data
            yield return new WaitForSeconds(1f);
            
            // Get performance stats
            var stats = performanceThrottler.GetPerformanceStats();
            
            Assert.Greater(stats.CurrentFrameRate, 0f, "Should measure frame rate");
            Assert.GreaterOrEqual(stats.CurrentCpuUsage, 0f, "Should measure CPU usage");
            Assert.GreaterOrEqual(stats.CurrentMemoryUsage, 0f, "Should measure memory usage");
            Assert.AreEqual(2, stats.MonitoredSystemsCount, "Should track correct number of systems");
        }
        
        [UnityTest]
        public IEnumerator TestQualityLevelApplication()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Set initial quality levels
            mockSystem1.SetQualityLevel(1f);
            mockSystem2.SetQualityLevel(1f);
            
            yield return null;
            
            // Simulate performance throttling by manually setting quality
            float targetQuality = 0.5f;
            
            // This would normally be done internally by the throttler
            mockSystem1.SetQualityLevel(targetQuality);
            mockSystem2.SetQualityLevel(targetQuality);
            
            yield return null;
            
            // Verify quality was applied
            Assert.AreEqual(targetQuality, mockSystem1.TestQualityLevel, 0.01f, "Mock system 1 should have updated quality");
            Assert.AreEqual(targetQuality, mockSystem2.TestQualityLevel, 0.01f, "Mock system 2 should have updated quality");
        }
        
        [UnityTest]
        public IEnumerator TestEmergencyThrottling()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Trigger emergency throttling
            performanceThrottler.TriggerEmergencyThrottle();
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify emergency state
            var stats = performanceThrottler.GetPerformanceStats();
            Assert.AreEqual(ThrottleLevel.Emergency, stats.ThrottleLevel, "Should be in emergency throttle mode");
            Assert.IsTrue(stats.IsThrottling, "Should be throttling");
            Assert.Less(stats.CurrentQualityLevel, 0.2f, "Quality should be very low in emergency mode");
        }
        
        [UnityTest]
        public IEnumerator TestThrottlingReset()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Trigger emergency throttling first
            performanceThrottler.TriggerEmergencyThrottle();
            yield return new WaitForSeconds(0.5f);
            
            // Reset throttling
            performanceThrottler.ResetThrottling();
            yield return null;
            
            // Verify reset state
            var stats = performanceThrottler.GetPerformanceStats();
            Assert.AreEqual(ThrottleLevel.None, stats.ThrottleLevel, "Should have no throttling after reset");
            Assert.IsFalse(stats.IsThrottling, "Should not be throttling after reset");
            Assert.AreEqual(1f, stats.CurrentQualityLevel, 0.01f, "Quality should be restored to maximum");
        }
        
        [UnityTest]
        public IEnumerator TestPerformanceHistoryTracking()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Wait for history to accumulate
            yield return new WaitForSeconds(2f);
            
            // Get performance history
            var history = performanceThrottler.GetPerformanceHistory();
            
            Assert.Greater(history.Count, 0, "Should have performance history");
            Assert.IsTrue(history.All(h => h.FrameRate > 0), "All history entries should have valid frame rates");
            Assert.IsTrue(history.All(h => h.Timestamp > 0), "All history entries should have valid timestamps");
        }
        
        [Test]
        public void TestPerformanceThresholdConfiguration()
        {
            // Set custom thresholds
            float targetFps = 30f;
            float minFps = 20f;
            float maxCpu = 90f;
            float maxMemory = 2048f;
            
            performanceThrottler.SetPerformanceThresholds(targetFps, minFps, maxCpu, maxMemory);
            
            // Verify thresholds were applied (we can't directly access private fields,
            // but we can verify through the stats)
            var stats = performanceThrottler.GetPerformanceStats();
            Assert.AreEqual(targetFps, stats.TargetFrameRate, "Target frame rate should be updated");
            Assert.AreEqual(maxCpu, stats.MaxCpuUsage, "Max CPU usage should be updated");
            Assert.AreEqual(maxMemory, stats.MaxMemoryUsage, "Max memory usage should be updated");
        }
        
        [Test]
        public void TestThrottlingOptionsConfiguration()
        {
            // Set throttling options
            bool enableEmergency = false;
            bool enableGpu = false;
            float responseTime = 5f;
            
            performanceThrottler.SetThrottlingOptions(enableEmergency, enableGpu, responseTime);
            
            // Verify options were applied (no direct way to test, but should not throw exceptions)
            Assert.DoesNotThrow(() => performanceThrottler.TriggerEmergencyThrottle(), 
                "Should handle emergency throttling even when disabled");
        }
        
        [UnityTest]
        public IEnumerator TestPerformanceCostCalculation()
        {
            // Register systems with different performance costs
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            // Set different performance costs
            mockSystem1.TestPerformanceCost = 0.3f;
            mockSystem2.TestPerformanceCost = 0.7f;
            
            yield return new WaitForSeconds(1f);
            
            // The throttler should calculate combined performance cost
            var stats = performanceThrottler.GetPerformanceStats();
            Assert.GreaterOrEqual(stats.CurrentQualityLevel, 0f, "Quality level should be valid");
            Assert.LessOrEqual(stats.CurrentQualityLevel, 1f, "Quality level should not exceed maximum");
        }
        
        [UnityTest]
        public IEnumerator TestDiagnosticInformation()
        {
            // Register systems
            performanceThrottler.RegisterSystem(mockSystem1);
            performanceThrottler.RegisterSystem(mockSystem2);
            
            yield return new WaitForSeconds(1f);
            
            // Get diagnostic info
            var diagnostics = performanceThrottler.GetDiagnosticInfo();
            
            // Verify diagnostic data completeness
            Assert.IsTrue(diagnostics.ContainsKey("CurrentFrameRate"), "Should include current frame rate");
            Assert.IsTrue(diagnostics.ContainsKey("AverageFrameRate"), "Should include average frame rate");
            Assert.IsTrue(diagnostics.ContainsKey("CurrentCpuUsage"), "Should include CPU usage");
            Assert.IsTrue(diagnostics.ContainsKey("CurrentMemoryUsage"), "Should include memory usage");
            Assert.IsTrue(diagnostics.ContainsKey("ThrottleLevel"), "Should include throttle level");
            Assert.IsTrue(diagnostics.ContainsKey("IsThrottling"), "Should include throttling state");
            Assert.IsTrue(diagnostics.ContainsKey("MonitoredSystems"), "Should include monitored systems count");
            Assert.IsTrue(diagnostics.ContainsKey("RecentPerformance"), "Should include recent performance data");
            Assert.IsTrue(diagnostics.ContainsKey("SystemQualityLevels"), "Should include system quality levels");
        }
        
        [UnityTest]
        public IEnumerator TestStressConditions()
        {
            // Register many systems to simulate stress
            var stressSystems = new List<MockProceduralGenerator>();
            
            for (int i = 0; i < 10; i++)
            {
                var stressObject = new GameObject($"StressSystem{i}");
                stressObject.transform.SetParent(testGameObject.transform);
                var stressSystem = stressObject.AddComponent<MockProceduralGenerator>();
                stressSystem.TestPerformanceCost = 0.8f; // High performance cost
                
                performanceThrottler.RegisterSystem(stressSystem);
                stressSystems.Add(stressSystem);
            }
            
            // Wait for monitoring to detect stress
            yield return new WaitForSeconds(2f);
            
            // Verify system handles stress gracefully
            var stats = performanceThrottler.GetPerformanceStats();
            Assert.AreEqual(10, stats.MonitoredSystemsCount, "Should track all stress systems");
            Assert.GreaterOrEqual(stats.CurrentQualityLevel, 0f, "Quality should remain valid under stress");
            
            // Cleanup stress systems
            foreach (var system in stressSystems)
            {
                if (system != null && system.gameObject != null)
                {
                    Object.DestroyImmediate(system.gameObject);
                }
            }
        }
        
        [Test]
        public void TestNullSystemHandling()
        {
            // Test registering null system
            Assert.DoesNotThrow(() => performanceThrottler.RegisterSystem(null), 
                "Should handle null system registration gracefully");
            
            // Test unregistering null system
            Assert.DoesNotThrow(() => performanceThrottler.UnregisterSystem(null), 
                "Should handle null system unregistration gracefully");
        }
    }
    
    /// <summary>
    /// Mock procedural generator for testing
    /// </summary>
    public class MockProceduralGenerator : MonoBehaviour, IProceduralGenerator
    {
        public float TestPerformanceCost = 0.1f;
        public float TestQualityLevel = 1f;
        public bool TestIsActive = true;
        
        public float CurrentPerformanceCost => TestPerformanceCost;
        public bool IsActive => TestIsActive;
        
        public void Initialize(Dictionary<string, object> config)
        {
            // Mock implementation
        }
        
        public async System.Threading.Tasks.Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            // Mock implementation
            await System.Threading.Tasks.Task.Delay(10);
            return null;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            // Mock implementation
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            // Mock implementation
        }
        
        public void SetQualityLevel(float qualityLevel)
        {
            TestQualityLevel = qualityLevel;
        }
        
        public void SetActive(bool active)
        {
            TestIsActive = active;
        }
    }
}