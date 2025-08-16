using System.Collections.Generic;
using UnityEngine;
using NeonQuest.Core.Examples;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Demonstration script showing how to set up and use the KiroAgentHooksManager
    /// This can be attached to a GameObject in the scene to test the hook system
    /// </summary>
    public class KiroHooksDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool autoStartDemo = true;
        [SerializeField] private float simulationInterval = 2.0f;
        [SerializeField] private bool useRealHooks = false;

        private KiroAgentHooksManager _hooksManager;
        private List<IKiroAgentHook> _demoHooks = new List<IKiroAgentHook>();
        private float _lastSimulationTime;
        private int _simulationStep = 0;

        private void Start()
        {
            SetupHooksManager();
            
            if (autoStartDemo)
            {
                StartDemo();
            }
        }

        private void Update()
        {
            if (autoStartDemo && Time.time - _lastSimulationTime > simulationInterval)
            {
                SimulatePlayerBehaviorEvent();
                _lastSimulationTime = Time.time;
            }
        }

        private void SetupHooksManager()
        {
            // Get or create the hooks manager
            _hooksManager = GetComponent<KiroAgentHooksManager>();
            if (_hooksManager == null)
            {
                _hooksManager = gameObject.AddComponent<KiroAgentHooksManager>();
            }

            // Subscribe to events for demonstration
            _hooksManager.OnHookRegistered += OnHookRegistered;
            _hooksManager.OnHookUnregistered += OnHookUnregistered;
            _hooksManager.OnHookExecuted += OnHookExecuted;
            _hooksManager.OnHookError += OnHookError;

            NeonQuestLogger.LogInfo("KiroAgentHooksManager setup complete");
        }

        [ContextMenu("Start Demo")]
        public void StartDemo()
        {
            RegisterDemoHooks();
            NeonQuestLogger.LogInfo("Kiro Hooks Demo started");
        }

        [ContextMenu("Stop Demo")]
        public void StopDemo()
        {
            UnregisterAllDemoHooks();
            NeonQuestLogger.LogInfo("Kiro Hooks Demo stopped");
        }

        [ContextMenu("Simulate Movement Event")]
        public void SimulateMovementEvent()
        {
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.MovementChanged,
                PlayerPosition = transform.position + Random.insideUnitSphere * 10f,
                Timestamp = Time.realtimeSinceStartup,
                BehaviorData = new Dictionary<string, object>
                {
                    ["speed"] = Random.Range(1f, 8f),
                    ["direction"] = Random.insideUnitSphere.normalized
                },
                EnvironmentState = new Dictionary<string, object>
                {
                    ["zone"] = "corridor",
                    ["lighting"] = Random.value > 0.5f ? "bright" : "dim"
                },
                Confidence = Random.Range(0.6f, 1.0f)
            };

            _ = _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.MovementChanged, eventData);
        }

        [ContextMenu("Simulate Intention Event")]
        public void SimulateIntentionEvent()
        {
            var intentions = new[] { "exploration", "backtracking", "searching", "resting" };
            var selectedIntention = intentions[Random.Range(0, intentions.Length)];

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.IntentionPredicted,
                PlayerPosition = transform.position,
                Timestamp = Time.realtimeSinceStartup,
                BehaviorData = new Dictionary<string, object>
                {
                    ["intention"] = selectedIntention,
                    ["previousIntention"] = intentions[Random.Range(0, intentions.Length)]
                },
                EnvironmentState = new Dictionary<string, object>
                {
                    ["zone"] = "junction",
                    ["pathOptions"] = Random.Range(2, 5)
                },
                Confidence = Random.Range(0.7f, 0.95f)
            };

            _ = _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.IntentionPredicted, eventData);
        }

        private void RegisterDemoHooks()
        {
            _demoHooks.Clear();

            if (useRealHooks)
            {
                // Register real environment response hook
                var environmentHook = new EnvironmentResponseHook();
                _demoHooks.Add(environmentHook);
                _hooksManager.RegisterHook(environmentHook);
            }
            else
            {
                // Register mock hooks for demonstration
                var movementHook = MockKiroAgentHook.CreateSuccessHook("demo-movement-hook", priority: 10);
                movementHook.HookName = "Demo Movement Hook";
                movementHook.MockResultData["demoType"] = "movement";
                _demoHooks.Add(movementHook);
                _hooksManager.RegisterHook(movementHook);

                var intentionHook = MockKiroAgentHook.CreateSuccessHook("demo-intention-hook", priority: 8);
                intentionHook.HookName = "Demo Intention Hook";
                intentionHook.MockResultData["demoType"] = "intention";
                _demoHooks.Add(intentionHook);
                _hooksManager.RegisterHook(intentionHook);

                var patternHook = MockKiroAgentHook.CreateSuccessHook("demo-pattern-hook", priority: 5);
                patternHook.HookName = "Demo Pattern Hook";
                patternHook.MockResultData["demoType"] = "pattern";
                _demoHooks.Add(patternHook);
                _hooksManager.RegisterHook(patternHook);

                // Add one hook that occasionally fails for demonstration
                var unreliableHook = new MockKiroAgentHook("demo-unreliable-hook", priority: 3)
                {
                    HookName = "Demo Unreliable Hook",
                    ShouldSimulateFailure = Random.value < 0.3f // 30% chance of failure
                };
                _demoHooks.Add(unreliableHook);
                _hooksManager.RegisterHook(unreliableHook);
            }

            NeonQuestLogger.LogInfo($"Registered {_demoHooks.Count} demo hooks");
        }

        private void UnregisterAllDemoHooks()
        {
            foreach (var hook in _demoHooks)
            {
                _hooksManager.UnregisterHook(hook.HookId);
            }
            _demoHooks.Clear();
        }

        private void SimulatePlayerBehaviorEvent()
        {
            _simulationStep++;

            switch (_simulationStep % 4)
            {
                case 0:
                    SimulateMovementEvent();
                    break;
                case 1:
                    SimulateIntentionEvent();
                    break;
                case 2:
                    SimulatePatternEvent();
                    break;
                case 3:
                    SimulateDwellTimeEvent();
                    break;
            }
        }

        private void SimulatePatternEvent()
        {
            var patterns = new[] { "rapid_exploration", "cautious_movement", "repetitive_path", "random_wandering" };
            var selectedPattern = patterns[Random.Range(0, patterns.Length)];

            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.PatternRecognized,
                PlayerPosition = transform.position,
                Timestamp = Time.realtimeSinceStartup,
                BehaviorData = new Dictionary<string, object>
                {
                    ["pattern"] = selectedPattern,
                    ["duration"] = Random.Range(5f, 30f),
                    ["consistency"] = Random.Range(0.5f, 1.0f)
                },
                EnvironmentState = new Dictionary<string, object>
                {
                    ["zone"] = "corridor",
                    ["complexity"] = Random.Range(0.3f, 0.8f)
                },
                Confidence = Random.Range(0.6f, 0.9f)
            };

            _ = _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.PatternRecognized, eventData);
        }

        private void SimulateDwellTimeEvent()
        {
            var eventData = new PlayerBehaviorEventData
            {
                EventType = PlayerBehaviorEventType.DwellTimeUpdated,
                PlayerPosition = transform.position,
                Timestamp = Time.realtimeSinceStartup,
                BehaviorData = new Dictionary<string, object>
                {
                    ["dwellTime"] = Random.Range(2f, 15f),
                    ["reason"] = Random.value > 0.5f ? "observing" : "deciding"
                },
                EnvironmentState = new Dictionary<string, object>
                {
                    ["zone"] = "junction",
                    ["interestLevel"] = Random.Range(0.2f, 0.9f)
                },
                Confidence = Random.Range(0.8f, 1.0f)
            };

            _ = _hooksManager.TriggerHooksAsync(PlayerBehaviorEventType.DwellTimeUpdated, eventData);
        }

        // Event handlers for demonstration
        private void OnHookRegistered(string hookId)
        {
            NeonQuestLogger.LogInfo($"[Demo] Hook registered: {hookId}");
        }

        private void OnHookUnregistered(string hookId)
        {
            NeonQuestLogger.LogInfo($"[Demo] Hook unregistered: {hookId}");
        }

        private void OnHookExecuted(string hookId, HookExecutionResult result)
        {
            var status = result.Success ? "SUCCESS" : "FAILED";
            var message = $"[Demo] Hook {hookId} executed: {status} ({result.ExecutionTimeMs:F1}ms)";
            
            if (result.Success && result.ResultData.Count > 0)
            {
                message += $" - Data: {string.Join(", ", result.ResultData.Keys)}";
            }
            else if (!result.Success)
            {
                message += $" - Error: {result.Message}";
            }

            NeonQuestLogger.LogInfo(message);
        }

        private void OnHookError(string hookId, System.Exception exception)
        {
            NeonQuestLogger.LogError($"[Demo] Hook {hookId} error: {exception.Message}");
        }

        private void OnDestroy()
        {
            StopDemo();
        }

        // Inspector methods for runtime testing
        [ContextMenu("Show Hook Stats")]
        public void ShowHookStats()
        {
            if (_hooksManager == null) return;

            var hookIds = _hooksManager.GetRegisteredHookIds();
            NeonQuestLogger.LogInfo($"=== Hook Statistics ({hookIds.Length} hooks) ===");

            foreach (var hookId in hookIds)
            {
                var stats = _hooksManager.GetHookStats(hookId);
                var successRate = stats.TotalExecutions > 0 ? (stats.SuccessRate * 100f).ToString("F1") : "N/A";
                
                NeonQuestLogger.LogInfo($"{hookId}: {stats.TotalExecutions} executions, " +
                    $"{successRate}% success rate, {stats.AverageExecutionTimeMs:F1}ms avg time");
            }
        }
    }
}