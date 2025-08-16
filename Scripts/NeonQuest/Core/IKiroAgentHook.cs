using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonQuest.Core
{
    /// <summary>
    /// Interface for Kiro agent hooks that can be registered to respond to player behavior events
    /// </summary>
    public interface IKiroAgentHook
    {
        /// <summary>
        /// Unique identifier for this hook
        /// </summary>
        string HookId { get; }

        /// <summary>
        /// Human-readable name for this hook
        /// </summary>
        string HookName { get; }

        /// <summary>
        /// Description of what this hook does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Priority of this hook (higher values execute first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this hook is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Maximum execution time in milliseconds before timeout
        /// </summary>
        int TimeoutMs { get; }

        /// <summary>
        /// Types of player behavior events this hook responds to
        /// </summary>
        PlayerBehaviorEventType[] SupportedEventTypes { get; }

        /// <summary>
        /// Executes the hook with the provided event data
        /// </summary>
        /// <param name="eventType">Type of behavior event</param>
        /// <param name="eventData">Event data containing player behavior information</param>
        /// <returns>Hook execution result</returns>
        Task<HookExecutionResult> ExecuteAsync(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData);

        /// <summary>
        /// Validates whether this hook should execute for the given event
        /// </summary>
        /// <param name="eventType">Type of behavior event</param>
        /// <param name="eventData">Event data</param>
        /// <returns>True if hook should execute</returns>
        bool ShouldExecute(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData);

        /// <summary>
        /// Called when the hook is registered with the manager
        /// </summary>
        void OnRegistered();

        /// <summary>
        /// Called when the hook is unregistered from the manager
        /// </summary>
        void OnUnregistered();
    }

    /// <summary>
    /// Types of player behavior events that can trigger hooks
    /// </summary>
    public enum PlayerBehaviorEventType
    {
        MovementChanged,
        IntentionPredicted,
        PatternRecognized,
        DwellTimeUpdated,
        EnvironmentalFactorsUpdated,
        BehaviorContextUpdated
    }

    /// <summary>
    /// Data structure containing player behavior event information
    /// </summary>
    [Serializable]
    public struct PlayerBehaviorEventData
    {
        public PlayerBehaviorEventType EventType;
        public float Timestamp;
        public Vector3 PlayerPosition;
        public Dictionary<string, object> BehaviorData;
        public Dictionary<string, object> EnvironmentState;
        public Dictionary<string, float> EnvironmentalFactors;
        public string TriggerId;
        public float Confidence;
    }

    /// <summary>
    /// Result of hook execution
    /// </summary>
    [Serializable]
    public struct HookExecutionResult
    {
        public bool Success;
        public string Message;
        public Dictionary<string, object> ResultData;
        public float ExecutionTimeMs;
        public string ErrorDetails;

        public static HookExecutionResult CreateSuccess(string message = "", Dictionary<string, object> resultData = null, float executionTimeMs = 0f)
        {
            return new HookExecutionResult
            {
                Success = true,
                Message = message,
                ResultData = resultData ?? new Dictionary<string, object>(),
                ExecutionTimeMs = executionTimeMs,
                ErrorDetails = null
            };
        }

        public static HookExecutionResult CreateFailure(string message, string errorDetails = "", float executionTimeMs = 0f)
        {
            return new HookExecutionResult
            {
                Success = false,
                Message = message,
                ResultData = new Dictionary<string, object>(),
                ExecutionTimeMs = executionTimeMs,
                ErrorDetails = errorDetails
            };
        }
    }
}