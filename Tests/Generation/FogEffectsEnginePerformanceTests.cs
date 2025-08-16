using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class FogEffectsEnginePerformanceTests
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
        public void CreateMultipleFogEffects_WithinPerformanceLimits()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var stopwatch = Stopwatch.StartNew();

            // Act - Create multiple effects
            for (int i = 0; i < 10; i++)
            {
                fogEngine.CreateCustomFogEffect($"perf_test_{i}", 0.5f, Color.red, 1f);
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, "Creating 10 fog effects should take less than 50ms");
            Assert.AreEqual(10, fogEngine.GetActiveEffects().Count);
        }

        [Test]
        public void PerformanceCost_IncreasesWithActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            float initialCost = fogEngine.CurrentPerformanceCost;

            // Act - Add effects progressively
            fogEngine.CreateCustomFogEffect("effect1", 0.3f, Color.red, 2f);
            float costWith1Effect = fogEngine.CurrentPerformanceCost;

            fogEngine.CreateCustomFogEffect("effect2", 0.6f, Color.blue, 2f);
            float costWith2Effects = fogEngine.CurrentPerformanceCost;

            fogEngine.CreateCustomFogEffect("effect3", 0.9f, Color.green, 2f);
            float costWith3Effects = fogEngine.CurrentPerformanceCost;

            // Assert
            Assert.Greater(costWith1Effect, initialCost);
            Assert.Greater(costWith2Effects, costWith1Effect);
            Assert.Greater(costWith3Effects, costWith2Effects);
            Assert.LessOrEqual(costWith3Effects, 1f, "Performance cost should not exceed 1.0");
        }

        [Test]
        public void QualityLevel_AffectsMaxActiveEffects()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });

            // Test with high quality
            fogEngine.SetQualityLevel(1f);
            int effectsCreatedHighQuality = 0;
            for (int i = 0; i < 15; i++)
            {
                fogEngine.CreateCustomFogEffect($"high_quality_{i}", 0.5f, Color.red, 1f);
                effectsCreatedHighQuality++;
            }
            int highQualityCount = fogEngine.GetActiveEffects().Count;

            // Clear effects
            for (int i = 0; i < 15; i++)
            {
                fogEngine.RemoveFogEffect($"high_quality_{i}");
            }

            // Test with low quality
            fogEngine.SetQualityLevel(0.3f);
            int effectsCreatedLowQuality = 0;
            for (int i = 0; i < 15; i++)
            {
                fogEngine.CreateCustomFogEffect($"low_quality_{i}", 0.5f, Color.blue, 1f);
                effectsCreatedLowQuality++;
            }
            int lowQualityCount = fogEngine.GetActiveEffects().Count;

            // Assert
            Assert.Greater(highQualityCount, lowQualityCount, 
                "High quality should allow more active effects than low quality");
        }

        [UnityTest]
        public IEnumerator UpdateGeneration_MaintainsPerformanceUnderLoad()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            // Create multiple effects
            for (int i = 0; i < 8; i++)
            {
                fogEngine.CreateCustomFogEffect($"load_test_{i}", 0.5f, Color.cyan, 5f);
            }

            var stopwatch = Stopwatch.StartNew();
            int updateCount = 0;

            // Act - Run updates for a period
            while (stopwatch.ElapsedMilliseconds < 1000) // 1 second
            {
                var environmentState = new Dictionary<string, object>
                {
                    { "currentZone", "industrial" },
                    { "gameplayEvent", "combat_start" }
                };
                
                fogEngine.UpdateGeneration(Time.deltaTime, environmentState);
                updateCount++;
                yield return null;
            }

            stopwatch.Stop();

            // Assert
            Assert.Greater(updateCount, 30, "Should complete at least 30 updates in 1 second");
            Assert.Less(fogEngine.CurrentPerformanceCost, 1f, "Performance cost should remain under 1.0");
        }

        [Test]
        public void CleanupDistantContent_ImprovesPerformance()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            // Create effects with positions
            for (int i = 0; i < 10; i++)
            {
                var effect = new FogEffect
                {
                    Id = $"distant_effect_{i}",
                    TargetDensity = 0.5f,
                    TargetColor = Color.white,
                    Duration = 10f,
                    Position = new Vector3(i * 50f, 0f, 0f), // Spread them out
                    HasPosition = true
                };
                fogEngine.RegisterFogEffect(effect);
            }

            float performanceCostBefore = fogEngine.CurrentPerformanceCost;
            int effectCountBefore = fogEngine.GetActiveEffects().Count;

            // Act - Cleanup distant content
            fogEngine.CleanupDistantContent(100f, Vector3.zero);

            float performanceCostAfter = fogEngine.CurrentPerformanceCost;
            int effectCountAfter = fogEngine.GetActiveEffects().Count;

            // Assert
            Assert.Less(effectCountAfter, effectCountBefore, "Should remove distant effects");
            Assert.LessOrEqual(performanceCostAfter, performanceCostBefore, "Performance cost should not increase");
        }

        [UnityTest]
        public IEnumerator FogTransitions_CompleteWithinReasonableTime()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var stopwatch = Stopwatch.StartNew();

            // Act - Create a transition with known duration
            fogEngine.CreateCustomFogEffect("timed_transition", 0.8f, Color.magenta, 1f);

            // Wait for transition to complete with some buffer
            yield return new WaitForSeconds(1.5f);

            stopwatch.Stop();

            // Assert
            var activeTransitions = fogEngine.GetActiveTransitions();
            Assert.AreEqual(0, activeTransitions.Count, "Transition should be complete");
            Assert.Less(stopwatch.ElapsedMilliseconds, 2000, "Transition should complete within reasonable time");
        }

        [Test]
        public void MemoryUsage_RemainsStableWithEffectChurn()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            // Measure initial memory (approximate)
            int initialEffectCount = fogEngine.GetActiveEffects().Count;

            // Act - Create and remove effects repeatedly
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Create effects
                for (int i = 0; i < 5; i++)
                {
                    fogEngine.CreateCustomFogEffect($"churn_{cycle}_{i}", 0.5f, Color.red, 0.5f);
                }

                // Remove effects
                for (int i = 0; i < 5; i++)
                {
                    fogEngine.RemoveFogEffect($"churn_{cycle}_{i}");
                }
            }

            // Assert
            int finalEffectCount = fogEngine.GetActiveEffects().Count;
            Assert.AreEqual(initialEffectCount, finalEffectCount, 
                "Effect count should return to initial state after churn");
        }

        [Test]
        public void GetDiagnosticInfo_ExecutesQuickly()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            
            // Create some effects to make diagnostic info more complex
            for (int i = 0; i < 5; i++)
            {
                fogEngine.CreateCustomFogEffect($"diagnostic_{i}", 0.5f, Color.yellow, 2f);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            var diagnosticInfo = fogEngine.GetDiagnosticInfo();

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(diagnosticInfo);
            Assert.Less(stopwatch.ElapsedMilliseconds, 10, "Getting diagnostic info should be very fast");
            Assert.IsTrue(diagnosticInfo.Count >= 7, "Should contain all expected diagnostic fields");
        }

        [UnityTest]
        public IEnumerator CoordinatedAtmosphericChanges_DoNotBlockExecution()
        {
            // Arrange
            fogEngine.Initialize(new Dictionary<string, object> { { "config", testConfig } });
            var stopwatch = Stopwatch.StartNew();

            // Act - Trigger multiple coordinated changes
            fogEngine.TriggerCoordinatedAtmosphericChange("industrial");
            fogEngine.TriggerCoordinatedAtmosphericChange("residential");
            fogEngine.TriggerCoordinatedAtmosphericChange("underground");

            // Continue with other operations immediately
            fogEngine.CreateCustomFogEffect("non_blocking_test", 0.6f, Color.green, 1f);

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, 
                "Coordinated changes should not block immediate execution");
            Assert.AreEqual(1, fogEngine.GetActiveEffects().Count);

            // Wait for coordination to complete
            yield return new WaitForSeconds(2f);
            
            Assert.IsTrue(fogEngine.IsActive);
        }
    }
}