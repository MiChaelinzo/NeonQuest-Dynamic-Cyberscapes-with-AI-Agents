using System;
using System.Collections.Generic;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Diagnostics
{
    /// <summary>
    /// Monitors system performance and collects metrics for analysis
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [System.Serializable]
        public class PerformanceMetrics
        {
            public float FrameRate { get; set; }
            public float FrameTime { get; set; }
            public float CpuTime { get; set; }
            public float GpuTime { get; set; }
            public long MemoryUsage { get; set; }
            public int ActiveGameObjects { get; set; }
            public int GeneratedObjectsCount { get; set; }
            public float GenerationTime { get; set; }
            public DateTime Timestamp { get; set; }
        }

        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _sampleInterval = 1.0f;
        [SerializeField] private int _maxSampleHistory = 300; // 5 minutes at 1 second intervals
        [SerializeField] private float _performanceWarningThreshold = 45.0f; // FPS
        [SerializeField] private float _performanceCriticalThreshold = 30.0f; // FPS

        private Queue<PerformanceMetrics> _metricsHistory = new Queue<PerformanceMetrics>();
        private float _lastSampleTime;
        private float _frameTimeAccumulator;
        private int _frameCount;
        
        // Performance regression detection
        private float _baselineFrameRate = 60.0f;
        private bool _regressionDetected = false;
        private int _consecutivePoorFrames = 0;
        private const int REGRESSION_FRAME_THRESHOLD = 30;

        public event Action<PerformanceMetrics> OnMetricsUpdated;
        public event Action<PerformanceMetrics> OnPerformanceWarning;
        public event Action<PerformanceMetrics> OnPerformanceCritical;
        public event Action OnPerformanceRegressionDetected;

        public PerformanceMetrics CurrentMetrics { get; private set; }
        public bool IsMonitoring => _enableMonitoring;
        public IReadOnlyCollection<PerformanceMetrics> MetricsHistory => _metricsHistory;

        private void Start()
        {
            if (_enableMonitoring)
            {
                _lastSampleTime = Time.time;
                NeonQuestLogger.LogInfo("Performance monitoring started", NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void Update()
        {
            if (!_enableMonitoring) return;

            UpdateFrameMetrics();

            if (Time.time - _lastSampleTime >= _sampleInterval)
            {
                CollectMetrics();
                _lastSampleTime = Time.time;
            }
        }

        private void UpdateFrameMetrics()
        {
            _frameTimeAccumulator += Time.unscaledDeltaTime;
            _frameCount++;

            // Check for performance issues on a per-frame basis
            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            
            if (currentFPS < _performanceCriticalThreshold)
            {
                _consecutivePoorFrames++;
                if (_consecutivePoorFrames >= REGRESSION_FRAME_THRESHOLD && !_regressionDetected)
                {
                    DetectPerformanceRegression();
                }
            }
            else
            {
                _consecutivePoorFrames = 0;
            }
        }

        private void CollectMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                FrameRate = _frameCount / _frameTimeAccumulator,
                FrameTime = (_frameTimeAccumulator / _frameCount) * 1000f, // Convert to milliseconds
                CpuTime = Time.realtimeSinceStartup,
                MemoryUsage = GC.GetTotalMemory(false),
                ActiveGameObjects = FindObjectsOfType<GameObject>().Length,
                Timestamp = DateTime.Now
            };

            // Reset accumulators
            _frameTimeAccumulator = 0f;
            _frameCount = 0;

            // Add GPU time if available (Unity 2019.1+)
            #if UNITY_2019_1_OR_NEWER
            metrics.GpuTime = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong("GPU");
            #endif

            CurrentMetrics = metrics;
            AddMetricsToHistory(metrics);
            
            OnMetricsUpdated?.Invoke(metrics);
            CheckPerformanceThresholds(metrics);

            NeonQuestLogger.LogDebug($"Performance: {metrics.FrameRate:F1} FPS, {metrics.FrameTime:F2}ms frame time, {metrics.MemoryUsage / 1024 / 1024}MB memory", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void AddMetricsToHistory(PerformanceMetrics metrics)
        {
            _metricsHistory.Enqueue(metrics);
            
            while (_metricsHistory.Count > _maxSampleHistory)
            {
                _metricsHistory.Dequeue();
            }
        }

        private void CheckPerformanceThresholds(PerformanceMetrics metrics)
        {
            if (metrics.FrameRate < _performanceCriticalThreshold)
            {
                OnPerformanceCritical?.Invoke(metrics);
                NeonQuestLogger.LogError($"Critical performance: {metrics.FrameRate:F1} FPS (threshold: {_performanceCriticalThreshold})", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
            else if (metrics.FrameRate < _performanceWarningThreshold)
            {
                OnPerformanceWarning?.Invoke(metrics);
                NeonQuestLogger.LogWarning($"Performance warning: {metrics.FrameRate:F1} FPS (threshold: {_performanceWarningThreshold})", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void DetectPerformanceRegression()
        {
            _regressionDetected = true;
            OnPerformanceRegressionDetected?.Invoke();
            
            NeonQuestLogger.LogCritical($"Performance regression detected: {_consecutivePoorFrames} consecutive poor frames", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Get average performance metrics over a specified time period
        /// </summary>
        public PerformanceMetrics GetAverageMetrics(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            var relevantMetrics = new List<PerformanceMetrics>();

            foreach (var metric in _metricsHistory)
            {
                if (metric.Timestamp >= cutoffTime)
                {
                    relevantMetrics.Add(metric);
                }
            }

            if (relevantMetrics.Count == 0)
                return CurrentMetrics;

            var avgMetrics = new PerformanceMetrics
            {
                Timestamp = DateTime.Now
            };

            float totalFrameRate = 0f;
            float totalFrameTime = 0f;
            long totalMemory = 0L;
            int totalGameObjects = 0;

            foreach (var metric in relevantMetrics)
            {
                totalFrameRate += metric.FrameRate;
                totalFrameTime += metric.FrameTime;
                totalMemory += metric.MemoryUsage;
                totalGameObjects += metric.ActiveGameObjects;
            }

            int count = relevantMetrics.Count;
            avgMetrics.FrameRate = totalFrameRate / count;
            avgMetrics.FrameTime = totalFrameTime / count;
            avgMetrics.MemoryUsage = totalMemory / count;
            avgMetrics.ActiveGameObjects = totalGameObjects / count;

            return avgMetrics;
        }

        /// <summary>
        /// Record generation-specific performance metrics
        /// </summary>
        public void RecordGenerationMetrics(int objectsGenerated, float generationTime)
        {
            if (CurrentMetrics != null)
            {
                CurrentMetrics.GeneratedObjectsCount = objectsGenerated;
                CurrentMetrics.GenerationTime = generationTime;
            }

            NeonQuestLogger.LogDebug($"Generation metrics: {objectsGenerated} objects in {generationTime:F2}ms", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Reset performance regression detection
        /// </summary>
        public void ResetRegressionDetection()
        {
            _regressionDetected = false;
            _consecutivePoorFrames = 0;
            NeonQuestLogger.LogInfo("Performance regression detection reset", NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Enable or disable performance monitoring
        /// </summary>
        public void SetMonitoringEnabled(bool enabled)
        {
            _enableMonitoring = enabled;
            NeonQuestLogger.LogInfo($"Performance monitoring {(enabled ? "enabled" : "disabled")}", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Clear metrics history
        /// </summary>
        public void ClearHistory()
        {
            _metricsHistory.Clear();
            NeonQuestLogger.LogInfo("Performance metrics history cleared", NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Get performance summary as formatted string
        /// </summary>
        public string GetPerformanceSummary()
        {
            if (CurrentMetrics == null) return "No metrics available";

            var avgMetrics = GetAverageMetrics(TimeSpan.FromMinutes(1));
            
            return $"Current: {CurrentMetrics.FrameRate:F1} FPS, {CurrentMetrics.FrameTime:F2}ms\n" +
                   $"1min Avg: {avgMetrics.FrameRate:F1} FPS, {avgMetrics.FrameTime:F2}ms\n" +
                   $"Memory: {CurrentMetrics.MemoryUsage / 1024 / 1024}MB\n" +
                   $"GameObjects: {CurrentMetrics.ActiveGameObjects}\n" +
                   $"Regression: {(_regressionDetected ? "DETECTED" : "None")}";
        }
    }
}