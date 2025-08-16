using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;
using System.Threading;

namespace NeonQuest.Core
{
    /// <summary>
    /// Manages registration, lifecycle, and execution of Kiro agent hooks
    /// </summary>
    public class KiroAgentHooksManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxConcurrentHooks = 5;
        [SerializeField] private int defaultTimeoutMs = 5000;
        [SerializeField] private int maxRetryAttempts = 3;
        [SerializeField] private float retryDelayMs = 1000f;
        [SerializeField] private bool enablePerformanceThrottling = true;
        [SerializeField] private float maxFrameTimeMs = 16.67f; // 60 FPS target

        private readonly Dictionary<string, IKiroAgentHook> _registeredHooks = new Dictionary<string, IKiroAgentHook>();
        private readonly Dictionary<string, HookExecutionStats> _hookStats = new Dictionary<string, HookExecutionStats>();
        private readonly Queue<HookExecutionRequest> _executionQueue = new Queue<HookExecutionRequest>();
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly object _lockObject = new object();

        private bool _isProcessingQueue = false;
        private CancellationTokenSource _cancellationTokenSource;

        public event Action<string, HookExecutionResult> OnHookExecuted;
        public event Action<string, Exception> OnHookError;
        public event Action<string> OnHookRegistered;
        public event Action<string> OnHookUnregistered;

        public int RegisteredHookCount => _registeredHooks.Count;
        public int QueuedExecutionCount => _executionQueue.Count;
        public bool IsProcessingQueue => _isProcessingQueue;

