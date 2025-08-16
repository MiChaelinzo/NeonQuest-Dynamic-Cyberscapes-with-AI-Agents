using System;
using System.Collections.Generic;
using UnityEngine;
using NeonQuest.Configuration;

namespace NeonQuest.Core
{
    /// <summary>
    /// Concrete implementation of environment trigger that evaluates conditions and dispatches generation commands
    /// </summary>
    public class EnvironmentTrigger : IEnvironmentTrigger
    {
        private readonly GenerationRule rule;
        private float lastTriggerTime;
        private readonly Dictionary<string, object> triggerContext;

        public float Priority => rule.Priority;
        public float Cooldown => rule.Cooldown;
        public bool IsOnCooldown => Time.time - lastTriggerTime < Cooldown;
        public string RuleName => rule.RuleName;

        // Events for trigger activation
        public event Action<string, Dictionary<string, object>> OnTriggerActivated;
        public event Action<GenerationAction> OnGenerationCommandDispatched;

        public EnvironmentTrigger(GenerationRule generationRule)
        {
            rule = generationRule ?? throw new ArgumentNullException(nameof(generationRule));
            triggerContext = new Dictionary<string, object>();
            lastTriggerTime = -rule.Cooldown; // Allow immediate first trigger
        }

        public bool EvaluateConditions(Vector3 playerPosition, Dictionary<string, object> behaviorData, Dictionary<string, object> environmentState)
        {
            if (rule == null || !rule.IsValid())
                return false;

            if (IsOnCooldown)
                return false;

            // Build evaluation context
            var context = BuildEvaluationContext(playerPosition, behaviorData, environmentState);

            // Evaluate all conditions (AND logic)
            foreach (var condition in rule.Conditions)
            {
                if (!condition.Evaluate(context))
                {
                    return false;
                }
            }

            return true;
        }

        public void DispatchGenerationCommand(Dictionary<string, object> triggerData)
        {
            if (rule == null || rule.Actions == null)
                return;

            // Update trigger context with current data
            UpdateTriggerContext(triggerData);

            // Dispatch each action in the rule
            foreach (var action in rule.Actions)
            {
                try
                {
                    DispatchAction(action, triggerData);
                    OnGenerationCommandDispatched?.Invoke(action);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to dispatch action {action.Type} for trigger {rule.RuleName}: {ex.Message}");
                }
            }

            // Update cooldown
            lastTriggerTime = Time.time;
            
            // Fire trigger activated event
            OnTriggerActivated?.Invoke(rule.RuleName, new Dictionary<string, object>(triggerContext));
        }

        public void ResetCooldown()
        {
            lastTriggerTime = -rule.Cooldown;
        }

        private Dictionary<string, object> BuildEvaluationContext(Vector3 playerPosition, Dictionary<string, object> behaviorData, Dictionary<string, object> environmentState)
        {
            var context = new Dictionary<string, object>();

            // Add player position data
            context["PlayerPosition"] = playerPosition;
            context["PlayerPositionX"] = playerPosition.x;
            context["PlayerPositionY"] = playerPosition.y;
            context["PlayerPositionZ"] = playerPosition.z;

            // Add behavior data
            if (behaviorData != null)
            {
                foreach (var kvp in behaviorData)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            // Add environment state
            if (environmentState != null)
            {
                foreach (var kvp in environmentState)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            // Add time-based context
            context["GameTime"] = Time.time;
            context["TimeSinceLastTrigger"] = Time.time - lastTriggerTime;

            return context;
        }

        private void UpdateTriggerContext(Dictionary<string, object> triggerData)
        {
            triggerContext.Clear();
            triggerContext["RuleName"] = rule.RuleName;
            triggerContext["TriggerTime"] = Time.time;
            triggerContext["Priority"] = rule.Priority;

            if (triggerData != null)
            {
                foreach (var kvp in triggerData)
                {
                    triggerContext[kvp.Key] = kvp.Value;
                }
            }
        }

        private void DispatchAction(GenerationAction action, Dictionary<string, object> triggerData)
        {
            // Create action context with trigger data and action parameters
            var actionContext = new Dictionary<string, object>(triggerData ?? new Dictionary<string, object>());
            
            // Add action-specific parameters
            foreach (var param in action.Parameters)
            {
                actionContext[param.Key] = param.Value;
            }

            // Add action metadata
            actionContext["ActionType"] = action.Type.ToString();
            actionContext["ActionTarget"] = action.Target;
            actionContext["ActionIntensity"] = action.Intensity;
            actionContext["ActionDuration"] = action.Duration;
            actionContext["TriggerRule"] = rule.RuleName;

            // Log action dispatch for debugging
            Debug.Log($"Dispatching {action.Type} action from trigger '{rule.RuleName}' with target '{action.Target}'");

            // The actual dispatch will be handled by the EnvironmentTriggersManager
            // which will route commands to appropriate generation systems
        }

        public GenerationRule GetRule()
        {
            return rule;
        }

        public Dictionary<string, object> GetTriggerContext()
        {
            return new Dictionary<string, object>(triggerContext);
        }

        public float GetTimeSinceLastTrigger()
        {
            return Time.time - lastTriggerTime;
        }

        public bool HasTriggeredRecently(float timeWindow)
        {
            return Time.time - lastTriggerTime <= timeWindow;
        }
    }
}