using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Diagnostics
{
    /// <summary>
    /// Central manager for all diagnostic and monitoring systems
    /// </summary>
    public class DiagnosticsManager : MonoBehaviour
    {
        [System.Serializable]
        public class DiagnosticReport
        {
            public DateTime Timestamp { get; set; }
            public PerformanceMonitor.PerformanceMetrics Performance { get; set; }
            public Dictionary<string, SystemDiagnostics.SystemHealth> SystemHealth { get; set; }
            public List<string> RecentErrors { get; set; }
            public string Summary { get; set; }
        }

        [Header("Diagnostic Settings")]
        [SerializeField] private bool _enableDiagnostics = true;
        [SerializeField] private bool _enableReportGeneration = true;
        [SerializeField] private float _reportGenerationInterval = 60.0f; // 1 minute
        [SerializeField] private int _maxReportHistory = 100;
        [SerializeField] private bool _saveReportsToFile = false;
        [SerializeField] private string _reportDirectory = "DiagnosticReports";

        [Header("Performance Regression Detection")]
        [SerializeField] private bool _enableRegressionDetection = true;
        [SerializeField] private float _regressionThreshold = 0.15f; // 15% performance drop
        [SerializeField] private int _regressionSampleSize = 30;

        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;
        private DiagnosticUI _diagnosticUI;
        
        private Queue<DiagnosticReport> _reportHistory = new Queue<DiagnosticReport>();
        private List<string> _recentErrors = new List<string>();
        private float _lastReportTime;
        
        // Performance regression tracking
        private Queue<float> _performanceBaseline = new Queue<float>();
        private bool _baselineEstablished = false;
        private float _currentAveragePerformance;

        public event Action<DiagnosticReport> OnReportGenerated;
        public event Action<float> OnPerformanceRegressionDetected;
        public event Action<string> OnCriticalSystemError;

        public bool DiagnosticsEnabled => _enableDiagnostics;
        public IReadOnlyCollection<DiagnosticReport> ReportHistory => _reportHistory;
        public DiagnosticReport LatestReport { get; private set; }

        private void Awake()
        {
            InitializeDiagnosticComponents();
            
            if (_saveReportsToFile)
            {
                CreateReportDirectory();
            }
        }

        private void Start()
        {
            if (_enableDiagnostics)
            {
                SetupEventSubscriptions();
                NeonQuestLogger.LogInfo("Diagnostics Manager initialized", NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void Update()
        {
            if (!_enableDiagnostics) return;

            if (_enableReportGeneration && Time.time - _lastReportTime >= _reportGenerationInterval)
            {
                GenerateDiagnosticReport();
                _lastReportTime = Time.time;
            }
        }

        private void InitializeDiagnosticComponents()
        {
            // Get or create PerformanceMonitor
            _performanceMonitor = GetComponent<PerformanceMonitor>();
            if (_performanceMonitor == null)
            {
                _performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            }

            // Get or create SystemDiagnostics
            _systemDiagnostics = GetComponent<SystemDiagnostics>();
            if (_systemDiagnostics == null)
            {
                _systemDiagnostics = gameObject.AddComponent<SystemDiagnostics>();
            }

            // Get or create DiagnosticUI
            _diagnosticUI = FindObjectOfType<DiagnosticUI>();
            if (_diagnosticUI == null)
            {
                GameObject uiGO = new GameObject("DiagnosticUI");
                _diagnosticUI = uiGO.AddComponent<DiagnosticUI>();
            }
        }

        private void SetupEventSubscriptions()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated += HandlePerformanceMetrics;
                _performanceMonitor.OnPerformanceCritical += HandleCriticalPerformance;
                _performanceMonitor.OnPerformanceRegressionDetected += HandlePerformanceRegression;
            }

            if (_systemDiagnostics != null)
            {
                _systemDiagnostics.OnComponentHealthChanged += HandleComponentHealthChange;
                _systemDiagnostics.OnSystemHealthUpdated += HandleSystemHealthUpdate;
            }

            // Subscribe to Unity log messages for error tracking
            Application.logMessageReceived += HandleLogMessage;
        }

        private void HandlePerformanceMetrics(PerformanceMonitor.PerformanceMetrics metrics)
        {
            if (_enableRegressionDetection)
            {
                UpdatePerformanceBaseline(metrics.FrameRate);
                CheckForPerformanceRegression(metrics.FrameRate);
            }
        }

        private void HandleCriticalPerformance(PerformanceMonitor.PerformanceMetrics metrics)
        {
            string errorMessage = $"Critical performance detected: {metrics.FrameRate:F1} FPS";
            OnCriticalSystemError?.Invoke(errorMessage);
            AddRecentError(errorMessage);
        }

        private void HandlePerformanceRegression()
        {
            string errorMessage = "Performance regression detected by PerformanceMonitor";
            OnCriticalSystemError?.Invoke(errorMessage);
            AddRecentError(errorMessage);
        }

        private void HandleComponentHealthChange(SystemDiagnostics.SystemHealth health)
        {
            if (health.Status == SystemDiagnostics.HealthStatus.Critical)
            {
                string errorMessage = $"Critical component health: {health.ComponentName} - {health.StatusMessage}";
                OnCriticalSystemError?.Invoke(errorMessage);
                AddRecentError(errorMessage);
            }
        }

        private void HandleSystemHealthUpdate(Dictionary<string, SystemDiagnostics.SystemHealth> healthData)
        {
            // Log system health updates for debugging
            NeonQuestLogger.LogDebug($"System health updated: {healthData.Count} components monitored", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                AddRecentError($"[{type}] {logString}");
            }
        }

        private void UpdatePerformanceBaseline(float frameRate)
        {
            _performanceBaseline.Enqueue(frameRate);
            
            while (_performanceBaseline.Count > _regressionSampleSize)
            {
                _performanceBaseline.Dequeue();
            }

            if (_performanceBaseline.Count >= _regressionSampleSize && !_baselineEstablished)
            {
                EstablishPerformanceBaseline();
            }
        }

        private void EstablishPerformanceBaseline()
        {
            float total = 0f;
            foreach (float fps in _performanceBaseline)
            {
                total += fps;
            }
            
            _currentAveragePerformance = total / _performanceBaseline.Count;
            _baselineEstablished = true;
            
            NeonQuestLogger.LogInfo($"Performance baseline established: {_currentAveragePerformance:F1} FPS", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void CheckForPerformanceRegression(float currentFrameRate)
        {
            if (!_baselineEstablished) return;

            float performanceDrop = (_currentAveragePerformance - currentFrameRate) / _currentAveragePerformance;
            
            if (performanceDrop > _regressionThreshold)
            {
                OnPerformanceRegressionDetected?.Invoke(performanceDrop);
                
                string errorMessage = $"Performance regression detected: {performanceDrop * 100:F1}% drop from baseline";
                AddRecentError(errorMessage);
                
                NeonQuestLogger.LogWarning(errorMessage, NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void AddRecentError(string error)
        {
            _recentErrors.Add($"{DateTime.Now:HH:mm:ss} - {error}");
            
            // Keep only recent errors (last 50)
            while (_recentErrors.Count > 50)
            {
                _recentErrors.RemoveAt(0);
            }
        }

        private void GenerateDiagnosticReport()
        {
            var report = new DiagnosticReport
            {
                Timestamp = DateTime.Now,
                Performance = _performanceMonitor?.CurrentMetrics,
                SystemHealth = new Dictionary<string, SystemDiagnostics.SystemHealth>(),
                RecentErrors = new List<string>(_recentErrors),
                Summary = GenerateReportSummary()
            };

            // Copy system health data
            if (_systemDiagnostics?.ComponentHealth != null)
            {
                foreach (var kvp in _systemDiagnostics.ComponentHealth)
                {
                    report.SystemHealth[kvp.Key] = kvp.Value;
                }
            }

            LatestReport = report;
            AddReportToHistory(report);
            
            OnReportGenerated?.Invoke(report);

            if (_saveReportsToFile)
            {
                SaveReportToFile(report);
            }

            NeonQuestLogger.LogDebug($"Diagnostic report generated: {report.Summary}", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private string GenerateReportSummary()
        {
            var summary = new StringBuilder();
            
            // Performance summary
            if (_performanceMonitor?.CurrentMetrics != null)
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                summary.AppendLine($"Performance: {metrics.FrameRate:F1} FPS, {metrics.MemoryUsage / 1024 / 1024}MB");
            }

            // System health summary
            if (_systemDiagnostics != null)
            {
                summary.AppendLine($"System Health: {_systemDiagnostics.OverallSystemHealth}");
            }

            // Error summary
            if (_recentErrors.Count > 0)
            {
                summary.AppendLine($"Recent Errors: {_recentErrors.Count}");
            }

            return summary.ToString().Trim();
        }

        private void AddReportToHistory(DiagnosticReport report)
        {
            _reportHistory.Enqueue(report);
            
            while (_reportHistory.Count > _maxReportHistory)
            {
                _reportHistory.Dequeue();
            }
        }

        private void CreateReportDirectory()
        {
            string fullPath = Path.Combine(Application.persistentDataPath, _reportDirectory);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                NeonQuestLogger.LogInfo($"Created diagnostic report directory: {fullPath}", 
                    NeonQuestLogger.LogCategory.General, this);
            }
        }

        private void SaveReportToFile(DiagnosticReport report)
        {
            try
            {
                string fileName = $"diagnostic_report_{report.Timestamp:yyyyMMdd_HHmmss}.txt";
                string fullPath = Path.Combine(Application.persistentDataPath, _reportDirectory, fileName);
                
                var content = new StringBuilder();
                content.AppendLine($"Diagnostic Report - {report.Timestamp}");
                content.AppendLine("=" + new string('=', 50));
                content.AppendLine();
                
                content.AppendLine("SUMMARY:");
                content.AppendLine(report.Summary);
                content.AppendLine();
                
                if (report.Performance != null)
                {
                    content.AppendLine("PERFORMANCE METRICS:");
                    content.AppendLine($"  Frame Rate: {report.Performance.FrameRate:F1} FPS");
                    content.AppendLine($"  Frame Time: {report.Performance.FrameTime:F2}ms");
                    content.AppendLine($"  Memory Usage: {report.Performance.MemoryUsage / 1024 / 1024}MB");
                    content.AppendLine($"  Active GameObjects: {report.Performance.ActiveGameObjects}");
                    content.AppendLine();
                }
                
                if (report.SystemHealth.Count > 0)
                {
                    content.AppendLine("SYSTEM HEALTH:");
                    foreach (var health in report.SystemHealth.Values)
                    {
                        content.AppendLine($"  {health.ComponentName}: {health.Status} - {health.StatusMessage}");
                    }
                    content.AppendLine();
                }
                
                if (report.RecentErrors.Count > 0)
                {
                    content.AppendLine("RECENT ERRORS:");
                    foreach (var error in report.RecentErrors)
                    {
                        content.AppendLine($"  {error}");
                    }
                }
                
                File.WriteAllText(fullPath, content.ToString());
            }
            catch (Exception ex)
            {
                NeonQuestLogger.LogError($"Failed to save diagnostic report: {ex.Message}", 
                    NeonQuestLogger.LogCategory.General, this);
            }
        }

        /// <summary>
        /// Enable or disable diagnostic monitoring
        /// </summary>
        public void SetDiagnosticsEnabled(bool enabled)
        {
            _enableDiagnostics = enabled;
            
            if (_performanceMonitor != null)
            {
                _performanceMonitor.SetMonitoringEnabled(enabled);
            }
            
            NeonQuestLogger.LogInfo($"Diagnostics {(enabled ? "enabled" : "disabled")}", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Force generation of diagnostic report
        /// </summary>
        public void ForceReportGeneration()
        {
            GenerateDiagnosticReport();
            NeonQuestLogger.LogInfo("Manual diagnostic report generated", NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Clear all diagnostic history
        /// </summary>
        public void ClearDiagnosticHistory()
        {
            _reportHistory.Clear();
            _recentErrors.Clear();
            _performanceBaseline.Clear();
            _baselineEstablished = false;
            
            if (_performanceMonitor != null)
            {
                _performanceMonitor.ClearHistory();
            }
            
            if (_systemDiagnostics != null)
            {
                _systemDiagnostics.ResetHealthStatuses();
            }
            
            NeonQuestLogger.LogInfo("Diagnostic history cleared", NeonQuestLogger.LogCategory.Performance, this);
        }

        /// <summary>
        /// Get comprehensive diagnostic summary
        /// </summary>
        public string GetComprehensiveDiagnosticSummary()
        {
            var summary = new StringBuilder();
            
            summary.AppendLine("=== NEONQUEST DIAGNOSTIC SUMMARY ===");
            summary.AppendLine($"Generated: {DateTime.Now}");
            summary.AppendLine();
            
            if (_performanceMonitor != null)
            {
                summary.AppendLine("PERFORMANCE:");
                summary.AppendLine(_performanceMonitor.GetPerformanceSummary());
                summary.AppendLine();
            }
            
            if (_systemDiagnostics != null)
            {
                summary.AppendLine("SYSTEM HEALTH:");
                summary.AppendLine(_systemDiagnostics.GetHealthReport());
                summary.AppendLine();
            }
            
            summary.AppendLine($"DIAGNOSTIC REPORTS: {_reportHistory.Count} generated");
            summary.AppendLine($"RECENT ERRORS: {_recentErrors.Count}");
            
            if (_baselineEstablished)
            {
                summary.AppendLine($"PERFORMANCE BASELINE: {_currentAveragePerformance:F1} FPS");
            }
            
            return summary.ToString();
        }

        private void OnDestroy()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated -= HandlePerformanceMetrics;
                _performanceMonitor.OnPerformanceCritical -= HandleCriticalPerformance;
                _performanceMonitor.OnPerformanceRegressionDetected -= HandlePerformanceRegression;
            }

            if (_systemDiagnostics != null)
            {
                _systemDiagnostics.OnComponentHealthChanged -= HandleComponentHealthChange;
                _systemDiagnostics.OnSystemHealthUpdated -= HandleSystemHealthUpdate;
            }

            Application.logMessageReceived -= HandleLogMessage;
        }
    }
}