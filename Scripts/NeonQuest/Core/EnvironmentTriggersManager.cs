using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Core
{
    /// <summary>
    /// Manages multiple environment triggers, handles priority and cooldown management,
    /// and dispatches generation commands to appropriate systems
    /// </summary>
    public class EnvironmentTriggersManager : MonoBehaviour
    {
        [Header("Trigger Configuration")]
        [SerializeField] private float evaluationInterval = 0.1f;
        [SerializeField] private int maxTriggersPerFrame = 3;
        [SerializeField] private bool enableDebugLogging = false;

        // Dependencies
        private PlayerBehaviorAnalyzer behaviorAnalyzer;
        private PlayerMovementTracker movementTracker;
        private EnvironmentRulesEngine rulesEngine;

        // Trigger management
        private List<EnvironmentTrigger> activeTriggers;
        private Dictionary<string, EnvironmentTrigger> triggersByName;
        private Queue<TriggerEvaluationRequest> evaluationQueue;
        private List<TriggeredAction> pendingActions;

        // State tracking
        private Dictionary<string, object> currentEnvironmentState;
        private Vector3 lastPlayerPosition;
        private float lastEvaluationTime;

        // Events
        public event Action<string, Dictionary<string, object>> OnTriggerActivated;
        public event Action<GenerationAction, Dictionary<string, object>> OnGenerationCommandDispatched;
        public event Action<List<EnvironmentTrigger>> OnTriggersUpdated;

        // Statistics
        private TriggerStatistics statistics;
        
        // For testing - allows dependency injection
        private bool dependenciesInjected = false;

        private void Awake()
        {
            activeTriggers = new List<EnvironmentTrigger>();
            triggersByName = new Dictionary<string, EnvironmentTrigger>();
            evaluationQueue = new Queue<TriggerEvaluationRequest>();
            pendingActions = new List<TriggeredAction>();
            currentEnvironmentState = new Dictionary<string, object>();
            statistics = new TriggerStatistics();
        }

        private void Start()
        {
            if (!dependenciesInjected)
            {
                // Find dependencies
                behaviorAnalyzer = GetComponent<PlayerBehaviorAnalyzer>() ?? FindObjectOfType<PlayerBehaviorAnalyzer>();
                movementTracker = GetComponent<PlayerMovementTracker>() ?? FindObjectOfType<PlayerMovementTracker>();
                rulesEngine = FindObjectOfType<EnvironmentRulesEngine>();
            }

            if (behaviorAnalyzer == null)
            {
                Debug.LogError("EnvironmentTriggersManager requires PlayerBehaviorAnalyzer");
                enabled = false;
                return;
            }

            if (movementTracker == null)
            {
                Debug.LogError("EnvironmentTriggersManager requires PlayerMovementTracker");
                enabled = false;
                return;
            }

            if (rulesEngine == null)
            {
                Debug.LogError("EnvironmentTriggersManager requires EnvironmentRulesEngine");
                enabled = false;
                return;
            }

            // Subscribe to events
            behaviorAnalyzer.OnContextUpdated += OnBehaviorContextUpdated;
            movementTracker.OnMovementUpdate += OnMovementUpdate;
            rulesEngine.OnRulesUpdated += OnRulesUpdated;

            // Initialize triggers from rules engine
            InitializeTriggersFromRules();

            // Start evaluation loop
            InvokeRepeating(nameof(EvaluateTriggers), 0f, evaluationInterval);
        }

        private void InitializeTriggersFromRules()
        {
            if (rulesEngine?.GetConfiguration()?.Rules == null)
                return;

            var rules = rulesEngine.GetConfiguration().Rules;
            LoadTriggersFromRules(rules);
        }

        public void LoadTriggersFromRules(List<GenerationRule> rules)
        {
            // Clear existing triggers
            ClearAllTriggers();

            // Create triggers from rules
            foreach (var rule in rules)
            {
                if (rule.IsValid())
                {
                    AddTrigger(rule);
                }
                else
                {
                    Debug.LogWarning($"Invalid rule '{rule.RuleName}' skipped during trigger initialization");
                }
            }

            OnTriggersUpdated?.Invoke(new List<EnvironmentTrigger>(activeTriggers));
            
            if (enableDebugLogging)
            {
                Debug.Log($"Loaded {activeTriggers.Count} triggers from {rules.Count} rules");
            }
        }

        public void AddTrigger(GenerationRule rule)
        {
            if (rule == null || !rule.IsValid())
            {
                Debug.LogWarning("Cannot add invalid rule as trigger");
                return;
            }

            if (triggersByName.ContainsKey(rule.RuleName))
            {
                Debug.LogWarning($"Trigger with name '{rule.RuleName}' already exists. Replacing.");
                RemoveTrigger(rule.RuleName);
            }

            var trigger = new EnvironmentTrigger(rule);
            trigger.OnTriggerActivated += OnTriggerActivatedInternal;
            trigger.OnGenerationCommandDispatched += OnGenerationCommandDispatchedInternal;

            activeTriggers.Add(trigger);
            triggersByName[rule.RuleName] = trigger;

            // Sort triggers by priority (highest first)
            activeTriggers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            if (enableDebugLogging)
            {
                Debug.Log($"Added trigger '{rule.RuleName}' with priority {rule.Priority}");
            }
        }

        public bool RemoveTrigger(string ruleName)
        {
            if (!triggersByName.TryGetValue(ruleName, out var trigger))
                return false;

            trigger.OnTriggerActivated -= OnTriggerActivatedInternal;
            trigger.OnGenerationCommandDispatched -= OnGenerationCommandDispatchedInternal;

            activeTriggers.Remove(trigger);
            triggersByName.Remove(ruleName);

            if (enableDebugLogging)
            {
                Debug.Log($"Removed trigger '{ruleName}'");
            }

            return true;
        }

        public void ClearAllTriggers()
        {
            foreach (var trigger in activeTriggers)
            {
                trigger.OnTriggerActivated -= OnTriggerActivatedInternal;
                trigger.OnGenerationCommandDispatched -= OnGenerationCommandDispatchedInternal;
            }

            activeTriggers.Clear();
            triggersByName.Clear();
            evaluationQueue.Clear();
            pendingActions.Clear();

            if (enableDebugLogging)
            {
                Debug.Log("Cleared all triggers");
            }
        }

        private void EvaluateTriggers()
        {
            if (activeTriggers.Count == 0)
                return;

            // Update environment state
            UpdateEnvironmentState();

            // Get current player data
            var playerPosition = movementTracker.transform.position;
            var behaviorData = GetCurrentBehaviorData();

            // Evaluate triggers in priority order
            int triggersEvaluated = 0;
            var triggeredThisFrame = new List<EnvironmentTrigger>();

            foreach (var trigger in activeTriggers)
            {
                if (triggersEvaluated >= maxTriggersPerFrame)
                    break;

                try
                {
                    if (trigger.EvaluateConditions(playerPosition, behaviorData, currentEnvironmentState))
                    {
                        // Create trigger data
                        var triggerData = CreateTriggerData(trigger, playerPosition, behaviorData);
                        
                        // Dispatch the trigger
                        trigger.DispatchGenerationCommand(triggerData);
                        triggeredThisFrame.Add(trigger);

                        statistics.RecordTriggerActivation(trigger.RuleName);

                        if (enableDebugLogging)
                        {
                            Debug.Log($"Trigger '{trigger.RuleName}' activated with priority {trigger.Priority}");
                        }
                    }

                    triggersEvaluated++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error evaluating trigger '{trigger.RuleName}': {ex.Message}");
                    statistics.RecordTriggerError(trigger.RuleName);
                }
            }

            // Process any pending actions
            ProcessPendingActions();

            lastEvaluationTime = Time.time;
            lastPlayerPosition = playerPosition;
        }

        private void UpdateEnvironmentState()
        {
            currentEnvironmentState.Clear();

            // Add basic environment data
            currentEnvironmentState["GameTime"] = Time.time;
            currentEnvironmentState["FrameCount"] = Time.frameCount;
            currentEnvironmentState["DeltaTime"] = Time.deltaTime;

            // Add player position data
            var playerPos = movementTracker.transform.position;
            currentEnvironmentState["PlayerPosition"] = playerPos;
            currentEnvironmentState["PlayerPositionX"] = playerPos.x;
            currentEnvironmentState["PlayerPositionY"] = playerPos.y;
            currentEnvironmentState["PlayerPositionZ"] = playerPos.z;

            // Add movement data
            var movementData = movementTracker.GetCurrentMovementData();
            currentEnvironmentState["PlayerSpeed"] = movementData.Speed;
            currentEnvironmentState["PlayerDirection"] = movementData.Direction;
            currentEnvironmentState["MovementPattern"] = movementData.Pattern.ToString();
            currentEnvironmentState["IsBacktracking"] = movementData.IsBacktracking;
            currentEnvironmentState["DwellTime"] = movementData.DwellTime;

            // Add distance traveled since last evaluation
            if (lastPlayerPosition != Vector3.zero)
            {
                currentEnvironmentState["DistanceTraveled"] = Vector3.Distance(playerPos, lastPlayerPosition);
            }
        }

        private Dictionary<string, object> GetCurrentBehaviorData()
        {
            var behaviorData = new Dictionary<string, object>();

            if (behaviorAnalyzer != null)
            {
                var context = behaviorAnalyzer.GetCurrentBehaviorContext();
                var factors = behaviorAnalyzer.GetEnvironmentalFactors();

                behaviorData["PredictedIntention"] = context.PredictedIntention.ToString();
                behaviorData["IntentionConfidence"] = context.IntentionConfidence;

                foreach (var factor in factors)
                {
                    behaviorData[factor.Key] = factor.Value;
                }
            }

            return behaviorData;
        }

        private Dictionary<string, object> CreateTriggerData(EnvironmentTrigger trigger, Vector3 playerPosition, Dictionary<string, object> behaviorData)
        {
            var triggerData = new Dictionary<string, object>();

            // Add trigger metadata
            triggerData["TriggerName"] = trigger.RuleName;
            triggerData["TriggerPriority"] = trigger.Priority;
            triggerData["TriggerTime"] = Time.time;

            // Add player data
            triggerData["PlayerPosition"] = playerPosition;
            triggerData["PlayerPositionX"] = playerPosition.x;
            triggerData["PlayerPositionY"] = playerPosition.y;
            triggerData["PlayerPositionZ"] = playerPosition.z;

            // Add behavior data
            if (behaviorData != null)
            {
                foreach (var kvp in behaviorData)
                {
                    triggerData[kvp.Key] = kvp.Value;
                }
            }

            // Add environment state
            foreach (var kvp in currentEnvironmentState)
            {
                if (!triggerData.ContainsKey(kvp.Key))
                {
                    triggerData[kvp.Key] = kvp.Value;
                }
            }

            return triggerData;
        }

        private void ProcessPendingActions()
        {
            if (pendingActions.Count == 0)
                return;

            // Process actions in order of trigger priority
            pendingActions.Sort((a, b) => b.TriggerPriority.CompareTo(a.TriggerPriority));

            foreach (var action in pendingActions)
            {
                try
                {
                    OnGenerationCommandDispatched?.Invoke(action.Action, action.Context);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing pending action {action.Action.Type}: {ex.Message}");
                }
            }

            pendingActions.Clear();
        }

        private void OnTriggerActivatedInternal(string triggerName, Dictionary<string, object> context)
        {
            OnTriggerActivated?.Invoke(triggerName, context);
        }

        private void OnGenerationCommandDispatchedInternal(GenerationAction action)
        {
            // Queue the action for processing
            var triggeredAction = new TriggeredAction
            {
                Action = action,
                Context = currentEnvironmentState,
                TriggerPriority = GetTriggerPriority(action),
                Timestamp = Time.time
            };

            pendingActions.Add(triggeredAction);
        }

        private float GetTriggerPriority(GenerationAction action)
        {
            // Try to find the trigger that generated this action
            // This is a simplified approach - in a more complex system,
            // we might need to track action sources more explicitly
            return 1.0f;
        }

        private void OnBehaviorContextUpdated(BehaviorContext context)
        {
            // Behavior context updated - triggers will be evaluated on next cycle
        }

        private void OnMovementUpdate(MovementData movementData)
        {
            // Movement updated - triggers will be evaluated on next cycle
        }

        private void OnRulesUpdated(EnvironmentConfiguration config)
        {
            if (config?.Rules != null)
            {
                LoadTriggersFromRules(config.Rules);
            }
        }

        // Public API methods
        public List<EnvironmentTrigger> GetActiveTriggers()
        {
            return new List<EnvironmentTrigger>(activeTriggers);
        }

        public EnvironmentTrigger GetTrigger(string ruleName)
        {
            triggersByName.TryGetValue(ruleName, out var trigger);
            return trigger;
        }

        public int GetActiveTriggerCount()
        {
            return activeTriggers.Count;
        }

        public TriggerStatistics GetStatistics()
        {
            return statistics;
        }

        public void ResetTriggerCooldown(string ruleName)
        {
            if (triggersByName.TryGetValue(ruleName, out var trigger))
            {
                trigger.ResetCooldown();
            }
        }

        public void ResetAllTriggerCooldowns()
        {
            foreach (var trigger in activeTriggers)
            {
                trigger.ResetCooldown();
            }
        }

        public Dictionary<string, object> GetCurrentEnvironmentState()
        {
            return new Dictionary<string, object>(currentEnvironmentState);
        }
        
        // For testing - allows dependency injection
        public void InjectDependencies(PlayerBehaviorAnalyzer behaviorAnalyzer, PlayerMovementTracker movementTracker, EnvironmentRulesEngine rulesEngine)
        {
            this.behaviorAnalyzer = behaviorAnalyzer;
            this.movementTracker = movementTracker;
            this.rulesEngine = rulesEngine;
            this.dependenciesInjected = true;
        }

        private void OnDestroy()
        {
            CancelInvoke();
            ClearAllTriggers();

            if (behaviorAnalyzer != null)
                behaviorAnalyzer.OnContextUpdated -= OnBehaviorContextUpdated;
            
            if (movementTracker != null)
                movementTracker.OnMovementUpdate -= OnMovementUpdate;
            
            if (rulesEngine != null)
                rulesEngine.OnRulesUpdated -= OnRulesUpdated;
        }
    }

    [Serializable]
    public struct TriggerEvaluationRequest
    {
        public EnvironmentTrigger Trigger;
        public Vector3 PlayerPosition;
        public Dictionary<string, object> BehaviorData;
        public Dictionary<string, object> EnvironmentState;
        public float RequestTime;
    }

    [Serializable]
    public struct TriggeredAction
    {
        public GenerationAction Action;
        public Dictionary<string, object> Context;
        public float TriggerPriority;
        public float Timestamp;
    }

    [Serializable]
    public class TriggerStatistics
    {
        public Dictionary<string, int> TriggerActivationCounts { get; private set; }
        public Dictionary<string, int> TriggerErrorCounts { get; private set; }
        public Dictionary<string, float> LastActivationTimes { get; private set; }
        public int TotalEvaluations { get; private set; }
        public int TotalActivations { get; private set; }
        public int TotalErrors { get; private set; }

        public TriggerStatistics()
        {
            TriggerActivationCounts = new Dictionary<string, int>();
            TriggerErrorCounts = new Dictionary<string, int>();
            LastActivationTimes = new Dictionary<string, float>();
        }

        public void RecordTriggerActivation(string triggerName)
        {
            if (!TriggerActivationCounts.ContainsKey(triggerName))
                TriggerActivationCounts[triggerName] = 0;

            TriggerActivationCounts[triggerName]++;
            LastActivationTimes[triggerName] = Time.time;
            TotalActivations++;
        }

        public void RecordTriggerError(string triggerName)
        {
            if (!TriggerErrorCounts.ContainsKey(triggerName))
                TriggerErrorCounts[triggerName] = 0;

            TriggerErrorCounts[triggerName]++;
            TotalErrors++;
        }

        public void RecordEvaluation()
        {
            TotalEvaluations++;
        }

        public float GetActivationRate(string triggerName)
        {
            if (!TriggerActivationCounts.ContainsKey(triggerName) || TotalEvaluations == 0)
                return 0f;

            return (float)TriggerActivationCounts[triggerName] / TotalEvaluations;
        }

        public void Reset()
        {
            TriggerActivationCounts.Clear();
            TriggerErrorCounts.Clear();
            LastActivationTimes.Clear();
            TotalEvaluations = 0;
            TotalActivations = 0;
            TotalErrors = 0;
        }
    }
}