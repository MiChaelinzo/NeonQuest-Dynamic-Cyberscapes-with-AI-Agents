using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Monitors system performance and automatically adjusts generation complexity
    /// to maintain target framerate and resource usage thresholds
    /// </summary>
    public class PerformanceThrottler : MonoBehaviour
    {
        [Header("Performance Targets")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float minAcceptableFrameRate = 45f;
        [SerializeField] private float maxCpuUsagePercent = 80f;
        [SerializeField] private float maxMemoryUsageMB = 1024f;
        
        [Header("Monitoring Configuration")]
        [SerializeField] private float monitoringInterval = 0.5f;
        [SerializeField] private int performanceHistorySize = 20;
        [SerializeField] private float throttleResponseTime = 2f;
        
        [Header("Throttling Settings")]
        [SerializeField] private float qualityReductionStep = 0.1f;
        [SerializeField] private float qualityRecoveryStep = 0.05f;
        [SerializeField] private float emergencyThrottleThreshold = 30f;
        [SerializeField] private bool enableEmergencyThrottling = true;
        
        [Header("GPU Monitoring")]
        [SerializeField] private bool enableGpuMonitoring = true;
        [SerializeField] private float maxGpuMemoryUsageMB = 2048f;
        
        private Queue<PerformanceSnapshot> performanceHistory;
        private ProceduralGenerator proceduralGenerator;
        private List<IProceduralGenerator> monitoredSystems;
        private Coroutine monitoringCoroutine;
        
        // Performance state
        private float currentFrameRate;
        private float averageFrameRate;
        private float currentCpuUsage;
        private float currentMemoryUsage;
        private float currentGpuMemoryUsage;
        private float currentQualityLevel = 1f;
        private bool isThrottling = false;
        private float lastThrottleTime;
        
        // Throttling state
        private ThrottleLevel currentThrottleLevel = ThrottleLevel.None;
        private Dictionary<string, float> systemQualityLevels;
        
        public float CurrentFrameRate => currentFrameRate;
        public float AverageFrameRate => averageFrameRate;
        public float CurrentCpuUsage => currentCpuUsage;
        public float CurrentMemoryUsage => currentMemoryUsage;
        public float CurrentGpuMemoryUsage => currentGpuMemoryUsage;
        public float CurrentQualityLevel => currentQualityLevel;
        public bool IsThrottling => isThrottling;
        public ThrottleLevel CurrentThrottleLevel => currentThrottleLevel;
        
        private void Awake()
        {
            performanceHistory = new Queue<PerformanceSnapshot>();
            monitoredSystems = new List<IProceduralGenerator>();
            systemQualityLevels = new Dictionary<string, float>();
            
            // Set target frame rate
            Application.targetFrameRate = Mathf.RoundToInt(targetFrameRate);
        }
        
        private void Start()
        {
            InitializeMonitoring();
            StartPerformanceMonitoring();
        }
        
        private void OnDestroy()
        {
            StopPerformanceMonitoring();
        }
        
        private void InitializeMonitoring()
        {
            // Find procedural generator
            proceduralGenerator = FindObjectOfType<ProceduralGenerator>();
            if (proceduralGenerator == null)
            {
                Debug.LogWarning("PerformanceThrottler: ProceduralGenerator not found");
            }
            
            // Find all procedural generation systems
            var allGenerators = FindObjectsOfType<MonoBehaviour>()
                .OfType<IProceduralGenerator>()
                .ToList();
            
            foreach (var generator in allGenerators)
            {
                RegisterSystem(generator);
            }
            
            Debug.Log($"PerformanceThrottler: Monitoring {monitoredSystems.Count} systems");
        }
        
        public void RegisterSystem(IProceduralGenerator system)
        {
            if (system != null && !monitoredSystems.Contains(system))
            {
                monitoredSystems.Add(system);
                
                // Initialize quality level tracking
                string systemName = system.GetType().Name;
                systemQualityLevels[systemName] = 1f;
                
                Debug.Log($"PerformanceThrottler: Registered {systemName}");
            }
        }
        
        public void UnregisterSystem(IProceduralGenerator system)
        {
            if (system != null && monitoredSystems.Contains(system))
            {
                monitoredSystems.Remove(system);
                
                string systemName = system.GetType().Name;
                systemQualityLevels.Remove(systemName);
                
                Debug.Log($"PerformanceThrottler: Unregistered {systemName}");
            }
        }
        
        private void StartPerformanceMonitoring()
        {
            if (monitoringCoroutine != null)
            {
                StopCoroutine(monitoringCoroutine);
            }
            monitoringCoroutine = StartCoroutine(PerformanceMonitoringLoop());
        }
        
        private void StopPerformanceMonitoring()
        {
            if (monitoringCoroutine != null)
            {
                StopCoroutine(monitoringCoroutine);
                monitoringCoroutine = null;
            }
        }
        
        private IEnumerator PerformanceMonitoringLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(monitoringInterval);
                
                CollectPerformanceMetrics();
                AnalyzePerformance();
                ApplyThrottling();
            }
        }
        
        private void CollectPerformanceMetrics()
        {
            // Frame rate calculation
            currentFrameRate = 1f / Time.unscaledDeltaTime;
            
            // CPU usage (approximated from frame time)
            float frameTime = Time.unscaledDeltaTime;
            float targetFrameTime = 1f / targetFrameRate;
            currentCpuUsage = Mathf.Clamp01(frameTime / targetFrameTime) * 100f;
            
            // Memory usage
            currentMemoryUsage = Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f); // MB
            
            // GPU memory usage (if available)
            if (enableGpuMonitoring)
            {
                currentGpuMemoryUsage = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f); // MB
            }
            
            // Create performance snapshot
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = Time.time,
                FrameRate = currentFrameRate,
                CpuUsage = currentCpuUsage,
                MemoryUsage = currentMemoryUsage,
                GpuMemoryUsage = currentGpuMemoryUsage,
                QualityLevel = currentQualityLevel
            };
            
            // Add to history
            performanceHistory.Enqueue(snapshot);
            
            // Maintain history size
            while (performanceHistory.Count > performanceHistorySize)
            {
                performanceHistory.Dequeue();
            }
            
            // Calculate average frame rate
            if (performanceHistory.Count > 0)
            {
                averageFrameRate = performanceHistory.Average(s => s.FrameRate);
            }
        }
        
        private void AnalyzePerformance()
        {
            if (performanceHistory.Count < 3) return; // Need some history for analysis
            
            // Check for performance issues
            bool frameRateIssue = averageFrameRate < minAcceptableFrameRate;
            bool cpuIssue = currentCpuUsage > maxCpuUsagePercent;
            bool memoryIssue = currentMemoryUsage > maxMemoryUsageMB;
            bool gpuMemoryIssue = enableGpuMonitoring && currentGpuMemoryUsage > maxGpuMemoryUsageMB;
            
            // Emergency throttling check
            bool emergencyCondition = currentFrameRate < emergencyThrottleThreshold;
            
            // Determine throttle level
            ThrottleLevel newThrottleLevel = ThrottleLevel.None;
            
            if (emergencyCondition && enableEmergencyThrottling)
            {
                newThrottleLevel = ThrottleLevel.Emergency;
            }
            else if ((frameRateIssue && cpuIssue) || memoryIssue || gpuMemoryIssue)
            {
                newThrottleLevel = ThrottleLevel.Heavy;
            }
            else if (frameRateIssue || cpuIssue)
            {
                newThrottleLevel = ThrottleLevel.Moderate;
            }
            else if (averageFrameRate < targetFrameRate * 0.9f)
            {
                newThrottleLevel = ThrottleLevel.Light;
            }
            
            // Update throttle level
            if (newThrottleLevel != currentThrottleLevel)
            {
                currentThrottleLevel = newThrottleLevel;
                lastThrottleTime = Time.time;
                
                Debug.Log($"PerformanceThrottler: Throttle level changed to {currentThrottleLevel}");
            }
        }
        
        private void ApplyThrottling()
        {
            bool shouldThrottle = currentThrottleLevel != ThrottleLevel.None;
            
            if (shouldThrottle)
            {
                ApplyPerformanceReduction();
                isThrottling = true;
            }
            else if (isThrottling && CanRecoverPerformance())
            {
                ApplyPerformanceRecovery();
            }
        }
        
        private void ApplyPerformanceReduction()
        {
            float targetQuality = CalculateTargetQuality();
            
            if (targetQuality < currentQualityLevel)
            {
                currentQualityLevel = Mathf.Max(0.1f, targetQuality);
                ApplyQualityToSystems(currentQualityLevel);
                
                Debug.Log($"PerformanceThrottler: Reducing quality to {currentQualityLevel:F2}");
            }
        }
        
        private void ApplyPerformanceRecovery()
        {
            // Gradually increase quality if performance is stable
            float targetQuality = Mathf.Min(1f, currentQualityLevel + qualityRecoveryStep);
            
            if (targetQuality > currentQualityLevel)
            {
                currentQualityLevel = targetQuality;
                ApplyQualityToSystems(currentQualityLevel);
                
                Debug.Log($"PerformanceThrottler: Recovering quality to {currentQualityLevel:F2}");
                
                // Stop throttling if we've recovered to full quality
                if (currentQualityLevel >= 1f)
                {
                    isThrottling = false;
                }
            }
        }
        
        private float CalculateTargetQuality()
        {
            switch (currentThrottleLevel)
            {
                case ThrottleLevel.Light:
                    return currentQualityLevel - qualityReductionStep * 0.5f;
                    
                case ThrottleLevel.Moderate:
                    return currentQualityLevel - qualityReductionStep;
                    
                case ThrottleLevel.Heavy:
                    return currentQualityLevel - qualityReductionStep * 2f;
                    
                case ThrottleLevel.Emergency:
                    return 0.1f; // Minimum quality
                    
                default:
                    return currentQualityLevel;
            }
        }
        
        private void ApplyQualityToSystems(float qualityLevel)
        {
            // Apply to procedural generator (which will propagate to its systems)
            if (proceduralGenerator != null)
            {
                proceduralGenerator.SetQualityLevel(qualityLevel);
            }
            
            // Apply to individual monitored systems
            foreach (var system in monitoredSystems)
            {
                if (system != null && system.IsActive)
                {
                    system.SetQualityLevel(qualityLevel);
                    
                    string systemName = system.GetType().Name;
                    systemQualityLevels[systemName] = qualityLevel;
                }
            }
        }
        
        private bool CanRecoverPerformance()
        {
            // Only recover if performance has been stable for a while
            float timeSinceLastThrottle = Time.time - lastThrottleTime;
            if (timeSinceLastThrottle < throttleResponseTime)
                return false;
            
            // Check if performance metrics are good enough for recovery
            bool frameRateGood = averageFrameRate > targetFrameRate * 0.95f;
            bool cpuGood = currentCpuUsage < maxCpuUsagePercent * 0.8f;
            bool memoryGood = currentMemoryUsage < maxMemoryUsageMB * 0.8f;
            bool gpuMemoryGood = !enableGpuMonitoring || currentGpuMemoryUsage < maxGpuMemoryUsageMB * 0.8f;
            
            return frameRateGood && cpuGood && memoryGood && gpuMemoryGood;
        }
        
        /// <summary>
        /// Manually trigger emergency throttling
        /// </summary>
        public void TriggerEmergencyThrottle()
        {
            currentThrottleLevel = ThrottleLevel.Emergency;
            lastThrottleTime = Time.time;
            ApplyThrottling();
            
            Debug.LogWarning("PerformanceThrottler: Emergency throttling triggered manually");
        }
        
        /// <summary>
        /// Reset throttling and restore full quality
        /// </summary>
        public void ResetThrottling()
        {
            currentThrottleLevel = ThrottleLevel.None;
            currentQualityLevel = 1f;
            isThrottling = false;
            
            ApplyQualityToSystems(1f);
            
            Debug.Log("PerformanceThrottler: Throttling reset, quality restored to maximum");
        }
        
        /// <summary>
        /// Get current performance statistics
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                CurrentFrameRate = currentFrameRate,
                AverageFrameRate = averageFrameRate,
                TargetFrameRate = targetFrameRate,
                CurrentCpuUsage = currentCpuUsage,
                MaxCpuUsage = maxCpuUsagePercent,
                CurrentMemoryUsage = currentMemoryUsage,
                MaxMemoryUsage = maxMemoryUsageMB,
                CurrentGpuMemoryUsage = currentGpuMemoryUsage,
                MaxGpuMemoryUsage = maxGpuMemoryUsageMB,
                CurrentQualityLevel = currentQualityLevel,
                ThrottleLevel = currentThrottleLevel,
                IsThrottling = isThrottling,
                MonitoredSystemsCount = monitoredSystems.Count,
                PerformanceHistoryCount = performanceHistory.Count
            };
        }
        
        /// <summary>
        /// Get performance history for analysis
        /// </summary>
        public List<PerformanceSnapshot> GetPerformanceHistory()
        {
            return performanceHistory.ToList();
        }
        
        /// <summary>
        /// Get quality levels for all monitored systems
        /// </summary>
        public Dictionary<string, float> GetSystemQualityLevels()
        {
            return new Dictionary<string, float>(systemQualityLevels);
        }
        
        /// <summary>
        /// Set custom performance thresholds
        /// </summary>
        public void SetPerformanceThresholds(float targetFps, float minFps, float maxCpu, float maxMemory)
        {
            targetFrameRate = targetFps;
            minAcceptableFrameRate = minFps;
            maxCpuUsagePercent = maxCpu;
            maxMemoryUsageMB = maxMemory;
            
            Application.targetFrameRate = Mathf.RoundToInt(targetFrameRate);
            
            Debug.Log($"PerformanceThrottler: Updated thresholds - Target FPS: {targetFps}, Min FPS: {minFps}, Max CPU: {maxCpu}%, Max Memory: {maxMemory}MB");
        }
        
        /// <summary>
        /// Enable or disable specific throttling features
        /// </summary>
        public void SetThrottlingOptions(bool enableEmergency, bool enableGpu, float responseTime)
        {
            enableEmergencyThrottling = enableEmergency;
            enableGpuMonitoring = enableGpu;
            throttleResponseTime = responseTime;
            
            Debug.Log($"PerformanceThrottler: Updated options - Emergency: {enableEmergency}, GPU: {enableGpu}, Response Time: {responseTime}s");
        }
        
        /// <summary>
        /// Get diagnostic information for debugging
        /// </summary>
        public Dictionary<string, object> GetDiagnosticInfo()
        {
            var recentSnapshots = performanceHistory.TakeLast(5).ToList();
            
            return new Dictionary<string, object>
            {
                ["CurrentFrameRate"] = currentFrameRate,
                ["AverageFrameRate"] = averageFrameRate,
                ["TargetFrameRate"] = targetFrameRate,
                ["CurrentCpuUsage"] = currentCpuUsage,
                ["CurrentMemoryUsage"] = currentMemoryUsage,
                ["CurrentGpuMemoryUsage"] = currentGpuMemoryUsage,
                ["CurrentQualityLevel"] = currentQualityLevel,
                ["ThrottleLevel"] = currentThrottleLevel.ToString(),
                ["IsThrottling"] = isThrottling,
                ["MonitoredSystems"] = monitoredSystems.Count,
                ["RecentPerformance"] = recentSnapshots.Select(s => new {
                    s.Timestamp,
                    s.FrameRate,
                    s.CpuUsage,
                    s.MemoryUsage
                }).ToList(),
                ["SystemQualityLevels"] = systemQualityLevels
            };
        }
    }
    
    /// <summary>
    /// Represents a snapshot of performance metrics at a specific time
    /// </summary>
    [System.Serializable]
    public class PerformanceSnapshot
    {
        public float Timestamp;
        public float FrameRate;
        public float CpuUsage;
        public float MemoryUsage;
        public float GpuMemoryUsage;
        public float QualityLevel;
    }
    
    /// <summary>
    /// Current performance statistics
    /// </summary>
    [System.Serializable]
    public class PerformanceStats
    {
        public float CurrentFrameRate;
        public float AverageFrameRate;
        public float TargetFrameRate;
        public float CurrentCpuUsage;
        public float MaxCpuUsage;
        public float CurrentMemoryUsage;
        public float MaxMemoryUsage;
        public float CurrentGpuMemoryUsage;
        public float MaxGpuMemoryUsage;
        public float CurrentQualityLevel;
        public ThrottleLevel ThrottleLevel;
        public bool IsThrottling;
        public int MonitoredSystemsCount;
        public int PerformanceHistoryCount;
    }
    
    /// <summary>
    /// Levels of performance throttling
    /// </summary>
    public enum ThrottleLevel
    {
        None,       // No throttling needed
        Light,      // Minor performance adjustments
        Moderate,   // Noticeable quality reduction
        Heavy,      // Significant quality reduction
        Emergency   // Minimum quality to maintain stability
    }
}