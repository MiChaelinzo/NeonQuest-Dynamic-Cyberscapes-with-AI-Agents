using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class FogEffectsEngineTests
    {
        private GameObject testGameObject;
        private FogEffectsEngine fogEngine;
        private EnvironmentConfiguration testConfig;
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
            
            // Create test game object with FogEffectsEngine
            testGameObject = new GameObject("TestFogEffectsEngine");
            fogEngine = testGameObject.AddComponent<FogEffectsEngine>();
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.FogDensityRange = new Vector2(0.1f, 0.8f);
            testConfig.AtmosphereTransitionSpeed = 0.5f;
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
            fogEngine.Initialize(configData);

            // Assert
            Assert.IsTrue(fogEngine.IsActive);
            Assert.AreEqual(0f, fogEngine.CurrentPerformanceCost, 0.01f);
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode);
        }

        [Test]
        public void SetQualityLevel_WithValidValue_UpdatesParameters()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object>());

            // Act
            fogEngine.SetQualityLevel(0.5f);

            // Assert
            Assert.IsTrue(fogEngine.IsActive);
        }

        [Test]
        public void SetActive_WithFalse_DeactivatesEngine()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object>());
            Assert.IsTrue(fogEngine.IsActive);

            // Act
            fogEngine.SetActive(false);

            // Assert
            Assert.IsFalse(fogEngine.IsActive);
        }

        [Test]
        public void CreateCustomFogEffect_WithValidParameters_CreatesEffect()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.CreateCustomFogEffect("test_effect", 0.5f, Color.red, 2f, 7);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            
            var effect = activeEffects.Values.First();
            Assert.AreEqual("test_effect", effect.Id);
            Assert.AreEqual(0.5f, effect.TargetDensity, 0.01f);
            Assert.AreEqual(Color.red, effect.TargetColor);
            Assert.AreEqual(2f, effect.Duration, 0.01f);
            Assert.AreEqual(7, effect.Priority);
            Assert.AreEqual(FogEffectType.Custom, effect.EffectType);
        }

        [Test]
        public void RegisterFogEffect_WithValidEffect_AddsToActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var effect = new FogEffect
            {
                Id = "test_register",
                TargetDensity = 0.6f,
                TargetColor = Color.blue,
                Duration = 3f,
                Priority = 5,
                EffectType = FogEffectType.Zone
            };

            // Act
            fogEngine.RegisterFogEffect(effect);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            Assert.IsTrue(activeEffects.ContainsKey("test_register"));
        }

        [Test]
        public void RemoveFogEffect_WithExistingEffect_RemovesFromActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            fogEngine.CreateCustomFogEffect("test_remove", 0.4f, Color.green, 1f);
            
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);

            // Act
            fogEngine.RemoveFogEffect("test_remove");

            // Assert
            activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(0, activeEffects.Count);
        }

        [Test]
        public void TransitionToFogDensity_WithValidParameters_CreatesTransition()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.TransitionToFogDensity(0.7f, 2f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            
            var effect = activeEffects.Values.First();
            Assert.AreEqual("density_transition", effect.Id);
            Assert.AreEqual(0.7f, effect.TargetDensity, 0.01f);
            Assert.AreEqual(FogEffectType.Transition, effect.EffectType);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithZoneChange_ProcessesZoneEffect()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var environmentState = new Dictionary<string, object>
            {
                { "currentZone", "industrial" }
            };

            // Act
            fogEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for processing
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.IsTrue(activeEffects.Count > 0);
            
            var zoneEffect = activeEffects.Values.FirstOrDefault(e => e.Id.Contains("zone_industrial"));
            Assert.IsNotNull(zoneEffect);
            Assert.AreEqual(FogEffectType.Zone, zoneEffect.EffectType);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithGameplayEvent_ProcessesGameplayEffect()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var environmentState = new Dictionary<string, object>
            {
                { "gameplayEvent", "combat_start" }
            };

            // Act
            fogEngine.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Wait for processing
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.IsTrue(activeEffects.Count > 0);
            
            var combatEffect = activeEffects.Values.FirstOrDefault(e => e.Id == "combat_intensity");
            Assert.IsNotNull(combatEffect);
            Assert.AreEqual(FogEffectType.Gameplay, combatEffect.EffectType);
        }

        [Test]
        public void CleanupDistantContent_WithDistantEffects_RemovesFromActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var distantEffect = new FogEffect
            {
                Id = "distant_effect",
                TargetDensity = 0.5f,
                TargetColor = Color.white,
                Duration = 5f,
                Priority = 3,
                Position = new Vector3(200f, 0f, 0f),
                HasPosition = true
            };
            
            fogEngine.RegisterFogEffect(distantEffect);
            Assert.AreEqual(1, fogEngine.GetActiveEffects().Count);

            // Act
            fogEngine.CleanupDistantContent(100f, Vector3.zero);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(0, activeEffects.Count);
        }

        [UnityTest]
        public IEnumerator FogTransition_CompletesWithinExpectedTime()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            float originalDensity = RenderSettings.fogDensity;
            
            // Act - Create a short transition
            fogEngine.CreateCustomFogEffect("quick_transition", 0.9f, Color.yellow, 0.5f);
            
            // Wait for transition to complete
            yield return new WaitForSeconds(0.7f);

            // Assert
            var activeTransitions = fogEngine.GetActiveTransitions();
            Assert.AreEqual(0, activeTransitions.Count, "Transition should be complete");
            
            // Fog density should have changed
            Assert.AreNotEqual(originalDensity, RenderSettings.fogDensity, 0.01f);
        }

        [Test]
        public void GetCurrentFogState_ReturnsValidState()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            var currentState = fogEngine.GetCurrentFogState();

            // Assert
            Assert.IsNotNull(currentState);
            Assert.Greater(currentState.Density, 0f);
            Assert.AreNotEqual(Color.clear, currentState.Color);
        }

        [Test]
        public void PerformanceCost_UpdatesBasedOnActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            float initialCost = fogEngine.CurrentPerformanceCost;

            // Act - Add multiple effects
            fogEngine.CreateCustomFogEffect("effect1", 0.3f, Color.red, 2f);
            fogEngine.CreateCustomFogEffect("effect2", 0.6f, Color.blue, 3f);

            // Assert
            Assert.Greater(fogEngine.CurrentPerformanceCost, initialCost);
        }

        [Test]
        public void GetDiagnosticInfo_ReturnsCompleteInformation()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            fogEngine.CreateCustomFogEffect("diagnostic_test", 0.4f, Color.cyan, 1f);

            // Act
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();

            // Assert
            Assert.IsNotNull(diagnosticInfo);
            Assert.IsTrue(diagnosticInfo.ContainsKey("ActiveEffects"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("ActiveTransitions"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("CurrentDensity"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("CurrentColor"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("PerformanceCost"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("QualityLevel"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("IsActive"));
            
            Assert.AreEqual(1, (int)diagnosticInfo["ActiveEffects"]);
            Assert.IsTrue((bool)diagnosticInfo["IsActive"]);
        }

        [UnityTest]
        public IEnumerator TriggerCoordinatedAtmosphericChange_WithValidContext_ExecutesCoordination()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.TriggerCoordinatedAtmosphericChange("industrial");
            
            // Wait for coordination to execute
            yield return new WaitForSeconds(1.5f);

            // Assert - This test mainly verifies the method doesn't throw exceptions
            // Actual coordination would require LightingEngine and AudioEngine to be present
            Assert.IsTrue(fogEngine.IsActive);
        }

        [Test]
        public void FogState_CopyConstructor_CreatesAccurateCopy()
        {
            // Arrange
            var originalState = new FogState
            {
                Density = 0.5f,
                Color = Color.magenta,
                StartDistance = 10f,
                EndDistance = 200f,
                Mode = FogMode.Linear
            };

            // Act
            var copiedState = new FogState(originalState);

            // Assert
            Assert.AreEqual(originalState.Density, copiedState.Density);
            Assert.AreEqual(originalState.Color, copiedState.Color);
            Assert.AreEqual(originalState.StartDistance, copiedState.StartDistance);
            Assert.AreEqual(originalState.EndDistance, copiedState.EndDistance);
            Assert.AreEqual(originalState.Mode, copiedState.Mode);
        }

        [Test]
        public void FogEffect_DefaultConstructor_SetsReasonableDefaults()
        {
            // Act
            var effect = new FogEffect();

            // Assert
            Assert.AreEqual("", effect.Id);
            Assert.AreEqual(0.1f, effect.TargetDensity);
            Assert.AreEqual(Color.gray, effect.TargetColor);
            Assert.AreEqual(1f, effect.Duration);
            Assert.AreEqual(5, effect.Priority);
            Assert.AreEqual(FogEffectType.Generic, effect.EffectType);
            Assert.AreEqual(Vector3.zero, effect.Position);
            Assert.IsFalse(effect.HasPosition);
            Assert.AreEqual(0f, effect.ElapsedTime);
        }
    }
}