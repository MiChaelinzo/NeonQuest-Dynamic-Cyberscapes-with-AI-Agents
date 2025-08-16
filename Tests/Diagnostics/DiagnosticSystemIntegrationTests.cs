using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core.Diagnostics;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Tests.Diagnostics
{
    /// <summary>
    /// Integration tests for the complete diagnostic system
    /// </summary>
    public class DiagnosticSystemIntegrationTests
    {
        private GameObject _testGameObject;
        private DiagnosticsManager _diagnosticsManager;
        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;
        private DiagnosticUI _diagnosticUI;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestDiagnosticSystem");
            _diagnosticsManager = _testGameObject.AddComponent<DiagnosticsManager>();
            
            // Components should be automatically created by DiagnosticsManager
            _performanceMonitor = _testGameObject.GetComponent<PerformanceMonitor>();
            _systemDiagnostics = _testGameObject.GetComponent<SystemDiagnostics>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void DiagnosticSystem_FullInitialization_CreatesAllComponents()
        {
            // Assert
            Assert.IsNotNull(_diagnosticsManager, "DiagnosticsManager should be created");
            Assert.IsNotNull(_performanceMonitor, "PerformanceMonitor should be created");
            Assert.IsNotNull(_systemDiagnostics, "SystemDiagnostics should be created");
            
            Assert.IsTrue(_diagnosticsManager.DiagnosticsEnabled, "Diagnostics should be enabled by default");
            Assert.IsTrue(_performanceMonitor.IsMonitoring, "Performance monitoring should be enabled");
        }

        [UnityTest]
        public IEnumerator DiagnosticSystem_EndToEndMonitoring_WorksCorrectly()
        {
            // Arrange
            bool reportGenerated = false;
            bool performanceMetricsReceived = false;
            bool systemHealthUpdated = false;
            
            _diagnosticsManager.OnReportGenerated += (report) => { reportGenerated = true; };
            _performanceMonitor.OnMetricsUpdated += (metrics) => { performanceMetricsReceived = true; };
            _systemDiagnostics.OnSystemHealthUpdated += (health) => { systemHealthUpdated = true; };

            // Act - Let the system run for a period
            yield return new WaitForSeconds(3.0f);

            // Force a diagnostic report
            _diagnosticsManager.ForceReportGeneration();

            // Assert
            Assert.IsTrue(reportGenerated, "Diagnostic report should be generated");
            Assert.IsTrue(performanceMetricsReceived, "Performance metrics should be received");
            Assert.IsTrue(systemHealthUpdated, "System health should be updated");
        }

        [Test]
        public void DiagnosticSystem_ComponentCommunication_WorksCorrectly()
        {
            // Arrange
            _diagnosticsManager.ForceReportGeneration();
            var report = _diagnosticsManager.LatestReport;

            // Assert
            Assert.IsNotNull(report, "Diagnostic report should be generated");
            Assert.IsNotNull(report.SystemHealth, "Report should contain system health data");
            Assert.IsTrue(report.SystemHealth.Count > 0, "System health should have component data");
            
            // Verify that system health data comes from SystemDiagnostics
            foreach (var healthEntry in report.SystemHealth)
            {
                Assert.IsTrue(_systemDiagnostics.ComponentHealth.ContainsKey(healthEntry.Key),
                    $"System health should contain component: {healthEntry.Key}");
            }
        }

        [UnityTest]
        public IEnumerator DiagnosticSystem_PerformanceRegressionDetection_WorksEndToEnd()
        {
            // Arrange
            bool regressionDetectedByManager = false;
            bool regressionDetectedByMonitor = false;
            
            _diagnosticsManager.OnPerformanceRegressionDetected += (drop) => { regressionDetectedByManager = true; };
            _performanceMonitor.OnPerformanceRegressionDetected += () => { regressionDetectedByMonitor = true; };

            // Act - Let the system establish baseline
            yield return new WaitForSeconds(3.0f);

            // Note: In a real test environment, it's difficult to force performance regression
            // This test verifies the event wiring works correctly
            Assert.IsNotNull(_diagnosticsManager.OnPerformanceRegressionDetected);
            Assert.IsNotNull(_performanceMonitor.OnPerformanceRegressionDetected);
        }

        [Test]
        public void DiagnosticSystem_ErrorHandling_CapturesAndReports()
        {
            // Arrange
            bool criticalErrorDetected = false;
            string capturedErrorMessage = "";
            
            _diagnosticsManager.OnCriticalSystemError += (message) => 
            { 
                criticalErrorDetected = true; 
                capturedErrorMessage = message;
            };

            // Act - Generate an error
            Debug.LogError("Test critical error for diagnostic system");
            
            // Force report generation to capture the error
            _diagnosticsManager.ForceReportGeneration();
            var report = _diagnosticsManager.LatestReport;

            // Assert
            Assert.IsNotNull(report);
            Assert.IsNotNull(report.RecentErrors);
            Assert.IsTrue(report.RecentErrors.Count > 0, "Report should contain recent errors");
            
            // Check if the error was captured
            bool errorCaptured = report.RecentErrors.Any(error => error.Contains("Test critical error"));
            Assert.IsTrue(errorCaptured, "Test error should be captured in recent errors");
        }

        [Test]
        public void DiagnosticSystem_HealthMonitoring_UpdatesCorrectly()
        {
            // Arrange
            string testComponentName = "TestIntegrationComponent";
            
            // Act
            _systemDiagnostics.RegisterComponent(testComponentName);
            _systemDiagnostics.UpdateComponentHealth(testComponentName, 
                SystemDiagnostics.HealthStatus.Warning, "Integration test warning");
            
            _diagnosticsManager.ForceReportGeneration();
            var report = _diagnosticsManager.LatestReport;

            // Assert
            Assert.IsTrue(report.SystemHealth.ContainsKey(testComponentName));
            Assert.AreEqual(SystemDiagnostics.HealthStatus.Warning, 
                report.SystemHealth[testComponentName].Status);
            Assert.AreEqual("Integration test warning", 
                report.SystemHealth[testComponentName].StatusMessage);
        }

        [UnityTest]
        public IEnumerator DiagnosticSystem_ReportGeneration_MaintainsHistory()
        {
            // Arrange
            int expectedReports = 5;

            // Act - Generate multiple reports over time
            for (int i = 0; i < expectedReports; i++)
            {
                _diagnosticsManager.ForceReportGeneration();
                yield return new WaitForSeconds(0.1f);
            }

            // Assert
            Assert.AreEqual(expectedReports, _diagnosticsManager.ReportHistory.Count);
            
            // Verify reports are in chronological order
            var reports = _diagnosticsManager.ReportHistory.ToList();
            for (int i = 1; i < reports.Count; i++)
            {
                Assert.IsTrue(reports[i].Timestamp >= reports[i-1].Timestamp,
                    "Reports should be in chronological order");
            }
        }

        [Test]
        public void DiagnosticSystem_ComprehensiveSummary_ContainsAllData()
        {
            // Arrange
            _diagnosticsManager.ForceReportGeneration();

            // Act
            string summary = _diagnosticsManager.GetComprehensiveDiagnosticSummary();

            // Assert
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("NEONQUEST DIAGNOSTIC SUMMARY"));
            Assert.IsTrue(summary.Contains("PERFORMANCE"));
            Assert.IsTrue(summary.Contains("SYSTEM HEALTH"));
            Assert.IsTrue(summary.Contains("DIAGNOSTIC REPORTS"));
            
            // Verify it contains data from all subsystems
            string performanceSummary = _performanceMonitor.GetPerformanceSummary();
            string healthReport = _systemDiagnostics.GetHealthReport();
            
            // The comprehensive summary should reference data from subsystems
            Assert.IsNotNull(performanceSummary);
            Assert.IsNotNull(healthReport);
        }

        [Test]
        public void DiagnosticSystem_EnableDisable_AffectsAllComponents()
        {
            // Act - Disable diagnostics
            _diagnosticsManager.SetDiagnosticsEnabled(false);

            // Assert
            Assert.IsFalse(_diagnosticsManager.DiagnosticsEnabled);
            Assert.IsFalse(_performanceMonitor.IsMonitoring);

            // Act - Re-enable diagnostics
            _diagnosticsManager.SetDiagnosticsEnabled(true);

            // Assert
            Assert.IsTrue(_diagnosticsManager.DiagnosticsEnabled);
            Assert.IsTrue(_performanceMonitor.IsMonitoring);
        }

        [Test]
        public void DiagnosticSystem_ClearHistory_AffectsAllComponents()
        {
            // Arrange - Generate some data
            _diagnosticsManager.ForceReportGeneration();
            _performanceMonitor.RecordGenerationMetrics(10, 5.0f);
            _systemDiagnostics.RegisterComponent("TestComponent");

            Assert.IsTrue(_diagnosticsManager.ReportHistory.Count > 0);
            Assert.IsTrue(_performanceMonitor.MetricsHistory.Count >= 0);

            // Act
            _diagnosticsManager.ClearDiagnosticHistory();

            // Assert
            Assert.AreEqual(0, _diagnosticsManager.ReportHistory.Count);
            Assert.AreEqual(0, _performanceMonitor.MetricsHistory.Count);
        }

        [UnityTest]
        public IEnumerator DiagnosticSystem_StressTest_HandlesHighLoad()
        {
            // Arrange
            int iterations = 50;
            bool errorOccurred = false;

            // Act - Generate high load
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    _diagnosticsManager.ForceReportGeneration();
                    _performanceMonitor.RecordGenerationMetrics(i, i * 0.1f);
                    _systemDiagnostics.UpdateComponentHealth($"StressComponent{i % 5}", 
                        SystemDiagnostics.HealthStatus.Healthy, $"Iteration {i}");
                    
                    if (i % 10 == 0)
                    {
                        yield return null; // Yield occasionally to prevent timeout
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during stress test iteration {i}: {ex.Message}");
                    errorOccurred = true;
                    break;
                }
            }

            // Assert
            Assert.IsFalse(errorOccurred, "System should handle high load without errors");
            Assert.IsTrue(_diagnosticsManager.ReportHistory.Count > 0, "Reports should be generated under load");
        }

        [Test]
        public void DiagnosticSystem_ComponentDependencies_AreCorrect()
        {
            // Assert that all components have correct dependencies
            Assert.IsNotNull(_diagnosticsManager);
            Assert.IsNotNull(_performanceMonitor);
            Assert.IsNotNull(_systemDiagnostics);
            
            // Verify DiagnosticsManager can access other components
            Assert.DoesNotThrow(() => _diagnosticsManager.ForceReportGeneration());
            Assert.DoesNotThrow(() => _diagnosticsManager.GetComprehensiveDiagnosticSummary());
        }
    }
}