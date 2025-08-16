using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Tests.PlayerBehavior
{
    public class PlayerBehaviorAnalyzerTests
    {
        private GameObject testObject;
        private PlayerBehaviorAnalyzer analyzer;
        private PlayerMovementTracker tracker;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestPlayer");
            tracker = testObject.AddComponent<PlayerMovementTracker>();
            analyzer = testObject.AddComponent<PlayerBehaviorAnalyzer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }

        [Test]
        public void GetCurrentBehaviorContext_InitialState_ReturnsValidContext()
        {
            // Act
            var context = analyzer.GetCurrentBehaviorContext();

            // Assert
            Assert.IsNotNull(context.EnvironmentalFactors);
            Assert.GreaterOrEqual(context.AnalysisTimestamp, 0f);
        }

        [Test]
        public void GetPredictedIntention_InitialState_ReturnsValidIntention()
        {
            // Act
            var intention = analyzer.GetPredictedIntention();

            // Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), intention));
        }

        [Test]
        public void GetIntentionConfidence_InitialState_ReturnsValidConfidence()
        {
            // Act
            float confidence = analyzer.GetIntentionConfidence();

            // Assert
            Assert.GreaterOrEqual(confidence, 0f);
            Assert.LessOrEqual(confidence, 1f);
        }

        [Test]
        public void GetEnvironmentalFactors_InitialState_ReturnsAllFactors()
        {
            // Act
            var factors = analyzer.GetEnvironmentalFactors();

            // Assert
            Assert.IsNotNull(factors);
            Assert.IsTrue(factors.ContainsKey("exploration_tendency"));
            Assert.IsTrue(factors.ContainsKey("backtracking_frequency"));
            Assert.IsTrue(factors.ContainsKey("dwell_preference"));
            Assert.IsTrue(factors.ContainsKey("movement_consistency"));
            Assert.IsTrue(factors.ContainsKey("spatial_awareness"));
        }

        [Test]
        public void GetBehaviorHistory_InitialState_ReturnsEmptyHistory()
        {
            // Act
            var history = analyzer.GetBehaviorHistory();

            // Assert
            Assert.IsNotNull(history);
            Assert.AreEqual(0, history.Length);
        }

        [UnityTest]
        public IEnumerator IntentionPrediction_PlayerExploring_PredictsExploringIntention()
        {
            // Arrange
            bool intentionPredicted = false;
            PlayerIntention predictedIntention = PlayerIntention.Resting;
            float predictedConfidence = 0f;

            analyzer.OnIntentionPredicted += (intention, confidence) =>
            {
                intentionPredicted = true;
                predictedIntention = intention;
                predictedConfidence = confidence;
            };

            // Act - Simulate exploration movement
            Vector3[] explorationPath = {
                new Vector3(0f, 0f, 0f),
                new Vector3(5f, 0f, 0f),
                new Vector3(10f, 0f, 5f),
                new Vector3(15f, 0f, 10f),
                new Vector3(20f, 0f, 15f)
            };

            foreach (var position in explorationPath)
            {
                testObject.transform.position = position;
                yield return new WaitForSeconds(0.6f); // Wait for analysis interval
            }

            // Assert
            if (intentionPredicted)
            {
                Assert.IsTrue(predictedIntention == PlayerIntention.Exploring || 
                             predictedIntention == PlayerIntention.Wandering);
                Assert.Greater(predictedConfidence, 0f);
            }
        }

        [UnityTest]
        public IEnumerator ContextUpdate_PlayerMoves_TriggersContextUpdate()
        {
            // Arrange
            bool contextUpdated = false;
            BehaviorContext receivedContext = default;

            analyzer.OnContextUpdated += (context) =>
            {
                contextUpdated = true;
                receivedContext = context;
            };

            // Act
            testObject.transform.position = new Vector3(5f, 0f, 0f);
            yield return new WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(contextUpdated);
            Assert.IsNotNull(receivedContext.EnvironmentalFactors);
        }

        [UnityTest]
        public IEnumerator EnvironmentalFactorsUpdate_PlayerMoves_TriggersFactorsUpdate()
        {
            // Arrange
            bool factorsUpdated = false;
            Dictionary<string, float> receivedFactors = null;

            analyzer.OnEnvironmentalFactorsUpdated += (factors) =>
            {
                factorsUpdated = true;
                receivedFactors = factors;
            };

            // Act
            testObject.transform.position = new Vector3(3f, 0f, 0f);
            yield return new WaitForSeconds(0.6f);

            // Assert
            Assert.IsTrue(factorsUpdated);
            Assert.IsNotNull(receivedFactors);
            Assert.Greater(receivedFactors.Count, 0);
        }

        [UnityTest]
        public IEnumerator BehaviorHistory_AfterMovement_ContainsSnapshots()
        {
            // Arrange
            testObject.transform.position = Vector3.zero;

            // Act - Move and wait for snapshots to accumulate
            for (int i = 0; i < 5; i++)
            {
                testObject.transform.position = new Vector3(i * 2f, 0f, 0f);
                yield return new WaitForSeconds(0.2f);
            }

            // Assert
            var history = analyzer.GetBehaviorHistory();
            Assert.Greater(history.Length, 0);
        }

        [Test]
        public void BehaviorSnapshot_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var movementData = new MovementData
            {
                Position = Vector3.one,
                Speed = 5f,
                Pattern = MovementPattern.Exploring
            };

            var environmentalContext = new EnvironmentalContext
            {
                Position = Vector3.one,
                NearbyObjects = new List<string> { "test" },
                LightingLevel = 0.5f,
                AmbientSoundLevel = 0.3f
            };

            var gameplayContext = new GameplayContext
            {
                GameTime = 10f,
                SessionDuration = 100f,
                CurrentObjective = "explore"
            };

            var snapshot = new BehaviorSnapshot
            {
                Timestamp = 15f,
                MovementData = movementData,
                EnvironmentalContext = environmentalContext,
                GameplayContext = gameplayContext
            };

            // Assert
            Assert.AreEqual(15f, snapshot.Timestamp);
            Assert.AreEqual(MovementPattern.Exploring, snapshot.MovementData.Pattern);
            Assert.AreEqual(0.5f, snapshot.EnvironmentalContext.LightingLevel);
            Assert.AreEqual("explore", snapshot.GameplayContext.CurrentObjective);
        }

        [Test]
        public void BehaviorContext_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var factors = new Dictionary<string, float> { { "test", 0.5f } };
            var movementData = new MovementData { Speed = 3f };

            var context = new BehaviorContext
            {
                PredictedIntention = PlayerIntention.Searching,
                IntentionConfidence = 0.8f,
                EnvironmentalFactors = factors,
                CurrentMovementData = movementData,
                AnalysisTimestamp = 20f
            };

            // Assert
            Assert.AreEqual(PlayerIntention.Searching, context.PredictedIntention);
            Assert.AreEqual(0.8f, context.IntentionConfidence);
            Assert.AreEqual(factors, context.EnvironmentalFactors);
            Assert.AreEqual(3f, context.CurrentMovementData.Speed);
            Assert.AreEqual(20f, context.AnalysisTimestamp);
        }

        [Test]
        public void PlayerIntention_EnumValues_AreValid()
        {
            // Act & Assert
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), PlayerIntention.Exploring));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), PlayerIntention.Backtracking));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), PlayerIntention.Searching));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), PlayerIntention.Resting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerIntention), PlayerIntention.Wandering));
        }

        [UnityTest]
        public IEnumerator GetBehaviorHistory_WithCount_ReturnsLimitedHistory()
        {
            // Arrange - Generate some behavior history
            for (int i = 0; i < 10; i++)
            {
                testObject.transform.position = new Vector3(i, 0f, 0f);
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.5f); // Wait for analysis

            // Act
            var limitedHistory = analyzer.GetBehaviorHistory(5);
            var fullHistory = analyzer.GetBehaviorHistory();

            // Assert
            if (fullHistory.Length > 5)
            {
                Assert.AreEqual(5, limitedHistory.Length);
                Assert.LessOrEqual(limitedHistory.Length, fullHistory.Length);
            }
        }

        [Test]
        public void EnvironmentalContext_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var nearbyObjects = new List<string> { "corridor", "neon_sign" };
            var context = new EnvironmentalContext
            {
                Position = Vector3.up,
                NearbyObjects = nearbyObjects,
                LightingLevel = 0.7f,
                AmbientSoundLevel = 0.4f
            };

            // Assert
            Assert.AreEqual(Vector3.up, context.Position);
            Assert.AreEqual(nearbyObjects, context.NearbyObjects);
            Assert.AreEqual(0.7f, context.LightingLevel);
            Assert.AreEqual(0.4f, context.AmbientSoundLevel);
        }

        [Test]
        public void GameplayContext_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var context = new GameplayContext
            {
                GameTime = 150f,
                SessionDuration = 300f,
                CurrentObjective = "search"
            };

            // Assert
            Assert.AreEqual(150f, context.GameTime);
            Assert.AreEqual(300f, context.SessionDuration);
            Assert.AreEqual("search", context.CurrentObjective);
        }
    }
}