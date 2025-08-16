using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core
{
    /// <summary>
    /// Mock implementation of IKiroAgentHook for testing without full Kiro integration
    /// </summary>
    public class MockKiroAgentHook : IKiroAgentHook
    {
        public string HookId { get; private set; }
        public string HookName { get; private set; }
        public string Description { get; private set; }
        public int Priority { get; private set; }
        public bool IsEnabled { get; set; }
        public int TimeoutMs { get; private set; }
        public PlayerBehaviorEventType[] SupportedEventTypes { get; private set; }

        // Mock-specific properties
        public bool ShouldSimulateFailure { get; set; }
        public bool ShouldSimulateTimeout { get; set; }
        public int SimulatedExecutionTimeMs { get; set; }
        public Dictionary<string, object> MockResultData { get; set; }
        public Func<PlayerBehaviorEventType, PlayerBehaviorEventData, bool> CustomShouldExecuteLogic { get; set; }

        // Tracking properties for testing
        public int ExecutionCount { get; private set; }
        public PlayerBehaviorEventType LastEventType { get; private set; }
        public PlayerBehaviorEventData LastEventData { get; private set; }
        public bool WasRegistered { get; private set; }
        public bool WasUnregistered { get; private set; }

        public MockKiroAgentHook(
            string hookId,
            string hookName = null,
            string description = null,
            int priority = 0,
            int timeoutMs = 5000,
            PlayerBehaviorEventType[] supportedEventTypes = null)
        {
            HookId = hookId;
            HookName = hookName ?? $"Mock Hook {hookId}";
            Description = description ?? $"Mock hook for testing purposes";
            Priority = priority;
            IsEnabled = true;
            TimeoutMs = timeoutMs;
            SupportedEventTypes = supportedEventTypes ?? new[] { 
                PlayerBehaviorEventType.MovementChanged,
                PlayerBehaviorEventType.IntentionPredicted,
                PlayerBehaviorEventType.PatternRecognized
            };

            SimulatedExecutionTimeMs = 100;
            MockResultData = new Dictionary<string, object>();
        }

        public async Task<HookExecutionResult> ExecuteAsync(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            ExecutionCount++;
            LastEventType = eventType;
            LastEventData = eventData;

            NeonQuestLogger.LogInfo($"Mock hook {HookId} executing for event {eventType}");

            // Simulate timeout if requested
            if (ShouldSimulateTimeout)
            {
                await Task.Delay(TimeoutMs + 1000);
            }
            else
            {
                // Simulate normal execution time
                await Task.Delay(SimulatedExecutionTimeMs);
            }

            // Simulate failure if requested
            if (ShouldSimulateFailure)
            {
                return HookExecutionResult.CreateFailure(
                    "Simulated failure for testing",
                    "Mock hook was configured to simulate failure",
                    SimulatedExecutionTimeMs);
            }

            // Create success result with mock data
            var resultData = new Dictionary<string, object>(MockResultData)
            {
                ["executionCount"] = ExecutionCount,
                ["eventType"] = eventType.ToString(),
                ["playerPosition"] = eventData.PlayerPosition,
                ["timestamp"] = eventData.Timestamp
            };

            return HookExecutionResult.CreateSuccess(
                $"Mock hook {HookId} executed successfully",
                resultData,
                SimulatedExecutionTimeMs);
        }

        public bool ShouldExecute(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            // Use custom logic if provided
            if (CustomShouldExecuteLogic != null)
            {
                return CustomShouldExecuteLogic(eventType, eventData);
            }

            // Default logic: execute if event type is supported and hook is enabled
            return IsEnabled && Array.Exists(SupportedEventTypes, t => t == eventType);
        }

        public void OnRegistered()
        {
            WasRegistered = true;
            NeonQuestLogger.LogInfo($"Mock hook {HookId} was registered");
        }

        public void OnUnregistered()
        {
            WasUnregistered = true;
            NeonQuestLogger.LogInfo($"Mock hook {HookId} was unregistered");
        }

        /// <summary>
        /// Resets all tracking properties for testing
        /// </summary>
        public void ResetTracking()
        {
            ExecutionCount = 0;
            WasRegistered = false;
            WasUnregistered = false;
            LastEventType = default;
            LastEventData = default;
        }

        /// <summary>
        /// Creates a mock hook that always succeeds
        /// </summary>
        public static MockKiroAgentHook CreateSuccessHook(string hookId, int priority = 0)
        {
            return new MockKiroAgentHook(hookId, priority: priority)
            {
                ShouldSimulateFailure = false,
                ShouldSimulateTimeout = false,
                SimulatedExecutionTimeMs = 50
            };
        }

        /// <summary>
        /// Creates a mock hook that always fails
        /// </summary>
        public static MockKiroAgentHook CreateFailureHook(string hookId, int priority = 0)
        {
            return new MockKiroAgentHook(hookId, priority: priority)
            {
                ShouldSimulateFailure = true,
                ShouldSimulateTimeout = false,
                SimulatedExecutionTimeMs = 100
            };
        }

        /// <summary>
        /// Creates a mock hook that times out
        /// </summary>
        public static MockKiroAgentHook CreateTimeoutHook(string hookId, int timeoutMs = 1000, int priority = 0)
        {
            return new MockKiroAgentHook(hookId, timeoutMs: timeoutMs, priority: priority)
            {
                ShouldSimulateFailure = false,
                ShouldSimulateTimeout = true
            };
        }

        /// <summary>
        /// Creates a mock hook with custom execution logic
        /// </summary>
        public static MockKiroAgentHook CreateCustomHook(
            string hookId, 
            Func<PlayerBehaviorEventType, PlayerBehaviorEventData, bool> shouldExecuteLogic,
            int priority = 0)
        {
            return new MockKiroAgentHook(hookId, priority: priority)
            {
                CustomShouldExecuteLogic = shouldExecuteLogic,
                ShouldSimulateFailure = false,
                ShouldSimulateTimeout = false,
                SimulatedExecutionTimeMs = 75
            };
        }
    }
}