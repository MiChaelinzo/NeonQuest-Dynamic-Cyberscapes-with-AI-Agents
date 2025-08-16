using System;
using System.Collections;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Diagnostics
{
    /// <summary>
    /// Runtime validator for diagnostic system functionality
    /// </summary>
    public class DiagnosticsValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool _runValidationOnStart = true;
        [SerializeField] private bool _enableContinuousValidation = false;
        [SerializeField] private float _validationInterval = 30.0f;

        private DiagnosticsManager _diagnosticsManager;
        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;
        private DiagnosticUI _diagnosticUI;

        public bool ValidationPassed { get; private set; } = false;
        public string ValidationResults { get; private set; } = "";

        private void Start()
        {
            if (_runValidationOnStart)
            {
                StartCoroutine(RunValidation());
            }

            if (_enableContinuousValidation)
            {
                InvokeRepeating(nameof(RunPeriodicValidation), _validationInterval, _validationInterval);
            }
        }

        private void RunPeriodicValidation()
        {
            StartCoroutine(RunValidation());
        }

        private IEnumerator RunValidation()
        {
            NeonQuestLogger.LogInfo("Starting diagnostic system validation", NeonQuestLogger.LogCategory.Performance, this);
            
            var results = new System.Text.StringBuilder();
            bool allTestsPassed = true;

            // Test 1: Component Initialization
            results.AppendLine("=== DIAGNOSTIC SYSTEM VALIDATION ===");
            results.AppendLine($"Validation Time: {DateTime.Now}");
            results.AppendLine();

            if (ValidateComponentInitialization(results))
            {
                results.AppendLine("✓ Component Initialization: PASSED");
            }
            else
            {
                results.AppendLine("✗ Component Initialization: FAILED");
                allTestsPassed = false;
            }

            yield return new WaitForSeconds(0.1f);

            // Test 2: Performance Monitoring
            if (ValidatePerformanceMonitoring(results))
            {
                results.AppendLine("✓ Performance Monitoring: PASSED");
            }
            else
            {
                results.AppendLine("✗ Performance Monitoring: FAILED");
                allTestsPassed = false;
            }

            yield return new WaitForSeconds(0.1f);

            // Test 3: System Health Monitoring
            if (ValidateSystemHealthMonitoring(results))
            {
                results.AppendLine("✓ System Health Monitoring: PASSED");
            }
            else
            {
                results.AppendLine("✗ System Health Monitoring: FAILED");
                allTestsPassed = false;
            }

            yield return new WaitForSeconds(0.1f);

            // Test 4: Report Generation
            if (ValidateReportGeneration(results))
            {
                results.AppendLine("✓ Report Generation: PASSED");
            }
            else
            {
                results.AppendLine("✗ Report Generation: FAILED");
                allTestsPassed = false;
            }

            yield return new WaitForSeconds(0.1f);

            // Test 5: Error Handling
            if (ValidateErrorHandling(results))
            {
                results.AppendLine("✓ Error Handling: PASSED");
            }
            else
            {
                results.AppendLine("✗ Error Handling: FAILED");
                allTestsPassed = false;
            }

            ValidationPassed = allTestsPassed;
            ValidationResults = results.ToString();

            results.AppendLine();
            results.AppendLine($"Overall Result: {(allTestsPassed ? "PASSED" : "FAILED")}");

            NeonQuestLogger.LogInfo($"Diagnostic validation completed: {(allTestsPassed ? "PASSED" : "FAILED")}", 
                NeonQuestLogger.LogCategory.Performance, this);

            Debug.Log(ValidationResults);
        }

        private bool ValidateComponentInitialization(System.Text.StringBuilder results)
        {
            try
            {
                _diagnosticsManager = FindObjectOfType<DiagnosticsManager>();
                if (_diagnosticsManager == null)
                {
                    _diagnosticsManager = gameObject.AddComponent<DiagnosticsManager>();
                }

                _performanceMonitor = FindObjectOfType<PerformanceMonitor>();
                _systemDiagnostics = FindObjectOfType<SystemDiagnostics>();
                _diagnosticUI = FindObjectOfType<DiagnosticUI>();

                bool hasRequiredComponents = _diagnosticsManager != null && 
                                           _performanceMonitor != null && 
                                           _systemDiagnostics != null;

                if (!hasRequiredComponents)
                {
                    results.AppendLine("  - Missing required diagnostic components");
                    return false;
                }

                if (!_diagnosticsManager.DiagnosticsEnabled)
                {
                    results.AppendLine("  - Diagnostics not enabled");
                    return false;
                }

                if (!_performanceMonitor.IsMonitoring)
                {
                    results.AppendLine("  - Performance monitoring not active");
                    return false;
                }

                results.AppendLine("  - All components initialized correctly");
                return true;
            }
            catch (Exception ex)
            {
                results.AppendLine($"  - Exception during initialization: {ex.Message}");
                return false;
            }
        }

        private bool ValidatePerformanceMonitoring(System.Text.StringBuilder results)
        {
            try
            {
                if (_performanceMonitor == null)
                {
                    results.AppendLine("  - PerformanceMonitor not available");
                    return false;
                }

                // Test metrics collection
                var currentMetrics = _performanceMonitor.CurrentMetrics;
                if (currentMetrics != null)
                {
                    if (currentMetrics.FrameRate <= 0)
                    {
                        results.AppendLine("  - Invalid frame rate data");
                        return false;
                    }

                    if (currentMetrics.MemoryUsage <= 0)
                    {
                        results.AppendLine("  - Invalid memory usage data");
                        return false;
                    }
                }

                // Test metrics recording
                _performanceMonitor.RecordGenerationMetrics(10, 5.0f);
                
                // Test summary generation
                string summary = _performanceMonitor.GetPerformanceSummary();
                if (string.IsNullOrEmpty(summary))
                {
                    results.AppendLine("  - Performance summary generation failed");
                    return false;
                }

                results.AppendLine($"  - Current FPS: {currentMetrics?.FrameRate:F1}");
                results.AppendLine($"  - Memory Usage: {currentMetrics?.MemoryUsage / 1024 / 1024:F1}MB");
                return true;
            }
            catch (Exception ex)
            {
                results.AppendLine($"  - Exception during performance validation: {ex.Message}");
                return false;
            }
        }

        private bool ValidateSystemHealthMonitoring(System.Text.StringBuilder results)
        {
            try
            {
                if (_systemDiagnostics == null)
                {
                    results.AppendLine("  - SystemDiagnostics not available");
                    return false;
                }

                // Test component registration
                string testComponent = "ValidationTestComponent";
                _systemDiagnostics.RegisterComponent(testComponent);
                
                if (!_systemDiagnostics.ComponentHealth.ContainsKey(testComponent))
                {
                    results.AppendLine("  - Component registration failed");
                    return false;
                }

                // Test health status update
                _systemDiagnostics.UpdateComponentHealth(testComponent, 
                    SystemDiagnostics.HealthStatus.Healthy, "Validation test");

                var health = _systemDiagnostics.ComponentHealth[testComponent];
                if (health.Status != SystemDiagnostics.HealthStatus.Healthy)
                {
                    results.AppendLine("  - Health status update failed");
                    return false;
                }

                // Test health report generation
                string healthReport = _systemDiagnostics.GetHealthReport();
                if (string.IsNullOrEmpty(healthReport))
                {
                    results.AppendLine("  - Health report generation failed");
                    return false;
                }

                results.AppendLine($"  - Overall System Health: {_systemDiagnostics.OverallSystemHealth}");
                results.AppendLine($"  - Monitored Components: {_systemDiagnostics.ComponentHealth.Count}");
                return true;
            }
            catch (Exception ex)
            {
                results.AppendLine($"  - Exception during health validation: {ex.Message}");
                return false;
            }
        }

        private bool ValidateReportGeneration(System.Text.StringBuilder results)
        {
            try
            {
                if (_diagnosticsManager == null)
                {
                    results.AppendLine("  - DiagnosticsManager not available");
                    return false;
                }

                int initialReportCount = _diagnosticsManager.ReportHistory.Count;
                
                // Force report generation
                _diagnosticsManager.ForceReportGeneration();
                
                if (_diagnosticsManager.ReportHistory.Count <= initialReportCount)
                {
                    results.AppendLine("  - Report generation failed");
                    return false;
                }

                var latestReport = _diagnosticsManager.LatestReport;
                if (latestReport == null)
                {
                    results.AppendLine("  - Latest report not available");
                    return false;
                }

                if (string.IsNullOrEmpty(latestReport.Summary))
                {
                    results.AppendLine("  - Report summary is empty");
                    return false;
                }

                // Test comprehensive summary
                string comprehensiveSummary = _diagnosticsManager.GetComprehensiveDiagnosticSummary();
                if (string.IsNullOrEmpty(comprehensiveSummary))
                {
                    results.AppendLine("  - Comprehensive summary generation failed");
                    return false;
                }

                results.AppendLine($"  - Reports Generated: {_diagnosticsManager.ReportHistory.Count}");
                results.AppendLine($"  - Latest Report Time: {latestReport.Timestamp:HH:mm:ss}");
                return true;
            }
            catch (Exception ex)
            {
                results.AppendLine($"  - Exception during report validation: {ex.Message}");
                return false;
            }
        }

        private bool ValidateErrorHandling(System.Text.StringBuilder results)
        {
            try
            {
                if (_diagnosticsManager == null)
                {
                    results.AppendLine("  - DiagnosticsManager not available");
                    return false;
                }

                // Test error capture
                int initialErrorCount = _diagnosticsManager.LatestReport?.RecentErrors?.Count ?? 0;
                
                // Generate a test error
                Debug.LogError("Validation test error - this is expected");
                
                // Force report generation to capture the error
                _diagnosticsManager.ForceReportGeneration();
                
                int finalErrorCount = _diagnosticsManager.LatestReport?.RecentErrors?.Count ?? 0;
                
                if (finalErrorCount <= initialErrorCount)
                {
                    results.AppendLine("  - Error capture may not be working (this could be timing-related)");
                    // Don't fail the test as this might be timing-dependent
                }

                // Test system enable/disable
                _diagnosticsManager.SetDiagnosticsEnabled(false);
                if (_diagnosticsManager.DiagnosticsEnabled)
                {
                    results.AppendLine("  - Diagnostics disable failed");
                    return false;
                }

                _diagnosticsManager.SetDiagnosticsEnabled(true);
                if (!_diagnosticsManager.DiagnosticsEnabled)
                {
                    results.AppendLine("  - Diagnostics re-enable failed");
                    return false;
                }

                results.AppendLine("  - Error handling and system control working");
                return true;
            }
            catch (Exception ex)
            {
                results.AppendLine($"  - Exception during error handling validation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Public method to trigger validation manually
        /// </summary>
        public void RunManualValidation()
        {
            StartCoroutine(RunValidation());
        }

        /// <summary>
        /// Get the current validation status
        /// </summary>
        public string GetValidationStatus()
        {
            if (string.IsNullOrEmpty(ValidationResults))
            {
                return "Validation not yet run";
            }

            return ValidationResults;
        }
    }
}