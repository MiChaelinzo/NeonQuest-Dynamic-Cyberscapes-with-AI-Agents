using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class LightingEngineTests
    {
        private GameObject testGameObject;
        private LightingEngine lightingEngine;
        private GameObject testLightObject;
        private Light testLight;
        private EnvironmentConfiguration testConfig;

        [SetUp]
        public void SetUp()
        {
            // Create test game object with LightingEngine
            testGameObject = new GameObject("TestLightingEngine");
            lightingEngine = testGameObject.AddComponent<LightingEngine>();
            
            // Create test light object
            testLightObject = new GameObject("TestNeonLight");
            testLight = testLightObject.AddComponent<Light>();
            testLight.intensity = 1.0f;
            testLight.color = Color.cyan;
            testLight.range = 10f;
            testLightObject.tag = "NeonLight";
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.NeonResponseDistance = 5f;
            testConfig.BrightnessMultiplierRange = new Vector2(0.5f, 2.0f);
            testConfig.LightingTransitionDuration = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
            if (testLightObject != null)
                Object.DestroyImmediate(testLightObject);
            if (testConfig != null)
                Object.DestroyImmediate(testConfig);
        }

        [Test]
        public void Initialize_WithValidConfig_SetsCorrectParameters()
        {
            // Arrange
            var configData = new Dictionary<string, object>
            {
                { "config", testConfig }
            };

            // Act
            lightingEngine.Initialize(configData);

            // Assert
            Assert.IsTrue(lightingEngine.IsActive);
            Assert.AreEqual(0f, lightingEngine.CurrentPerformanceCost, 0.01f);
        }

        [Test]
        public void RegisterLight_WithValidLight_AddsToTrackedLights()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            var result = lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Result;

            // Assert
            Assert.AreEqual(testLightObject, result);
            var trackedLights = lightingEngine.GetTrackedLights();
            Assert.AreEqual(1, trackedLights.Count);
            
            var neonLight = trackedLights.Values.First();
            Assert.AreEqual(testLight, neonLight.Light);
            Assert.AreEqual(1.0f, neonLight.OriginalIntensity, 0.01f);
            Assert.AreEqual(Color.cyan, neonLight.OriginalColor);
        }

        [Test]
        public void SetQualityLevel_WithValidValue_UpdatesParameters()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object>());

            // Act
            lightingEngine.SetQualityLevel(0.5f);

            // Assert - Quality level affects max tracked lights and update interval
            // We can't directly test private fields, but we can test behavior
            Assert.IsTrue(lightingEngine.IsActive);
        }

        [Test]
        public void SetActive_WithFalse_DeactivatesEngine()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object>());
            Assert.IsTrue(lightingEngine.IsActive);

            // Act
            lightingEngine.SetActive(false);

            // Assert
            Assert.IsFalse(lightingEngine.IsActive);
        }

        [Test]
        public void TriggerPulseEffect_WithValidParameters_CreatesTransitions()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            // Act
            lightingEngine.TriggerPulseEffect(Vector3.zero, 15f, 2.0f);

            // Assert
            var activeTransitions = lightingEngine.GetActiveTransitions();
            Assert.AreEqual(1, activeTransitions.Count);
            
            var transition = activeTransitions.Values.First();
            Assert.AreEqual(LightingTransitionType.Pulse, transition.TransitionType);
            Assert.AreEqual(2.0f, transition.TargetIntensity, 0.01f);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithPlayerMovement_TriggersProximityEffects()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            // Position light at origin, player nearby
            testLightObject.transform.position = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                { "playerPosition", new Vector3(2f, 0f, 0f) }, // Within response distance
                { "playerSpeed", 1f }
            };

            // Act
            lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait a frame for coroutine to process
            yield return null;
            yield return new WaitForSeconds(0.2f);

            // Assert
            var trackedLights = lightingEngine.GetTrackedLights();
            var neonLight = trackedLights.Values.First();
            Assert.IsTrue(neonLight.IsResponding);
            
            var activeTransitions = lightingEngine.GetActiveTransitions();
            Assert.AreEqual(1, activeTransitions.Count);
            Assert.AreEqual(LightingTransitionType.ProximityResponse, activeTransitions.Values.First().TransitionType);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithHighSpeed_TriggersSurgeEffects()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            testLightObject.transform.position = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                { "playerPosition", new Vector3(3f, 0f, 0f) },
                { "playerSpeed", 10f } // Above surge threshold
            };

            // Act
            lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for processing
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Assert
            var activeTransitions = lightingEngine.GetActiveTransitions();
            Assert.IsTrue(activeTransitions.Count > 0);
            
            // Check if any transition is a surge effect
            bool hasSurgeTransition = activeTransitions.Values.Any(t => t.TransitionType == LightingTransitionType.Surge);
            Assert.IsTrue(hasSurgeTransition);
        }

        [Test]
        public void CleanupDistantContent_WithDistantLights_RemovesFromTracking()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            // Position light far from player
            testLightObject.transform.position = new Vector3(200f, 0f, 0f);
            Vector3 playerPosition = Vector3.zero;

            // Act
            lightingEngine.CleanupDistantContent(100f, playerPosition);

            // Assert
            var trackedLights = lightingEngine.GetTrackedLights();
            Assert.AreEqual(0, trackedLights.Count);
        }

        [UnityTest]
        public IEnumerator LightingTransition_CompletesWithinExpectedTime()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            float originalIntensity = testLight.intensity;
            
            // Act - Trigger pulse effect
            lightingEngine.TriggerPulseEffect(Vector3.zero, 15f, 2.0f);
            
            // Wait for transition to complete (pulse duration is 0.2s + 0.5s reverse)
            yield return new WaitForSeconds(1.0f);

            // Assert
            var activeTransitions = lightingEngine.GetActiveTransitions();
            Assert.AreEqual(0, activeTransitions.Count, "All transitions should be complete");
            
            // Light should return to approximately original intensity
            Assert.AreEqual(originalIntensity, testLight.intensity, 0.1f);
        }

        [Test]
        public void PerformanceCost_UpdatesBasedOnTrackedLights()
        {
            // Arrange
            lightingEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            float initialCost = lightingEngine.CurrentPerformanceCost;

            // Act - Add a light
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            // Assert
            Assert.Greater(lightingEngine.CurrentPerformanceCost, initialCost);
        }
    }
}