        private void Awake()
        {
            _executionSemaphore = new SemaphoreSlim(maxConcurrentHooks, maxConcurrentHooks);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void Start()
        {
            StartQueueProcessing();
        }

        private void OnDestroy()
        {
            StopQueueProcessing();
            UnregisterAllHooks();
            _executionSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Registers a new agent hook
        /// </summary>
        public bool RegisterHook(IKiroAgentHook hook)
        {
            if (hook == null)
            {
                NeonQuestLogger.LogError("Cannot register null hook");
                return false;
            }

            if (string.IsNullOrEmpty(hook.HookId))
            {
                NeonQuestLogger.LogError($"Hook {hook.HookName} has invalid HookId");
                return false;
            }

            lock (_lockObject)
            {
                if (_registeredHooks.ContainsKey(hook.HookId))
                {
                    NeonQuestLogger.LogWarning($"Hook with ID {hook.HookId} is already registered");
                    return false;
                }

                try
                {
                    _registeredHooks[hook.HookId] = hook;
                    _hookStats[hook.HookId] = new HookExecutionStats();
                    
                    hook.OnRegistered();
                    OnHookRegistered?.Invoke(hook.HookId);
                    
                    NeonQuestLogger.LogInfo($"Successfully registered hook: {hook.HookName} ({hook.HookId})");
                    return true;
                }
                catch (Exception ex)
                {
                    NeonQuestLogger.LogError($"Failed to register hook {hook.HookId}: {ex.Message}");
                    _registeredHooks.Remove(hook.HookId);
                    _hookStats.Remove(hook.HookId);
                    return false;
                }
            }
        }

        /// <summary>
        /// Unregisters an agent hook
        /// </summary>
        public bool UnregisterHook(string hookId)
        {
            if (string.IsNullOrEmpty(hookId))
            {
                NeonQuestLogger.LogError("Cannot unregister hook with null or empty ID");
                return false;
            }

            lock (_lockObject)
            {
                if (!_registeredHooks.TryGetValue(hookId, out var hook))
                {
                    NeonQuestLogger.LogWarning($"Hook with ID {hookId} is not registered");
                    return false;
                }

                try
                {
                    hook.OnUnregistered();
                    _registeredHooks.Remove(hookId);
                    _hookStats.Remove(hookId);
                    
                    OnHookUnregistered?.Invoke(hookId);
                    NeonQuestLogger.LogInfo($"Successfully unregistered hook: {hookId}");
                    return true;
                }
                catch (Exception ex)
                {
                    NeonQuestLogger.LogError($"Error during hook unregistration {hookId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unregisters all hooks
        /// </summary>
        public void UnregisterAllHooks()
        {
            lock (_lockObject)
            {
                var hookIds = _registeredHooks.Keys.ToList();
                foreach (var hookId in hookIds)
                {
                    UnregisterHook(hookId);
                }
            }
        }

        /// <summary>
        /// Triggers hooks for a specific player behavior event
        /// </summary>
        public async Task TriggerHooksAsync(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            var applicableHooks = GetApplicableHooks(eventType, eventData);
            
            if (applicableHooks.Count == 0)
            {
                return;
            }

            // Sort by priority (higher priority first)
            applicableHooks.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            var tasks = new List<Task>();
            foreach (var hook in applicableHooks)
            {
                if (enablePerformanceThrottling && ShouldThrottleExecution())
                {
                    QueueHookExecution(hook, eventType, eventData);
                }
                else
                {
                    tasks.Add(ExecuteHookWithRetryAsync(hook, eventType, eventData));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Gets execution statistics for a hook
        /// </summary>
        public HookExecutionStats GetHookStats(string hookId)
        {
            lock (_lockObject)
            {
                return _hookStats.TryGetValue(hookId, out var stats) ? stats : new HookExecutionStats();
            }
        }

        /// <summary>
        /// Gets all registered hook IDs
        /// </summary>
        public string[] GetRegisteredHookIds()
        {
            lock (_lockObject)
            {
                return _registeredHooks.Keys.ToArray();
            }
        }

        /// <summary>
        /// Gets a registered hook by ID
        /// </summary>
        public IKiroAgentHook GetHook(string hookId)
        {
            lock (_lockObject)
            {
                return _registeredHooks.TryGetValue(hookId, out var hook) ? hook : null;
            }
        }

        private List<IKiroAgentHook> GetApplicableHooks(PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            var applicableHooks = new List<IKiroAgentHook>();
            
            lock (_lockObject)
            {
                foreach (var hook in _registeredHooks.Values)
                {
                    if (!hook.IsEnabled)
                        continue;

                    if (!hook.SupportedEventTypes.Contains(eventType))
                        continue;

                    try
                    {
                        if (hook.ShouldExecute(eventType, eventData))
                        {
                            applicableHooks.Add(hook);
                        }
                    }
                    catch (Exception ex)
                    {
                        NeonQuestLogger.LogError($"Error in ShouldExecute for hook {hook.HookId}: {ex.Message}");
                        UpdateHookStats(hook.HookId, false, 0f, ex.Message);
                    }
                }
            }

            return applicableHooks;
        }

        private async Task ExecuteHookWithRetryAsync(IKiroAgentHook hook, PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            await _executionSemaphore.WaitAsync(_cancellationTokenSource.Token);
            
            try
            {
                var result = await ExecuteHookWithRetryLogic(hook, eventType, eventData);
                OnHookExecuted?.Invoke(hook.HookId, result);
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        private async Task<HookExecutionResult> ExecuteHookWithRetryLogic(IKiroAgentHook hook, PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            var startTime = Time.realtimeSinceStartup;
            Exception lastException = null;

            for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
            {
                try
                {
                    using (var timeoutCts = new CancellationTokenSource(hook.TimeoutMs > 0 ? hook.TimeoutMs : defaultTimeoutMs))
                    using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, timeoutCts.Token))
                    {
                        var executionTask = hook.ExecuteAsync(eventType, eventData);
                        var result = await executionTask.ConfigureAwait(false);
                        
                        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                        result.ExecutionTimeMs = executionTime;
                        
                        UpdateHookStats(hook.HookId, result.Success, executionTime, result.ErrorDetails);
                        return result;
                    }
                }
                catch (OperationCanceledException)
                {
                    var timeoutMessage = $"Hook {hook.HookId} timed out after {hook.TimeoutMs}ms";
                    NeonQuestLogger.LogWarning(timeoutMessage);
                    lastException = new TimeoutException(timeoutMessage);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    NeonQuestLogger.LogError($"Hook {hook.HookId} execution failed (attempt {attempt + 1}): {ex.Message}");
                }

                if (attempt < maxRetryAttempts - 1)
                {
                    await Task.Delay((int)(retryDelayMs * Math.Pow(2, attempt)), _cancellationTokenSource.Token);
                }
            }

            var totalExecutionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            var failureResult = HookExecutionResult.CreateFailure(
                $"Hook failed after {maxRetryAttempts} attempts", 
                lastException?.Message ?? "Unknown error", 
                totalExecutionTime);
            
            UpdateHookStats(hook.HookId, false, totalExecutionTime, lastException?.Message);
            OnHookError?.Invoke(hook.HookId, lastException);
            
            return failureResult;
        }

        private void QueueHookExecution(IKiroAgentHook hook, PlayerBehaviorEventType eventType, PlayerBehaviorEventData eventData)
        {
            lock (_lockObject)
            {
                _executionQueue.Enqueue(new HookExecutionRequest
                {
                    Hook = hook,
                    EventType = eventType,
                    EventData = eventData,
                    QueuedTime = Time.realtimeSinceStartup
                });
            }
        }

        private bool ShouldThrottleExecution()
        {
            return Time.deltaTime * 1000f > maxFrameTimeMs;
        }

        private void StartQueueProcessing()
        {
            if (_isProcessingQueue)
                return;

            _isProcessingQueue = true;
            _ = ProcessExecutionQueueAsync();
        }

        private void StopQueueProcessing()
        {
            _isProcessingQueue = false;
            _cancellationTokenSource?.Cancel();
        }

        private async Task ProcessExecutionQueueAsync()
        {
            while (_isProcessingQueue && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    HookExecutionRequest request = default;
                    bool hasRequest = false;

                    lock (_lockObject)
                    {
                        if (_executionQueue.Count > 0)
                        {
                            request = _executionQueue.Dequeue();
                            hasRequest = true;
                        }
                    }

                    if (hasRequest)
                    {
                        await ExecuteHookWithRetryAsync(request.Hook, request.EventType, request.EventData);
                    }
                    else
                    {
                        await Task.Delay(100, _cancellationTokenSource.Token); // Wait before checking queue again
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    NeonQuestLogger.LogError($"Error in queue processing: {ex.Message}");
                    await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before retrying
                }
            }
        }

        private void UpdateHookStats(string hookId, bool success, float executionTimeMs, string errorMessage = null)
        {
            lock (_lockObject)
            {
                if (_hookStats.TryGetValue(hookId, out var stats))
                {
                    stats.TotalExecutions++;
                    stats.TotalExecutionTimeMs += executionTimeMs;
                    stats.AverageExecutionTimeMs = stats.TotalExecutionTimeMs / stats.TotalExecutions;

                    if (success)
                    {
                        stats.SuccessfulExecutions++;
                    }
                    else
                    {
                        stats.FailedExecutions++;
                        stats.LastErrorMessage = errorMessage;
                        stats.LastErrorTime = Time.realtimeSinceStartup;
                    }

                    stats.SuccessRate = (float)stats.SuccessfulExecutions / stats.TotalExecutions;
                }
            }
        }

        private struct HookExecutionRequest
        {
            public IKiroAgentHook Hook;
            public PlayerBehaviorEventType EventType;
            public PlayerBehaviorEventData EventData;
            public float QueuedTime;
        }
    }

    /// <summary>
    /// Statistics for hook execution performance
    /// </summary>
    [Serializable]
    public class HookExecutionStats
    {
        public int TotalExecutions;
        public int SuccessfulExecutions;
        public int FailedExecutions;
        public float SuccessRate;
        public float TotalExecutionTimeMs;
        public float AverageExecutionTimeMs;
        public string LastErrorMessage;
        public float LastErrorTime;
    }
}