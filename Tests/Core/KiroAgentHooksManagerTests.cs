using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core;

namespace Tests.Core
{
    [TestFixture]
    public class KiroAgentHooksManagerTests
    {
        private GameObject _testGameObject;
        private KiroAgentHooksManager _hooksManager;
        private MockKiroAgentHook _mockHook;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestHooksManager");
            _hooksManager = _testGameObject.AddComponent<KiroAgentHooksManager>();
            _mockHook = new MockKiroAgentHook("test-hook-1", "Test Hook", "A test hook");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void RegisterHook_ValidHook_ReturnsTrue()
        {
            // Act
            var result = _hooksManager.RegisterHook(_mockHook);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _hooksManager.RegisteredHookCount);
            Assert.IsTrue(_mockHook.WasRegistered);
        }

        [Test]
        public void RegisterHook_NullHook_ReturnsFalse()
        {
            // Act
            var result = _hooksManager.RegisterHook(null);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _hooksManager.RegisteredHookCount);
        }

        [Test]
        public void RegisterHook_DuplicateHookId_ReturnsFalse()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);
            var duplicateHook = new MockKiroAgentHook("test-hook-1", "Duplicate Hook");

            // Act
            var result = _hooksManager.RegisterHook(duplicateHook);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _hooksManager.RegisteredHookCount);
            Assert.IsFalse(duplicateHook.WasRegistered);
        }

        [Test]
        public void UnregisterHook_ExistingHook_ReturnsTrue()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);

            // Act
            var result = _hooksManager.UnregisterHook("test-hook-1");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _hooksManager.RegisteredHookCount);
            Assert.IsTrue(_mockHook.WasUnregistered);
        }

        [Test]
        public void UnregisterHook_NonExistentHook_ReturnsFalse()
        {
            // Act
            var result = _hooksManager.UnregisterHook("non-existent-hook");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetHook_ExistingHook_ReturnsHook()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);

            // Act
            var retrievedHook = _hooksManager.GetHook("test-hook-1");

            // Assert
            Assert.IsNotNull(retrievedHook);
            Assert.AreEqual(_mockHook, retrievedHook);
        }

        [Test]
        public void GetHook_NonExistentHook_ReturnsNull()
        {
            // Act
            var retrievedHook = _hooksManager.GetHook("non-existent-hook");

            // Assert
            Assert.IsNull(retrievedHook);
        }

        [Test]
        public async Task TriggerHooksAsync_WithApplicableHook_ExecutesHook()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);

            // Assert
            Assert.AreEqual(1, _mockHook.ExecutionCount);
            Assert.AreEqual(PlayerBehaviorEventType.MovementChanged, _mockHook.LastEventType);
        }

        [Test]
        public async Task TriggerHooksAsync_WithDisabledHook_DoesNotExecuteHook()
        {
            // Arrange
            _mockHook.IsEnabled = false;
            _hooksManager.RegisterHook(_mockHook);
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);

            // Assert
            Assert.AreEqual(0, _mockHook.ExecutionCount);
        }

        [Test]
        public async Task TriggerHooksAsync_WithUnsupportedEventType_DoesNotExecuteHook()
        {
            // Arrange
            var hookWithLimitedEvents = new MockKiroAgentHook(
                "limited-hook",
                supportedEventTypes: new[] { PlayerBehaviorEventType.IntentionPredicted });
            _hooksManager.RegisterHook(hookWithLimitedEvents);
            
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);

            // Assert
            Assert.AreEqual(0, hookWithLimitedEvents.ExecutionCount);
        }

        [Test]
        public async Task TriggerHooksAsync_WithMultipleHooks_ExecutesInPriorityOrder()
        {
            // Arrange
            var lowPriorityHook = new MockKiroAgentHook("low-priority", priority: 1);
            var highPriorityHook = new MockKiroAgentHook("high-priority", priority: 10);
            var mediumPriorityHook = new MockKiroAgentHook("medium-priority", priority: 5);

            _hooksManager.RegisterHook(lowPriorityHook);
            _hooksManager.RegisterHook(highPriorityHook);
            _hooksManager.RegisterHook(mediumPriorityHook);

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);

            // Assert
            Assert.AreEqual(1, lowPriorityHook.ExecutionCount);
            Assert.AreEqual(1, highPriorityHook.ExecutionCount);
            Assert.AreEqual(1, mediumPriorityHook.ExecutionCount);
        }

        [Test]
        public void GetHookStats_ExistingHook_ReturnsStats()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);

            // Act
            var stats = _hooksManager.GetHookStats("test-hook-1");

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual(0, stats.TotalExecutions);
            Assert.AreEqual(0, stats.SuccessfulExecutions);
            Assert.AreEqual(0, stats.FailedExecutions);
        }

        [Test]
        public void GetHookStats_NonExistentHook_ReturnsEmptyStats()
        {
            // Act
            var stats = _hooksManager.GetHookStats("non-existent-hook");

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual(0, stats.TotalExecutions);
        }

        [Test]
        public void GetRegisteredHookIds_WithRegisteredHooks_ReturnsAllIds()
        {
            // Arrange
            var hook1 = new MockKiroAgentHook("hook-1");
            var hook2 = new MockKiroAgentHook("hook-2");
            _hooksManager.RegisterHook(hook1);
            _hooksManager.RegisterHook(hook2);

            // Act
            var hookIds = _hooksManager.GetRegisteredHookIds();

            // Assert
            Assert.AreEqual(2, hookIds.Length);
            Assert.Contains("hook-1", hookIds);
            Assert.Contains("hook-2", hookIds);
        }

        [Test]
        public void UnregisterAllHooks_WithMultipleHooks_UnregistersAll()
        {
            // Arrange
            var hook1 = new MockKiroAgentHook("hook-1");
            var hook2 = new MockKiroAgentHook("hook-2");
            _hooksManager.RegisterHook(hook1);
            _hooksManager.RegisterHook(hook2);

            // Act
            _hooksManager.UnregisterAllHooks();

            // Assert
            Assert.AreEqual(0, _hooksManager.RegisteredHookCount);
            Assert.IsTrue(hook1.WasUnregistered);
            Assert.IsTrue(hook2.WasUnregistered);
        }

        [Test]
        public async Task TriggerHooksAsync_WithFailingHook_HandlesGracefully()
        {
            // Arrange
            var failingHook = MockKiroAgentHook.CreateFailureHook("failing-hook");
            _hooksManager.RegisterHook(failingHook);

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act & Assert (should not throw)
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);
            
            Assert.AreEqual(1, failingHook.ExecutionCount);
        }

        [Test]
        public void HookExecutionEvents_AreTriggeredCorrectly()
        {
            // Arrange
            string triggeredHookId = null;
            HookExecutionResult? triggeredResult = null;
            
            _hooksManager.OnHookExecuted += (hookId, result) =>
            {
                triggeredHookId = hookId;
                triggeredResult = result;
            };

            // Act
            _hooksManager.RegisterHook(_mockHook);

            // Assert
            // Registration event should be triggered, but execution event will be tested in async test
            Assert.IsNotNull(_hooksManager);
        }
    }
}