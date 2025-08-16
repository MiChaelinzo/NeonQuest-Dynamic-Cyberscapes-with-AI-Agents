using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Diagnostics
{
    /// <summary>
    /// Comprehensive system diagnostics and health monitoring
    /// </summary>
    public class SystemDiagnostics : MonoBehaviour
    {
        [System.Serializable]
        public class SystemHealth
        {
            public string ComponentName { get; set; }
            public HealthStatus Status { get; set; }
            public string StatusMessage { get; set; }
            public DateTime LastCheck { get; set; }
            public float ResponseTime { get; set; }
        }

        public enum HealthStatus
        {
            Healthy,
            Warning,
            Critical,
            Unknown
        }

        [SerializeField] private float _healthCheckInterval = 5.0f;
        [SerializeField] private bool _enableAutomaticHealthChecks = true;
        [SerializeField] private bool _enablePerformanceRegression = true;

        private Dictionary<string, SystemHealth> _componentHealth = new Dictionary<string, SystemHealth>();
        private PerformanceMonitor _performanceMonitor;
        private float _lastHealthCheckTime;
        private List<float> _baselineFrameRates = new List<float>();
        private bool _baselineEstablished = false;

        public event Action<SystemHealth> OnComponentHealthChanged;
        public event Action<Dictionary<string, SystemHealth>> OnSystemHealthUpdated;

        public IReadOnlyDictionary<string, SystemHealth> ComponentHealth => _componentHealth;
        public HealthStatus OverallSystemHealth { get; private set; } = HealthStatus.Unknown;

        private void Start()
        {
            _performanceMonitor = GetComponent<PerformanceMonitor>();
            if (_performanceMonitor == null)
            {
                _performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            }

            if (_enablePerformanceRegression)
            {
                _performanceMonitor.OnMetricsUpdated += CollectBaselineData;
                _performanceMonitor.OnPerformanceRegressionDetected += HandlePerformanceRegression;
            }

            InitializeComponentHealth();
            NeonQuestLogger.LogInfo("System diagnostics initialized", NeonQuestLogger.LogCategory.Performance, this);
        }

        private void Update()
        {
            if (_enableAutomaticHealthChecks && Time.time - _lastHealthCheckTime >= _healthCheckInterval)
            {
                PerformHealthCheck();
                _lastHealthCheckTime = Time.time;
            }
        }

        private void InitializeComponentHealth()
        {
            // Initialize health status for core components
            RegisterComponent("PerformanceMonitor");
            RegisterComponent("AssetLoader");
            RegisterComponent("ProceduralGenerator");
            RegisterComponent("PlayerBehaviorTracker");
            RegisterComponent("ConfigurationSystem");
            RegisterComponent("AgentHooks");
        }

        /// <summary>
        /// Register a component for health monitoring
        /// </summary>
        public void RegisterComponent(string componentName)
        {
            if (!_componentHealth.ContainsKey(componentName))
            {
                _componentHealth[componentName] = new SystemHealth
                {
                    ComponentName = componentName,
                    Status = HealthStatus.Unknown,
                    StatusMessage = "Not checked",
                    LastCheck = DateTime.Now,
                    ResponseTime = 0f
                };

                NeonQuestLogger.LogDebug($"Registered component for health monitoring: {componentName}", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        /// <summary>
        /// Update health status for a specific component
        /// </summary>
        public void UpdateComponentHealth(string componentName, HealthStatus status, string message = "", float responseTime = 0f)
        {
            if (!_componentHealth.ContainsKey(componentName))
            {
                RegisterComponent(componentName);
            }

            var previousStatus = _componentHealth[componentName].Status;
            
            _componentHealth[componentName].Status = status;
            _componentHealth[componentName].StatusMessage = message;
            _componentHealth[componentName].LastCheck = DateTime.Now;
            _componentHealth[componentName].ResponseTime = responseTime;

            if (previousStatus != status)
            {
                OnComponentHealthChanged?.Invoke(_componentHealth[componentName]);
                
                NeonQuestLogger.LogInfo($"Component health changed: {componentName} -> {status} ({message})", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }

            UpdateOverallSystemHealth();
        }

        /// <summary>
        /// Perform comprehensive health check on all registered components
        /// </summary>
        public void PerformHealthCheck()
        {
            float startTime = Time.realtimeSinceStartup;

            CheckPerformanceMonitorHealth();
            CheckMemoryHealth();
            CheckFrameRateHealth();
            CheckComponentResponsiveness();

            float totalCheckTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            OnSystemHealthUpdated?.Invoke(_componentHealth);
            
            NeonQuestLogger.LogDebug($"Health check completed in {totalCheckTime:F2}ms", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void CheckPerformanceMonitorHealth()
        {
            if (_performanceMonitor != null && _performanceMonitor.IsMonitoring)
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                if (metrics != null)
                {
                    HealthStatus status = HealthStatus.Healthy;
                    string message = "Operating normally";

                    if (metrics.FrameRate < 30f)
                    {
                        status = HealthStatus.Critical;
                        message = $"Critical frame rate: {metrics.FrameRate:F1} FPS";
                    }
                    else if (metrics.FrameRate < 45f)
                    {
                        status = HealthStatus.Warning;
                        message = $"Low frame rate: {metrics.FrameRate:F1} FPS";
                    }

                    UpdateComponentHealth("PerformanceMonitor", status, message);
                }
                else
                {
                    UpdateComponentHealth("PerformanceMonitor", HealthStatus.Warning, "No metrics available");
                }
            }
            else
            {
                UpdateComponentHealth("PerformanceMonitor", HealthStatus.Critical, "Not running or disabled");
            }
        }

        private void CheckMemoryHealth()
        {
            long memoryUsage = GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / 1024f / 1024f;

            HealthStatus status = HealthStatus.Healthy;
            string message = $"Memory usage: {memoryMB:F1}MB";

            if (memoryMB > 1024f) // 1GB
            {
                status = HealthStatus.Critical;
                message = $"High memory usage: {memoryMB:F1}MB";
            }
            else if (memoryMB > 512f) // 512MB
            {
                status = HealthStatus.Warning;
                message = $"Elevated memory usage: {memoryMB:F1}MB";
            }

            UpdateComponentHealth("MemorySystem", status, message);
        }

        private void CheckFrameRateHealth()
        {
            if (_performanceMonitor?.CurrentMetrics != null)
            {
                float currentFPS = _performanceMonitor.CurrentMetrics.FrameRate;
                
                HealthStatus status = HealthStatus.Healthy;
                string message = $"Frame rate: {currentFPS:F1} FPS";

                if (currentFPS < 20f)
                {
                    status = HealthStatus.Critical;
                    message = $"Critical frame rate: {currentFPS:F1} FPS";
                }
                else if (currentFPS < 30f)
                {
                    status = HealthStatus.Warning;
                    message = $"Low frame rate: {currentFPS:F1} FPS";
                }

                UpdateComponentHealth("FrameRate", status, message);
            }
        }

        private void CheckComponentResponsiveness()
        {
            // Check if components are responding within acceptable time limits
            foreach (var component in _componentHealth.Keys.ToList())
            {
                var health = _componentHealth[component];
                var timeSinceLastCheck = DateTime.Now - health.LastCheck;

                if (timeSinceLastCheck.TotalMinutes > 5) // No update in 5 minutes
                {
                    UpdateComponentHealth(component, HealthStatus.Warning, 
                        $"No updates for {timeSinceLastCheck.TotalMinutes:F1} minutes");
                }
            }
        }

        private void CollectBaselineData(PerformanceMonitor.PerformanceMetrics metrics)
        {
            if (!_baselineEstablished)
            {
                _baselineFrameRates.Add(metrics.FrameRate);
                
                if (_baselineFrameRates.Count >= 60) // Collect 60 samples for baseline
                {
                    EstablishBaseline();
                }
            }
        }

        private void EstablishBaseline()
        {
            if (_baselineFrameRates.Count > 0)
            {
                float averageBaseline = _baselineFrameRates.Average();
                _baselineEstablished = true;
                
                NeonQuestLogger.LogInfo($"Performance baseline established: {averageBaseline:F1} FPS", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void HandlePerformanceRegression()
        {
            UpdateComponentHealth("PerformanceRegression", HealthStatus.Critical, 
                "Performance regression detected");
        }

        private void UpdateOverallSystemHealth()
        {
            var healthStatuses = _componentHealth.Values.Select(h => h.Status).ToList();
            
            if (healthStatuses.Any(s => s == HealthStatus.Critical))
            {
                OverallSystemHealth = HealthStatus.Critical;
            }
            else if (healthStatuses.Any(s => s == HealthStatus.Warning))
            {
                OverallSystemHealth = HealthStatus.Warning;
            }
            else if (healthStatuses.All(s => s == HealthStatus.Healthy))
            {
                OverallSystemHealth = HealthStatus.Healthy;
            }
            else
            {
                OverallSystemHealth = HealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Get system health report as formatted string
        /// </summary>
        public string GetHealthReport()
        {
            var report = $"Overall System Health: {OverallSystemHealth}\n";
            report += $"Last Check: {DateTime.Now:HH:mm:ss}\n\n";

            foreach (var health in _componentHealth.Values)
            {
                string statusIcon = health.Status switch
                {
                    HealthStatus.Healthy => "✓",
                    HealthStatus.Warning => "⚠",
                    HealthStatus.Critical => "✗",
                    _ => "?"
                };

                report += $"{statusIcon} {health.ComponentName}: {health.Status}\n";
                if (!string.IsNullOrEmpty(health.StatusMessage))
                {
                    report += $"   {health.StatusMessage}\n";
                }
            }

            return report;
        }

        /// <summary>
        /// Force a manual health check
        /// </summary>
        public void ForceHealthCheck()
        {
            PerformHealthCheck();
            NeonQuestLogger.LogInfo("Manual health check performed", NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Reset all component health statuses
        /// </summary>
        public void ResetHealthStatuses()
        {
            foreach (var componentName in _componentHealth.Keys.ToList())
            {
                UpdateComponentHealth(componentName, HealthStatus.Unknown, "Reset");
            }
            
            NeonQuestLogger.LogInfo("All health statuses reset", NeonQuestLogger.LogCategory.Performance, this);
        }

        private void OnDestroy()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated -= CollectBaselineData;
                _performanceMonitor.OnPerformanceRegressionDetected -= HandlePerformanceRegression;
            }
        }
    }
}