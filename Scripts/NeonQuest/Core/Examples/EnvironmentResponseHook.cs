using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Example hook that responds to player behavior by triggering environmental changes
    /// This demonstrates how to create a real Kiro agent hook for the NeonQuest system
    /// </summary>
    public class EnvironmentResponseHook : IKiroAgentHook
    {
        public string HookId => "environment-response-hook";
        public string HookName => "Environment Response Hook";
        public string Description => "Triggers environmental changes based on player movement patterns";
        public int Priority => 10;
        public bool IsEnabled { get; set; } = true;
        public int TimeoutMs => 3000;

        public PlayerBehaviorEventType[] SupportedEventTypes => new[]
        {
            PlayerBehaviorEventType.MovementChanged,
            PlayerBehaviorEventType.IntentionPredicted,
            PlayerBehaviorEventType.PatternRecognized,
            PlayerBehaviorEventType.DwellTimeUpdated
        };

        // Configuration
        private readonly float _movementThreshold = 2.0f;
        private readonly float _dwellTimeThreshold = 5.0f;
        private readonly Dictionary<string, float> _lastTriggerTimes = new Dictionary<string, float>();
        private readonly float _cooldownTime = 10.0f;

        public async Task<HookExecutionResult> ExecuteAsync(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            try
            {
                var startTime = Time.realtimeSinceStartup;
                var resultData = new Dictionary<string, object>();

                switch (eventType)
                {
                    case PlayerBehaviorEventType.MovementChanged:
                        await HandleMovementChanged(eventData, resultData);
                        break;

                    case PlayerBehaviorEventType.IntentionPredicted:
                        await HandleIntentionPredicted(eventData, resultData);
                        break;

                    case PlayerBehaviorEventType.PatternRecognized:
                        await HandlePatternRecognized(eventData, resultData);
                        break;

                    case PlayerBehaviorEventType.DwellTimeUpdated:
                        await HandleDwellTimeUpdated(eventData, resultData);
                        break;

                    default:
                        return HookExecutionResult.CreateFailure($"Unsupported event type: {eventType}");
                }

                var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                return HookExecutionResult.CreateSuccess(
                    $"Successfully processed {eventType} event",
                    resultData,
                    executionTime);
            }
            catch (Exception ex)
            {
                NeonQuestLogger.LogError($"EnvironmentResponseHook execution failed: {ex.Message}");
                return HookExecutionResult.CreateFailure(
                    "Hook execution failed",
                    ex.Message);
            }
        }

        public bool ShouldExecute(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            if (!IsEnabled)
                return false;

            // Check cooldown for this event type
            var cooldownKey = $"{eventType}_{eventData.PlayerPosition}";
            if (_lastTriggerTimes.TryGetValue(cooldownKey, out var lastTime))
            {
                if (Time.realtimeSinceStartup - lastTime < _cooldownTime)
                {
                    return false;
                }
            }

            // Event-specific conditions
            switch (eventType)
            {
                case PlayerBehaviorEventType.MovementChanged:
                    return eventData.BehaviorData.TryGetValue("speed", out var speedObj) &&
                           speedObj is float speed && speed > _movementThreshold;

                case PlayerBehaviorEventType.IntentionPredicted:
                    return eventData.Confidence > 0.7f;

                case PlayerBehaviorEventType.PatternRecognized:
                    return eventData.BehaviorData.ContainsKey("pattern") &&
                           eventData.Confidence > 0.6f;

                case PlayerBehaviorEventType.DwellTimeUpdated:
                    return eventData.BehaviorData.TryGetValue("dwellTime", out var dwellObj) &&
                           dwellObj is float dwellTime && dwellTime > _dwellTimeThreshold;

                default:
                    return false;
            }
        }

        public void OnRegistered()
        {
            NeonQuestLogger.LogInfo($"EnvironmentResponseHook registered successfully");
        }

        public void OnUnregistered()
        {
            NeonQuestLogger.LogInfo($"EnvironmentResponseHook unregistered");
            _lastTriggerTimes.Clear();
        }

        private async Task HandleMovementChanged(PlayerBehaviorEventData eventData, Dictionary<string, object> resultData)
        {
            // Simulate processing time
            await Task.Delay(50);

            var playerPosition = eventData.PlayerPosition;
            var speed = eventData.BehaviorData.TryGetValue("speed", out var speedObj) ? (float)speedObj : 0f;

            NeonQuestLogger.LogInfo($"Player movement detected at {playerPosition} with speed {speed}");

            // Trigger lighting response based on movement speed
            if (speed > 5.0f)
            {
                resultData["lightingResponse"] = "surge";
                resultData["intensity"] = Mathf.Clamp(speed / 10f, 0.5f, 2.0f);
            }
            else if (speed < 1.0f)
            {
                resultData["lightingResponse"] = "dim";
                resultData["intensity"] = 0.3f;
            }

            resultData["triggerType"] = "movement";
            resultData["playerSpeed"] = speed;
            
            UpdateCooldown($"MovementChanged_{playerPosition}");
        }

        private async Task HandleIntentionPredicted(PlayerBehaviorEventData eventData, Dictionary<string, object> resultData)
        {
            await Task.Delay(75);

            var intention = eventData.BehaviorData.TryGetValue("intention", out var intentionObj) ? 
                intentionObj.ToString() : "unknown";

            NeonQuestLogger.LogInfo($"Player intention predicted: {intention} (confidence: {eventData.Confidence})");

            switch (intention.ToLower())
            {
                case "exploration":
                    resultData["environmentResponse"] = "expand_corridors";
                    resultData["generationDistance"] = 75f;
                    break;

                case "backtracking":
                    resultData["environmentResponse"] = "maintain_consistency";
                    resultData["variationLevel"] = 0.2f;
                    break;

                case "searching":
                    resultData["environmentResponse"] = "add_interactive_elements";
                    resultData["interactiveElementCount"] = 3;
                    break;

                default:
                    resultData["environmentResponse"] = "default";
                    break;
            }

            resultData["triggerType"] = "intention";
            resultData["intention"] = intention;
            resultData["confidence"] = eventData.Confidence;

            UpdateCooldown($"IntentionPredicted_{intention}");
        }

        private async Task HandlePatternRecognized(PlayerBehaviorEventData eventData, Dictionary<string, object> resultData)
        {
            await Task.Delay(100);

            var pattern = eventData.BehaviorData.TryGetValue("pattern", out var patternObj) ? 
                patternObj.ToString() : "unknown";

            NeonQuestLogger.LogInfo($"Player pattern recognized: {pattern} (confidence: {eventData.Confidence})");

            switch (pattern.ToLower())
            {
                case "rapid_exploration":
                    resultData["atmosphericResponse"] = "increase_fog_density";
                    resultData["fogDensity"] = 0.8f;
                    resultData["audioResponse"] = "intensify_ambient";
                    break;

                case "cautious_movement":
                    resultData["atmosphericResponse"] = "reduce_fog_density";
                    resultData["fogDensity"] = 0.3f;
                    resultData["audioResponse"] = "subtle_ambient";
                    break;

                case "repetitive_path":
                    resultData["layoutResponse"] = "introduce_variation";
                    resultData["variationIntensity"] = 0.6f;
                    break;

                default:
                    resultData["atmosphericResponse"] = "maintain_current";
                    break;
            }

            resultData["triggerType"] = "pattern";
            resultData["pattern"] = pattern;
            resultData["confidence"] = eventData.Confidence;

            UpdateCooldown($"PatternRecognized_{pattern}");
        }

        private async Task HandleDwellTimeUpdated(PlayerBehaviorEventData eventData, Dictionary<string, object> resultData)
        {
            await Task.Delay(25);

            var dwellTime = eventData.BehaviorData.TryGetValue("dwellTime", out var dwellObj) ? 
                (float)dwellObj : 0f;

            NeonQuestLogger.LogInfo($"Player dwell time updated: {dwellTime}s at {eventData.PlayerPosition}");

            if (dwellTime > 10f)
            {
                // Player has been stationary for a while, create subtle environmental changes
                resultData["environmentalChange"] = "subtle_variation";
                resultData["changeIntensity"] = Mathf.Clamp(dwellTime / 30f, 0.1f, 0.5f);
                resultData["lightingPulse"] = true;
            }
            else if (dwellTime > 5f)
            {
                // Moderate dwell time, adjust lighting focus
                resultData["lightingFocus"] = "increase";
                resultData["focusRadius"] = 10f;
            }

            resultData["triggerType"] = "dwellTime";
            resultData["dwellTime"] = dwellTime;

            UpdateCooldown($"DwellTimeUpdated_{eventData.PlayerPosition}");
        }

        private void UpdateCooldown(string key)
        {
            _lastTriggerTimes[key] = Time.realtimeSinceStartup;
        }
    }
}