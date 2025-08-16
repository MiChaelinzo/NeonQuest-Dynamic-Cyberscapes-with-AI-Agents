using System.Collections;
using UnityEngine;
using NeonQuest.Core.Diagnostics;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Example script demonstrating the complete diagnostic system setup and usage
    /// </summary>
    public class DiagnosticSystemExample : MonoBehaviour
    {
        [Header("Example Settings")]
        [SerializeField] private bool _setupDiagnosticsOnStart = true;
        [SerializeField] private bool _runPerformanceTest = true;
        [SerializeField] private bool _simulateErrors = false;
        [SerializeField] private float _testDuration = 30.0f;

        private DiagnosticsManager _diagnosticsManager;
        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;
        private DiagnosticUI _diagnosticUI;
        private DiagnosticsValidator _validator;

        private void Start()
        {
            if (_setupDiagnosticsOnStart)
            {
                SetupDiagnosticSystem();
                StartCoroutine(RunExampleScenario());
            }
        }

        private void SetupDiagnosticSystem()
        {
            NeonQuestLogger.LogInfo("Setting up diagnostic system example", NeonQuestLogger.LogCategory.General, this);

            // Create or get DiagnosticsManager (this will create other components automatically)
            _diagnosticsManager = FindObjectOfType<DiagnosticsManager>();
            if (_diagnosticsManager == null)
            {
                GameObject diagnosticsGO = new GameObject("DiagnosticsSystem");
                _diagnosticsManager = diagnosticsGO.AddComponent<DiagnosticsManager>();
            }

            // Get references to other components
            _performanceMonitor = _diagnosticsManager.GetComponent<PerformanceMonitor>();
            _systemDiagnostics = _diagnosticsManager.GetComponent<SystemDiagnostics>();
            _diagnosticUI = FindObjectOfType<DiagnosticUI>();

            // Add validator
            _validator = _diagnosticsManager.gameObject.AddComponent<DiagnosticsValidator>();

            // Subscribe to events for demonstration
            SubscribeToEvents();

            NeonQuestLogger.LogInfo("Diagnostic system setup complete", NeonQuestLogger.LogCategory.General, this);
        }

        private void SubscribeToEvents()
        {
            if (_diagnosticsManager != null)
            {
                _diagnosticsManager.OnReportGenerated += HandleReportGenerated;
                _diagnosticsManager.OnPerformanceRegressionDetected += HandlePerformanceRegression;
                _diagnosticsManager.OnCriticalSystemError += HandleCriticalError;
            }

            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated += HandleMetricsUpdated;
                _performanceMonitor.OnPerformanceWarning += HandlePerformanceWarning;
                _performanceMonitor.OnPerformanceCritical += HandlePerformanceCritical;
            }

            if (_systemDiagnostics != null)
            {
                _systemDiagnostics.OnComponentHealthChanged += HandleComponentHealthChanged;
                _systemDiagnostics.OnSystemHealthUpdated += HandleSystemHealthUpdated;
            }
        }

        private IEnumerator RunExampleScenario()
        {
            NeonQuestLogger.LogInfo($"Starting diagnostic system example scenario (duration: {_testDuration}s)", 
                NeonQuestLogger.LogCategory.General, this);

            float startTime = Time.time;
            int iterationCount = 0;

            while (Time.time - startTime < _testDuration)
            {
                iterationCount++;

                // Simulate various system activities
                if (_runPerformanceTest)
                {
                    SimulatePerformanceActivity(iterationCount);
                }

                // Simulate system health changes
                SimulateSystemHealthChanges(iterationCount);

                // Occasionally simulate errors
                if (_simulateErrors && iterationCount % 20 == 0)
                {
                    SimulateSystemErrors();
                }

                // Force diagnostic report every 10 iterations
                if (iterationCount % 10 == 0)
                {
                    _diagnosticsManager?.ForceReportGeneration();
                    LogCurrentSystemStatus();
                }

                yield return new WaitForSeconds(1.0f);
            }

            // Final validation
            if (_validator != null)
            {
                _validator.RunManualValidation();
                yield return new WaitForSeconds(2.0f);
                NeonQuestLogger.LogInfo($"Final validation results:\n{_validator.GetValidationStatus()}", 
                    NeonQuestLogger.LogCategory.General, this);
            }

            NeonQuestLogger.LogInfo("Diagnostic system example scenario completed", 
                NeonQuestLogger.LogCategory.General, this);
        }

        private void SimulatePerformanceActivity(int iteration)
        {
            // Simulate procedural generation activity
            int objectsGenerated = Random.Range(5, 25);
            float generationTime = Random.Range(1.0f, 10.0f);
            
            _performanceMonitor?.RecordGenerationMetrics(objectsGenerated, generationTime);

            // Occasionally create temporary objects to affect memory usage
            if (iteration % 15 == 0)
            {
                StartCoroutine(CreateTemporaryObjects());
            }
        }

        private IEnumerator CreateTemporaryObjects()
        {
            // Create some temporary objects to simulate memory usage
            GameObject[] tempObjects = new GameObject[Random.Range(10, 50)];
            
            for (int i = 0; i < tempObjects.Length; i++)
            {
                tempObjects[i] = new GameObject($"TempObject_{i}");
                tempObjects[i].AddComponent<MeshRenderer>();
                tempObjects[i].AddComponent<MeshFilter>();
            }

            yield return new WaitForSeconds(2.0f);

            // Clean up temporary objects
            foreach (var obj in tempObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
        }

        private void SimulateSystemHealthChanges(int iteration)
        {
            if (_systemDiagnostics == null) return;

            // Register and update various system components
            string[] components = { "ExampleGenerator", "ExampleAssetLoader", "ExampleAudioSystem", "ExampleLighting" };
            
            foreach (string component in components)
            {
                _systemDiagnostics.RegisterComponent(component);
                
                // Randomly update component health
                if (Random.Range(0f, 1f) < 0.3f) // 30% chance to update each iteration
                {
                    var status = GetRandomHealthStatus();
                    string message = GetHealthStatusMessage(status, iteration);
                    _systemDiagnostics.UpdateComponentHealth(component, status, message);
                }
            }
        }

        private SystemDiagnostics.HealthStatus GetRandomHealthStatus()
        {
            float rand = Random.Range(0f, 1f);
            if (rand < 0.7f) return SystemDiagnostics.HealthStatus.Healthy;
            if (rand < 0.9f) return SystemDiagnostics.HealthStatus.Warning;
            return SystemDiagnostics.HealthStatus.Critical;
        }

        private string GetHealthStatusMessage(SystemDiagnostics.HealthStatus status, int iteration)
        {
            return status switch
            {
                SystemDiagnostics.HealthStatus.Healthy => $"Operating normally (iteration {iteration})",
                SystemDiagnostics.HealthStatus.Warning => $"Minor issues detected (iteration {iteration})",
                SystemDiagnostics.HealthStatus.Critical => $"Critical issues found (iteration {iteration})",
                _ => $"Status unknown (iteration {iteration})"
            };
        }

        private void SimulateSystemErrors()
        {
            string[] errorMessages = {
                "Simulated asset loading error",
                "Simulated network timeout",
                "Simulated memory allocation failure",
                "Simulated configuration parsing error"
            };

            string errorMessage = errorMessages[Random.Range(0, errorMessages.Length)];
            Debug.LogError($"[SIMULATION] {errorMessage}");
        }

        private void LogCurrentSystemStatus()
        {
            if (_diagnosticsManager != null)
            {
                string summary = _diagnosticsManager.GetComprehensiveDiagnosticSummary();
                NeonQuestLogger.LogDebug($"Current System Status:\n{summary}", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        // Event Handlers
        private void HandleReportGenerated(DiagnosticsManager.DiagnosticReport report)
        {
            NeonQuestLogger.LogDebug($"Diagnostic report generated: {report.Summary}", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandlePerformanceRegression(float regressionAmount)
        {
            NeonQuestLogger.LogWarning($"Performance regression detected: {regressionAmount * 100:F1}% drop", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandleCriticalError(string errorMessage)
        {
            NeonQuestLogger.LogError($"Critical system error: {errorMessage}", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandleMetricsUpdated(PerformanceMonitor.PerformanceMetrics metrics)
        {
            // Log detailed metrics occasionally
            if (Random.Range(0f, 1f) < 0.1f) // 10% chance
            {
                NeonQuestLogger.LogDebug($"Performance update: {metrics.FrameRate:F1} FPS, {metrics.MemoryUsage / 1024 / 1024:F1}MB", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void HandlePerformanceWarning(PerformanceMonitor.PerformanceMetrics metrics)
        {
            NeonQuestLogger.LogWarning($"Performance warning: {metrics.FrameRate:F1} FPS", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandlePerformanceCritical(PerformanceMonitor.PerformanceMetrics metrics)
        {
            NeonQuestLogger.LogError($"Critical performance: {metrics.FrameRate:F1} FPS", 
                NeonQuestLogger.LogCategory.Performance, this);
        }

        private void HandleComponentHealthChanged(SystemDiagnostics.SystemHealth health)
        {
            if (health.Status == SystemDiagnostics.HealthStatus.Critical)
            {
                NeonQuestLogger.LogError($"Component health critical: {health.ComponentName} - {health.StatusMessage}", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void HandleSystemHealthUpdated(System.Collections.Generic.Dictionary<string, SystemDiagnostics.SystemHealth> healthData)
        {
            int criticalCount = 0;
            int warningCount = 0;
            int healthyCount = 0;

            foreach (var health in healthData.Values)
            {
                switch (health.Status)
                {
                    case SystemDiagnostics.HealthStatus.Critical: criticalCount++; break;
                    case SystemDiagnostics.HealthStatus.Warning: warningCount++; break;
                    case SystemDiagnostics.HealthStatus.Healthy: healthyCount++; break;
                }
            }

            if (criticalCount > 0 || warningCount > 0)
            {
                NeonQuestLogger.LogInfo($"System health summary: {healthyCount} healthy, {warningCount} warnings, {criticalCount} critical", 
                    NeonQuestLogger.LogCategory.Performance, this);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_diagnosticsManager != null)
            {
                _diagnosticsManager.OnReportGenerated -= HandleReportGenerated;
                _diagnosticsManager.OnPerformanceRegressionDetected -= HandlePerformanceRegression;
                _diagnosticsManager.OnCriticalSystemError -= HandleCriticalError;
            }

            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated -= HandleMetricsUpdated;
                _performanceMonitor.OnPerformanceWarning -= HandlePerformanceWarning;
                _performanceMonitor.OnPerformanceCritical -= HandlePerformanceCritical;
            }

            if (_systemDiagnostics != null)
            {
                _systemDiagnostics.OnComponentHealthChanged -= HandleComponentHealthChanged;
                _systemDiagnostics.OnSystemHealthUpdated -= HandleSystemHealthUpdated;
            }
        }

        // Public methods for manual testing
        [ContextMenu("Force Diagnostic Report")]
        public void ForceReport()
        {
            _diagnosticsManager?.ForceReportGeneration();
        }

        [ContextMenu("Clear Diagnostic History")]
        public void ClearHistory()
        {
            _diagnosticsManager?.ClearDiagnosticHistory();
        }

        [ContextMenu("Run Validation")]
        public void RunValidation()
        {
            _validator?.RunManualValidation();
        }

        [ContextMenu("Toggle Diagnostics")]
        public void ToggleDiagnostics()
        {
            if (_diagnosticsManager != null)
            {
                _diagnosticsManager.SetDiagnosticsEnabled(!_diagnosticsManager.DiagnosticsEnabled);
            }
        }
    }
}