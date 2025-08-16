using NUnit.Framework;
using UnityEngine;
using NeonQuest.Generation;
using NeonQuest.Configuration;
using System.Collections.Generic;

namespace Tests.Generation
{
    /// <summary>
    /// Basic validation tests for ProceduralGenerator and PerformanceThrottler
    /// </summary>
    public class ProceduralGeneratorValidationTests
    {
        [Test]
        public void TestProceduralGeneratorCreation()
        {
            var gameObject = new GameObject("TestProceduralGenerator");
            var proceduralGenerator = gameObject.AddComponent<ProceduralGenerator>();
            
            Assert.IsNotNull(proceduralGenerator, "ProceduralGenerator should be created");
            Assert.IsTrue(proceduralGenerator.IsActive, "ProceduralGenerator should be active by default");
            Assert.AreEqual(0f, proceduralGenerator.CurrentPerformanceCost, "Initial performance cost should be 0");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestPerformanceThrottlerCreation()
        {
            var gameObject = new GameObject("TestPerformanceThrottler");
            var performanceThrottler = gameObject.AddComponent<PerformanceThrottler>();
            
            Assert.IsNotNull(performanceThrottler, "PerformanceThrottler should be created");
            Assert.AreEqual(1f, performanceThrottler.CurrentQualityLevel, "Initial quality level should be 1.0");
            Assert.IsFalse(performanceThrottler.IsThrottling, "Should not be throttling initially");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestProceduralGeneratorInitialization()
        {
            var gameObject = new GameObject("TestProceduralGenerator");
            var proceduralGenerator = gameObject.AddComponent<ProceduralGenerator>();
            
            var config = new EnvironmentConfiguration
            {
                CorridorGenerationDistance = 50f,
                MaxActiveSegments = 10,
                PerformanceThreshold = 0.8f
            };
            
            var configData = new Dictionary<string, object>
            {
                ["config"] = config
            };
            
            Assert.DoesNotThrow(() => proceduralGenerator.Initialize(configData), 
                "ProceduralGenerator initialization should not throw");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestPerformanceThrottlerConfiguration()
        {
            var gameObject = new GameObject("TestPerformanceThrottler");
            var performanceThrottler = gameObject.AddComponent<PerformanceThrottler>();
            
            Assert.DoesNotThrow(() => performanceThrottler.SetPerformanceThresholds(60f, 45f, 80f, 512f),
                "Setting performance thresholds should not throw");
            
            Assert.DoesNotThrow(() => performanceThrottler.SetThrottlingOptions(true, true, 2f),
                "Setting throttling options should not throw");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestProceduralGeneratorQualityControl()
        {
            var gameObject = new GameObject("TestProceduralGenerator");
            var proceduralGenerator = gameObject.AddComponent<ProceduralGenerator>();
            
            // Test quality level setting
            proceduralGenerator.SetQualityLevel(0.5f);
            // We can't directly verify the internal quality level, but it should not throw
            
            // Test active state control
            proceduralGenerator.SetActive(false);
            Assert.IsFalse(proceduralGenerator.IsActive, "Should be inactive after SetActive(false)");
            
            proceduralGenerator.SetActive(true);
            Assert.IsTrue(proceduralGenerator.IsActive, "Should be active after SetActive(true)");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestPerformanceThrottlerDiagnostics()
        {
            var gameObject = new GameObject("TestPerformanceThrottler");
            var performanceThrottler = gameObject.AddComponent<PerformanceThrottler>();
            
            var diagnostics = performanceThrottler.GetDiagnosticInfo();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsTrue(diagnostics.ContainsKey("CurrentFrameRate"), "Should include frame rate");
            Assert.IsTrue(diagnostics.ContainsKey("CurrentQualityLevel"), "Should include quality level");
            Assert.IsTrue(diagnostics.ContainsKey("IsThrottling"), "Should include throttling state");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestProceduralGeneratorDiagnostics()
        {
            var gameObject = new GameObject("TestProceduralGenerator");
            var proceduralGenerator = gameObject.AddComponent<ProceduralGenerator>();
            
            var diagnostics = proceduralGenerator.GetDiagnosticInfo();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsTrue(diagnostics.ContainsKey("QueuedRequests"), "Should include queued requests");
            Assert.IsTrue(diagnostics.ContainsKey("ActiveGenerations"), "Should include active generations");
            Assert.IsTrue(diagnostics.ContainsKey("PerformanceCost"), "Should include performance cost");
            
            var systemsInfo = proceduralGenerator.GetSystemsInfo();
            Assert.IsNotNull(systemsInfo, "Systems info should not be null");
            
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void TestEnvironmentConfigurationValidation()
        {
            var config = new EnvironmentConfiguration
            {
                CorridorGenerationDistance = 50f,
                CorridorCleanupDistance = 100f,
                NeonResponseDistance = 5f,
                LightingTransitionDuration = 2f,
                AtmosphereTransitionSpeed = 0.1f,
                MaxActiveSegments = 10,
                PerformanceThreshold = 0.8f
            };
            
            Assert.IsTrue(config.IsValid(), "Valid configuration should pass validation");
            
            // Test invalid configuration
            config.CorridorGenerationDistance = -1f;
            Assert.IsFalse(config.IsValid(), "Invalid configuration should fail validation");
        }
    }
}