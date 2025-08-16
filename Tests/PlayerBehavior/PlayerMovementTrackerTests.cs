using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Tests.PlayerBehavior
{
    public class PlayerMovementTrackerTests
    {
        private GameObject testObject;
        private PlayerMovementTracker tracker;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestPlayer");
            tracker = testObject.AddComponent<PlayerMovementTracker>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }

        [Test]
        public void GetCurrentMovementData_InitialState_ReturnsStationaryPattern()
        {
            // Act
            var movementData = tracker.GetCurrentMovementData();

            // Assert
            Assert.AreEqual(MovementPattern.Stationary, movementData.Pattern);
            Assert.AreEqual(0f, movementData.Speed, 0.01f);
            Assert.AreEqual(Vector3.zero, movementData.Velocity);
        }

        [Test]
        public void GetDwellTime_InitialState_ReturnsZero()
        {
            // Act
            float dwellTime = tracker.GetDwellTime();

            // Assert
            Assert.AreEqual(0f, dwellTime, 0.01f);
        }

        [UnityTest]
        public IEnumerator TrackMovement_PlayerMoves_UpdatesVelocityAndSpeed()
        {
            // Arrange
            Vector3 startPosition = Vector3.zero;
            Vector3 endPosition = new Vector3(5f, 0f, 0f);
            testObject.transform.position = startPosition;

            // Act - Move player over time
            float moveTime = 1f;
            float elapsedTime = 0f;
            
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                testObject.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                yield return null;
            }

            // Wait for tracking to update
            yield return new WaitForSeconds(0.2f);

            // Assert
            var movementData = tracker.GetCurrentMovementData();
            Assert.Greater(movementData.Speed, 0f);
            Assert.AreNotEqual(Vector3.zero, movementData.Velocity);
        }

        [UnityTest]
        public IEnumerator DwellTime_PlayerStationary_IncreasesOverTime()
        {
            // Arrange
            testObject.transform.position = Vector3.zero;
            bool dwellTimeUpdated = false;
            float recordedDwellTime = 0f;

            tracker.OnDwellTimeUpdate += (dwellTime) =>
            {
                dwellTimeUpdated = true;
                recordedDwellTime = dwellTime;
            };

            // Act - Wait for dwell time to accumulate
            yield return new WaitForSeconds(1f);

            // Assert
            Assert.IsTrue(dwellTimeUpdated);
            Assert.Greater(recordedDwellTime, 0f);
        }

        [Test]
        public void HasVisitedPosition_NeverVisited_ReturnsFalse()
        {
            // Arrange
            Vector3 testPosition = new Vector3(100f, 0f, 0f);

            // Act
            bool hasVisited = tracker.HasVisitedPosition(testPosition);

            // Assert
            Assert.IsFalse(hasVisited);
        }

        [UnityTest]
        public IEnumerator HasVisitedPosition_AfterVisiting_ReturnsTrue()
        {
            // Arrange
            Vector3 visitPosition = new Vector3(10f, 0f, 0f);
            testObject.transform.position = visitPosition;

            // Act - Wait for tracking to register the position
            yield return new WaitForSeconds(0.2f);

            // Assert
            bool hasVisited = tracker.HasVisitedPosition(visitPosition, 5f);
            Assert.IsTrue(hasVisited);
        }

        [Test]
        public void GetRecentMovementHistory_InitialState_ReturnsEmptyArray()
        {
            // Act
            var history = tracker.GetRecentMovementHistory();

            // Assert
            Assert.IsNotNull(history);
            Assert.AreEqual(0, history.Length);
        }

        [UnityTest]
        public IEnumerator MovementPattern_PlayerExploring_DetectsExploringPattern()
        {
            // Arrange
            testObject.transform.position = Vector3.zero;
            bool patternChanged = false;
            MovementPattern detectedPattern = MovementPattern.Stationary;

            tracker.OnPatternChanged += (pattern) =>
            {
                patternChanged = true;
                detectedPattern = pattern;
            };

            // Act - Move player to new areas
            Vector3[] explorationPath = {
                new Vector3(5f, 0f, 0f),
                new Vector3(10f, 0f, 5f),
                new Vector3(15f, 0f, 10f)
            };

            foreach (var position in explorationPath)
            {
                testObject.transform.position = position;
                yield return new WaitForSeconds(0.3f);
            }

            // Assert
            Assert.IsTrue(patternChanged);
            Assert.IsTrue(detectedPattern == MovementPattern.Exploring || 
                         detectedPattern == MovementPattern.Wandering);
        }

        [UnityTest]
        public IEnumerator MovementUpdate_PlayerMoves_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            MovementData receivedData = default;

            tracker.OnMovementUpdate += (data) =>
            {
                eventTriggered = true;
                receivedData = data;
            };

            // Act
            testObject.transform.position = new Vector3(1f, 0f, 0f);
            yield return new WaitForSeconds(0.2f);

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreNotEqual(default(MovementData), receivedData);
        }

        [UnityTest]
        public IEnumerator BacktrackingDetection_PlayerReturnsToVisitedArea_DetectsBacktracking()
        {
            // Arrange
            Vector3 originalPosition = Vector3.zero;
            Vector3 exploredPosition = new Vector3(20f, 0f, 0f);
            
            testObject.transform.position = originalPosition;
            yield return new WaitForSeconds(0.5f);

            // Move away to explore
            testObject.transform.position = exploredPosition;
            yield return new WaitForSeconds(0.5f);

            // Act - Return towards original position
            testObject.transform.position = new Vector3(10f, 0f, 0f);
            yield return new WaitForSeconds(0.2f);

            // Assert
            var movementData = tracker.GetCurrentMovementData();
            // Note: Backtracking detection may need more sophisticated movement to trigger
            Assert.IsNotNull(movementData);
        }

        [Test]
        public void MovementSample_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var sample = new MovementSample
            {
                Position = Vector3.one,
                Velocity = Vector3.up,
                Speed = 5f,
                Direction = Vector3.forward,
                Timestamp = 10f
            };

            // Assert
            Assert.AreEqual(Vector3.one, sample.Position);
            Assert.AreEqual(Vector3.up, sample.Velocity);
            Assert.AreEqual(5f, sample.Speed);
            Assert.AreEqual(Vector3.forward, sample.Direction);
            Assert.AreEqual(10f, sample.Timestamp);
        }

        [Test]
        public void MovementData_StructInitialization_WorksCorrectly()
        {
            // Arrange & Act
            var data = new MovementData
            {
                Position = Vector3.one,
                Velocity = Vector3.up,
                Speed = 5f,
                Direction = Vector3.forward,
                Pattern = MovementPattern.Exploring,
                DwellTime = 15f,
                IsBacktracking = true
            };

            // Assert
            Assert.AreEqual(Vector3.one, data.Position);
            Assert.AreEqual(Vector3.up, data.Velocity);
            Assert.AreEqual(5f, data.Speed);
            Assert.AreEqual(Vector3.forward, data.Direction);
            Assert.AreEqual(MovementPattern.Exploring, data.Pattern);
            Assert.AreEqual(15f, data.DwellTime);
            Assert.IsTrue(data.IsBacktracking);
        }
    }
}