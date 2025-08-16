using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace Tests.Core
{
    [TestFixture]
    public class KiroAgentHooksIntegrationTests
    {
        private GameObject _testGameObject;
        private KiroAgentHooksManager _hooksManager;
        private PlayerBehaviorAnalyzer _behaviorAnalyzer;
        private MockKiroAgentHook _mockHook;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestIntegration");
            _hooksManager = _testGameObject.AddComponent<KiroAgentHooksManager>();
            _behaviorAnalyzer = _testGameObject.AddComponent<PlayerBehaviorAnalyzer>();
            
            _mockHook = new MockKiroAgentHook(
                "integration-test-hook",
                "Integration Test Hook",
                "Hook for testing integration with PlayerBehaviorAnalyzer",
                priority: 5,
                supportedEventTypes: new[] { 
                    PlayerBehaviorEventType.IntentionPredicted,
                    PlayerBehaviorEventType.PatternRecognized 
                });
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
        public async Task HooksManager_IntegratesWithBehaviorAnalyzer_ExecutesHooksOnEvents()
        {
            // Arrange
            _hooksManager.RegisterHook(_mockHook);
            
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.IntentionPredicted,
                PlayerPosition = new Vector3(10, 0, 5),
                Timestamp = Time.realtimeSinceStartup,
                BehaviorData = new Dictionary<string, object>
                {
                    ["intention"] = "exploration",
                    ["confidence"] = 0.85f
                },
                EnvironmentState = new Dictionary<string, object>
                {
                    ["zone"] = "corridor",
                    ["lighting"] = "dim"
                },
                Confidence = 0.85f
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.IntentionPredicted, eventData);

            // Assert
            Assert.AreEqual(1, _mockHook.ExecutionCount);
            Assert.AreEqual(PlayerBehaviorEventType.IntentionPredicted, _mockHook.LastEventType);
            Assert.AreEqual(new Vector3(10, 0, 5), _mockHook.LastEventData.PlayerPosition);
        }

        [Test]
        public async Task HooksManager_WithMultipleEventTypes_ExecutesCorrectHooks()
        {
            // Arrange
            var intentionHook = new MockKiroAgentHook(
                "intention-hook",
                supportedEventTypes: new[] { PlayerBehaviorEventType.IntentionPredicted });
            
            var patternHook = new MockKiroAgentHook(
                "pattern-hook",
                supportedEventTypes: new[] { PlayerBehaviorEventType.PatternRecognized });

            _hooksManager.RegisterHook(intentionHook);
            _hooksManager.RegisterHook(patternHook);

            var intentionEventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.IntentionPredicted,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            var patternEventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.PatternRecognized,
                PlayerPosition = Vector3.one,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.IntentionPredicted, intentionEventData);
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.PatternRecognized, patternEventData);

            // Assert
            Assert.AreEqual(1, intentionHook.ExecutionCount);
            Assert.AreEqual(0, patternHook.ExecutionCount); // Should not execute for intention event
            
            // Reset and test pattern event
            intentionHook.ResetTracking();
            patternHook.ResetTracking();
            
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.PatternRecognized, patternEventData);
            
            Assert.AreEqual(0, intentionHook.ExecutionCount); // Should not execute for pattern event
            Assert.AreEqual(1, patternHook.ExecutionCount);
        }

        [Test]
        public async Task HooksManager_WithErrorHandling_ContinuesOperationAfterFailure()
        {
            // Arrange
            var successHook = MockKiroAgentHook.CreateSuccessHook("success-hook", priority: 10);
            var failureHook = MockKiroAgentHook.CreateFailureHook("failure-hook", priority: 5);
            var anotherSuccessHook = MockKiroAgentHook.CreateSuccessHook("another-success-hook", priority: 1);

            _hooksManager.RegisterHook(successHook);
            _hooksManager.RegisterHook(failureHook);
            _hooksManager.RegisterHook(anotherSuccessHook);

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);

            // Assert - All hooks should have been executed despite one failing
            Assert.AreEqual(1, successHook.ExecutionCount);
            Assert.AreEqual(1, failureHook.ExecutionCount);
            Assert.AreEqual(1, anotherSuccessHook.ExecutionCount);

            // Verify stats are tracked correctly
            var successStats = _hooksManager.GetHookStats("success-hook");
            var failureStats = _hooksManager.GetHookStats("failure-hook");

            Assert.AreEqual(1, successStats.TotalExecutions);
            Assert.AreEqual(1, successStats.SuccessfulExecutions);
            Assert.AreEqual(0, successStats.FailedExecutions);

            Assert.AreEqual(1, failureStats.TotalExecutions);
            Assert.AreEqual(0, failureStats.SuccessfulExecutions);
            Assert.AreEqual(1, failureStats.FailedExecutions);
        }

        [Test]
        public void HooksManager_EventCallbacks_AreTriggeredCorrectly()
        {
            // Arrange
            string registeredHookId = null;
            string unregisteredHookId = null;
            string executedHookId = null;
            HookExecutionResult? executionResult = null;

            _hooksManager.OnHookRegistered += (hookId) => registeredHookId = hookId;
            _hooksManager.OnHookUnregistered += (hookId) => unregisteredHookId = hookId;
            _hooksManager.OnHookExecuted += (hookId, result) =>
            {
                executedHookId = hookId;
                executionResult = result;
            };

            // Act
            _hooksManager.RegisterHook(_mockHook);
            _hooksManager.UnregisterHook(_mockHook.HookId);

            // Assert
            Assert.AreEqual(_mockHook.HookId, registeredHookId);
            Assert.AreEqual(_mockHook.HookId, unregisteredHookId);
        }

        [Test]
        public async Task HooksManager_PerformanceThrottling_HandlesHighLoad()
        {
            // Arrange
            var hooks = new List<MockKiroAgentHook>();
            for (int i = 0; i < 10; i++)
            {
                var hook = new MockKiroAgentHook($"load-test-hook-{i}")
                {
                    SimulatedExecutionTimeMs = 50 // Simulate some processing time
                };
                hooks.Add(hook);
                _hooksManager.RegisterHook(hook);
            }

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = Vector3.zero,
                Timestamp = Time.realtimeSinceStartup
            };

            // Act
            var startTime = Time.realtimeSinceStartup;
            await _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);
            var endTime = Time.realtimeSinceStartup;

            // Assert
            var totalExecutionTime = (endTime - startTime) * 1000f;
            
            // All hooks should have been executed
            foreach (var hook in hooks)
            {
                Assert.AreEqual(1, hook.ExecutionCount, $"Hook {hook.HookId} was not executed");
            }

            // Execution should be reasonably fast due to concurrent execution
            Assert.Less(totalExecutionTime, 1000f, "Execution took too long, throttling may not be working correctly");
        }
    }
}