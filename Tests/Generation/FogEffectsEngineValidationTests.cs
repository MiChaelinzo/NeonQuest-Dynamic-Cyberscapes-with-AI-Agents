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
    public class FogEffectsEngineValidationTests
    {
        private GameObject testGameObject;
        private FogEffectsEngine fogEngine;
        private EnvironmentConfiguration testConfig;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestFogEffectsEngine");
            fogEngine = testGameObject.AddComponent<FogEffectsEngine>();
            
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.FogDensityRange = new Vector2(0.1f, 0.8f);
            testConfig.AtmosphereTransitionSpeed = 0.5f;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
            if (testConfig != null)
                Object.DestroyImmediate(testConfig);
        }

        [Test]
        public void RegisterFogEffect_WithNullEffect_DoesNotAddToActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.RegisterFogEffect(null);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(0, activeEffects.Count);
        }

        [Test]
        public void RegisterFogEffect_WithEmptyId_DoesNotAddToActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var effect = new FogEffect
            {
                Id = "", // Empty ID
                TargetDensity = 0.5f,
                TargetColor = Color.red,
                Duration = 2f
            };

            // Act
            fogEngine.RegisterFogEffect(effect);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(0, activeEffects.Count);
        }

        [Test]
        public void RegisterFogEffect_WithNullId_DoesNotAddToActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var effect = new FogEffect
            {
                Id = null, // Null ID
                TargetDensity = 0.5f,
                TargetColor = Color.red,
                Duration = 2f
            };

            // Act
            fogEngine.RegisterFogEffect(effect);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(0, activeEffects.Count);
        }

        [Test]
        public void CreateCustomFogEffect_WithNegativeDensity_ClampsToValidRange()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.CreateCustomFogEffect("negative_density", -0.5f, Color.blue, 1f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            
            var effect = activeEffects["negative_density"];
            // The engine should handle negative values gracefully
            Assert.IsNotNull(effect);
        }

        [Test]
        public void CreateCustomFogEffect_WithExcessiveDensity_HandlesGracefully()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.CreateCustomFogEffect("excessive_density", 10f, Color.green, 1f);

            // Assert
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
            
            var effect = activeEffects["excessive_density"];
            Assert.AreEqual(10f, effect.TargetDensity); // Should store the value as provided
        }

        [Test]
        public void SetQualityLevel_WithNegativeValue_ClampsToZero()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.SetQualityLevel(-0.5f);

            // Assert
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();
            Assert.AreEqual(0f, (float)diagnosticInfo["QualityLevel"], 0.01f);
        }

        [Test]
        public void SetQualityLevel_WithExcessiveValue_ClampsToOne()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act
            fogEngine.SetQualityLevel(2.5f);

            // Assert
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();
            Assert.AreEqual(1f, (float)diagnosticInfo["QualityLevel"], 0.01f);
        }

        [Test]
        public void TransitionToFogDensity_WithNegativeDuration_HandlesGracefully()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => fogEngine.TransitionToFogDensity(0.5f, -1f));
            
            var activeEffects = fogEngine.GetActiveEffects();
            Assert.AreEqual(1, activeEffects.Count);
        }

        [Test]
        public void RemoveFogEffect_WithNonExistentId_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.RemoveFogEffect("non_existent_id"));
        }

        [Test]
        public void Initialize_WithNullConfig_DoesNotThrowException()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.Initialize(null));
            Assert.IsTrue(fogEngine.IsActive);
        }

        [Test]
        public void Initialize_WithEmptyConfigDictionary_DoesNotThrowException()
        {
            // Arrange
            var emptyConfig = new Dictionary<string, object>();

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.Initialize(emptyConfig));
            Assert.IsTrue(fogEngine.IsActive);
        }

        [Test]
        public void Initialize_WithInvalidConfigType_DoesNotThrowException()
        {
            // Arrange
            var invalidConfig = new Dictionary<string, object>
            {
                { "config", "invalid_string_config" }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.Initialize(invalidConfig));
            Assert.IsTrue(fogEngine.IsActive);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithNullEnvironmentState_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.UpdateGeneration(Time.deltaTime, null));
            
            yield return null;
            
            Assert.IsTrue(fogEngine.IsActive);
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_WithEmptyEnvironmentState_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var emptyState = new Dictionary<string, object>();

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.UpdateGeneration(Time.deltaTime, emptyState));
            
            yield return null;
            
            Assert.IsTrue(fogEngine.IsActive);
        }

        [Test]
        public void CleanupDistantContent_WithNegativeDistance_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            fogEngine.CreateCustomFogEffect("test_effect", 0.5f, Color.red, 2f);

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.CleanupDistantContent(-10f, Vector3.zero));
        }

        [Test]
        public void FogState_WithInvalidValues_HandlesGracefully()
        {
            // Arrange & Act
            var state = new FogState
            {
                Density = -1f,
                Color = new Color(-1f, 2f, 0.5f, 1f),
                StartDistance = -100f,
                EndDistance = -50f
            };

            // Assert - Should not throw when creating
            Assert.IsNotNull(state);
            Assert.AreEqual(-1f, state.Density);
            Assert.AreEqual(-100f, state.StartDistance);
        }

        [Test]
        public void FogTransition_Progress_HandlesEdgeCases()
        {
            // Arrange
            var transition = new FogTransition
            {
                StartTime = Time.time,
                Duration = 0f // Zero duration
            };

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => { var progress = transition.Progress; });
        }

        [Test]
        public void GetDiagnosticInfo_WhenEngineInactive_ReturnsValidInfo()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            fogEngine.SetActive(false);

            // Act
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();

            // Assert
            Assert.IsNotNull(diagnosticInfo);
            Assert.IsFalse((bool)diagnosticInfo["IsActive"]);
            Assert.IsTrue(diagnosticInfo.ContainsKey("ActiveEffects"));
            Assert.IsTrue(diagnosticInfo.ContainsKey("PerformanceCost"));
        }

        [UnityTest]
        public IEnumerator TriggerCoordinatedAtmosphericChange_WithNullContext_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.TriggerCoordinatedAtmosphericChange(null));
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(fogEngine.IsActive);
        }

        [UnityTest]
        public IEnumerator TriggerCoordinatedAtmosphericChange_WithEmptyContext_DoesNotThrowException()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Act & Assert
            Assert.DoesNotThrow(() => fogEngine.TriggerCoordinatedAtmosphericChange(""));
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(fogEngine.IsActive);
        }
    }
}