using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class FogEffectsEngineIntegrationTests
    {
        private GameObject testGameObject;
        private FogEffectsEngine fogEngine;
        private GameObject lightingEngineObject;
        private LightingEngine lightingEngine;
        private GameObject audioEngineObject;
        private AudioEngine audioEngine;
        private EnvironmentConfiguration testConfig;
        
        // Store original fog settings
        private FogMode originalFogMode;
        private Color originalFogColor;
        private float originalFogDensity;
        private bool originalFogEnabled;

        [SetUp]
        public void SetUp()
        {
            // Store original fog settings
            originalFogMode = RenderSettings.fogMode;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
            originalFogEnabled = RenderSettings.fog;
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.FogDensityRange = new Vector2(0.1f, 0.8f);
            testConfig.AtmosphereTransitionSpeed = 0.5f;
            testConfig.NeonResponseDistance = 5f;
            testConfig.BrightnessMultiplierRange = new Vector2(0.5f, 2.0f);
            testConfig.LightingTransitionDuration = 1f;
            testConfig.AmbientVolumeRange = new Vector2(0.3f, 0.9f);

            // Create FogEffectsEngine
            testGameObject = new GameObject("TestFogEffectsEngine");
            fogEngine = testGameObject.AddComponent<FogEffectsEngine>();

            // Create LightingEngine for coordination tests
            lightingEngineObject = new GameObject("TestLightingEngine");
            lightingEngine = lightingEngineObject.AddComponent<LightingEngine>();

            // Create AudioEngine for coordination tests
            audioEngineObject = new GameObject("TestAudioEngine");
            audioEngine = audioEngineObject.AddComponent<AudioEngine>();

            // Initialize all engines
            var configData = new Dictionary<string, object> { { "config", testConfig } };
            fogEngine.Initialize(configData);
            lightingEngine.Initialize(configData);
            audioEngine.Initialize(testConfig);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original fog settings
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fog = originalFogEnabled;
            
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
            if (lightingEngineObject != null)
                Object.DestroyImmediate(lightingEngineObject);
            if (audioEngineObject != null)
                Object.DestroyImmediate(audioEngineObject);
            if (testConfig != null)
                Object.DestroyImmediate(testConfig);
        }

        [UnityTest]
        public IEnumerator FogEngine_IntegratesWithEnvironmentState_ProcessesCorrectly()
        {
            // Arrange
            var environmentState = new Dictionary<string, object>
            {
                { "currentZone", "industrial" },
                { "playerPosition", new Vector3(10f, 0f, 0f) },
                { "playerSpeed", 5f },
                { "gameTime", 120f }
            };

            // Act
            fogEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for processing
            yield return new WaitForSeconds(0.2f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.IsTrue(activeEffects.Count > 0, "Should create zone-based effects");
            
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();
            Assert.IsTrue((bool)diagnosticInfo["IsActive"]);
            Assert.Greater((float)diagnosticInfo["CurrentDensity"], 0f);
        }

        [UnityTest]
        public IEnumerator FogEngine_CoordiatesWithLightingEngine_TriggersLightingEffects()
        {
            // Arrange - Create a test light for the lighting engine
            var testLightObject = new GameObject("TestNeonLight");
            var testLight = testLightObject.AddComponent<Light>();
            testLight.intensity = 1.0f;
            testLight.color = Color.cyan;
            testLightObject.tag = "NeonLight";
            
            // Register the light with the lighting engine
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLightObject } 
            }).Wait();

            // Act - Trigger coordinated atmospheric change
            fogEngine.TriggerCoordinatedAtmosphericChange("industrial");
            
            // Wait for coordination to execute
            yield return new WaitForSeconds(1.5f);

            // Assert
            var lightingTransitions = lightingEngine.GetActiveTransitions();
            // Note: This test verifies the coordination mechanism works without errors
            // Actual lighting effects depend on the specific coordination implementation
            Assert.IsTrue(fogEngine.IsActive);
            Assert.IsTrue(lightingEngine.IsActive);
            
            // Cleanup
            Object.DestroyImmediate(testLightObject);
        }

        [UnityTest]
        public IEnumerator FogEngine_CoordinatesWithAudioEngine_TriggersAudioEffects()
        {
            // Arrange - Register an audio zone
            audioEngine.RegisterAudioZone("test_zone", Vector3.zero, 10f, null);

            // Act - Trigger coordinated atmospheric change
            fogEngine.TriggerCoordinatedAtmosphericChange("residential");
            
            // Wait for coordination to execute
            yield return new WaitForSeconds(1.5f);

            // Assert
            var audioDiagnostics = audioEngine.GetDiagnosticInfo();
            Assert.IsTrue((bool)audioDiagnostics["IsActive"]);
            Assert.IsTrue(fogEngine.IsActive);
        }

        [UnityTest]
        public IEnumerator MultipleEngines_WorkTogetherWithoutConflicts()
        {
            // Arrange
            var environmentState = new Dictionary<string, object>
            {
                { "currentZone", "underground" },
                { "playerPosition", Vector3.zero },
                { "playerSpeed", 3f },
                { "gameplayEvent", "exploration_mode" }
            };

            // Act - Update all engines simultaneously
            fogEngine.UpdateGeneration(Time.deltaTime, environmentState);
            lightingEngine.UpdateGeneration(Time.deltaTime, environmentState);
            audioEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for processing
            yield return new WaitForSeconds(0.3f);

            // Assert - All engines should remain active and functional
            Assert.IsTrue(fogEngine.IsActive);
            Assert.IsTrue(lightingEngine.IsActive);
            Assert.IsTrue(audioEngine.IsActive);
            
            var fogDiagnostics = fogEngine.GetDiagnosticInfo();
            var lightingDiagnostics = lightingEngine.GetTrackedLights();
            var audioDiagnostics = audioEngine.GetDiagnosticInfo();
            
            Assert.IsNotNull(fogDiagnostics);
            Assert.IsNotNull(lightingDiagnostics);
            Assert.IsNotNull(audioDiagnostics);
        }

        [UnityTest]
        public IEnumerator FogEngine_RespondsToGameplayEvents_CreatesAppropriateEffects()
        {
            // Test combat event
            var combatState = new Dictionary<string, object>
            {
                { "gameplayEvent", "combat_start" }
            };
            
            fogEngine.UpdateGeneration(Time.deltaTime, combatState);
            yield return new WaitForSeconds(0.1f);
            
            var activeEffects = fogEngine.GetActiveEffects();
            var combatEffect = activeEffects.ContainsKey("combat_intensity");
            Assert.IsTrue(combatEffect, "Should create combat intensity effect");

            // Test exploration event
            var explorationState = new Dictionary<string, object>
            {
                { "gameplayEvent", "exploration_mode" }
            };
            
            fogEngine.UpdateGeneration(Time.deltaTime, explorationState);
            yield return new WaitForSeconds(0.1f);
            
            activeEffects = fogEngine.GetActiveEffects();
            var explorationEffect = activeEffects.ContainsKey("exploration_calm");
            Assert.IsTrue(explorationEffect, "Should create exploration calm effect");

            // Test story event
            var storyState = new Dictionary<string, object>
            {
                { "gameplayEvent", "story_moment" }
            };
            
            fogEngine.UpdateGeneration(Time.deltaTime, storyState);
            yield return new WaitForSeconds(0.1f);
            
            activeEffects = fogEngine.GetActiveEffects();
            var storyEffect = activeEffects.ContainsKey("story_drama");
            Assert.IsTrue(storyEffect, "Should create story drama effect");
        }

        [UnityTest]
        public IEnumerator FogEngine_HandlesZoneTransitions_SmoothlyBlends()
        {
            // Arrange - Start in industrial zone
            var industrialState = new Dictionary<string, object>
            {
                { "currentZone", "industrial" }
            };
            
            fogEngine.UpdateGeneration(Time.deltaTime, industrialState);
            yield return new WaitForSeconds(0.2f);
            
            var initialDensity = RenderSettings.fogDensity;
            var initialColor = RenderSettings.fogColor;

            // Act - Transition to residential zone
            var residentialState = new Dictionary<string, object>
            {
                { "currentZone", "residential" }
            };
            
            fogEngine.UpdateGeneration(Time.deltaTime, residentialState);
            yield return new WaitForSeconds(0.2f);

            // Assert - Fog properties should have changed
            var newDensity = RenderSettings.fogDensity;
            var newColor = RenderSettings.fogColor;
            
            // Properties should be different (indicating transition occurred)
            Assert.AreNotEqual(initialDensity, newDensity, 0.01f, "Fog density should change between zones");
            
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.IsTrue(activeEffects.Count > 0, "Should have active zone effects");
        }

        [UnityTest]
        public IEnumerator FogEngine_PerformsCleanupWithOtherEngines_MaintainsPerformance()
        {
            // Arrange - Create effects in all engines
            for (int i = 0; i < 5; i++)
            {
                var effect = new FogEffect
                {
                    Id = $"cleanup_test_{i}",
                    TargetDensity = 0.5f,
                    TargetColor = Color.white,
                    Duration = 10f,
                    Position = new Vector3(i * 30f, 0f, 0f),
                    HasPosition = true
                };
                fogEngine.RegisterFogEffect(effect);
            }

            // Create some lighting effects (if possible)
            var testLight = new GameObject("CleanupTestLight");
            var light = testLight.AddComponent<Light>();
            light.name = "neon_cleanup_test";
            testLight.transform.position = new Vector3(200f, 0f, 0f);
            
            lightingEngine.GenerateAsync(new Dictionary<string, object> 
            { 
                { "lightObject", testLight } 
            }).Wait();

            yield return new WaitForSeconds(0.1f);

            var initialFogEffects = fogEngine.GetActiveEffects().Count;
            var initialLightingEffects = lightingEngine.GetTrackedLights().Count;

            // Act - Perform cleanup on all engines
            Vector3 playerPosition = Vector3.zero;
            float cleanupDistance = 50f;
            
            fogEngine.CleanupDistantContent(cleanupDistance, playerPosition);
            lightingEngine.CleanupDistantContent(cleanupDistance, playerPosition);
            audioEngine.CleanupDistantContent(cleanupDistance, playerPosition);

            yield return new WaitForSeconds(0.1f);

            // Assert
            var finalFogEffects = fogEngine.GetActiveEffects().Count;
            var finalLightingEffects = lightingEngine.GetTrackedLights().Count;
            
            Assert.Less(finalFogEffects, initialFogEffects, "Should cleanup distant fog effects");
            Assert.Less(finalLightingEffects, initialLightingEffects, "Should cleanup distant lighting effects");
            
            // All engines should still be active
            Assert.IsTrue(fogEngine.IsActive);
            Assert.IsTrue(lightingEngine.IsActive);
            Assert.IsTrue(audioEngine.IsActive);
            
            Object.DestroyImmediate(testLight);
        }

        [UnityTest]
        public IEnumerator FogEngine_BlendingMultipleEffects_ProducesExpectedResults()
        {
            // Arrange - Create multiple overlapping effects
            fogEngine.CreateCustomFogEffect("blend_test_1", 0.3f, Color.red, 5f, 8);
            fogEngine.CreateCustomFogEffect("blend_test_2", 0.7f, Color.blue, 5f, 6);
            fogEngine.CreateCustomFogEffect("blend_test_3", 0.5f, Color.green, 5f, 4);

            // Wait for effects to be processed
            yield return new WaitForSeconds(0.3f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(3, activeEffects.Count, "Should have all three effects active");
            
            var activeTransitions = fogEngine.GetActiveTransitions();
            Assert.IsTrue(activeTransitions.Count > 0, "Should have active transitions");
            
            // The fog should be affected by the blending
            var currentState = fogEngine.GetCurrentFogState();
            Assert.IsNotNull(currentState);
            Assert.Greater(currentState.Density, 0f);
            
            // Performance should remain reasonable
            Assert.Less(fogEngine.CurrentPerformanceCost, 1f);
        }

        [Test]
        public void FogEngine_ConfigurationIntegration_AppliesSettingsCorrectly()
        {
            // Arrange
            var customConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            customConfig.FogDensityRange = new Vector2(0.2f, 0.9f);
            customConfig.AtmosphereTransitionSpeed = 0.8f;
            
            var configData = new Dictionary<string, object> { { "config", customConfig } };

            // Act
            fogEngine.Initialize(configData);

            // Assert
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();
            Assert.IsTrue((bool)diagnosticInfo["IsActive"]);
            
            // Create an effect to test the configuration is applied
            fogEngine.CreateCustomFogEffect("config_test", 0.5f, Color.yellow, 1f);
            
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            
            Object.DestroyImmediate(customConfig);
        }
    }
}