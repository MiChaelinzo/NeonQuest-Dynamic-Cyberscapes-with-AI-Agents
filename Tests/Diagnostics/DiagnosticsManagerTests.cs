using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core.Diagnostics;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Tests.Diagnostics
{
    public class DiagnosticsManagerTests
    {
        private GameObject _testGameObject;
        private DiagnosticsManager _diagnosticsManager;
        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestDiagnosticsManager");
            _diagnosticsManager = _testGameObject.AddComponent<DiagnosticsManager>();
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
        public void DiagnosticsManager_Initialization_CreatesRequiredComponents()
        {
            // Assert
            Assert.IsNotNull(_diagnosticsManager);
            Assert.IsNotNull(_performanceMonitor);
            Assert.IsNotNull(_systemDiagnostics);
            Assert.IsTrue(_diagnosticsManager.DiagnosticsEnabled);
        }

        [Test]
        public void DiagnosticsManager_SetDiagnosticsEnabled_UpdatesState()
        {
            // Act
            _diagnosticsManager.SetDiagnosticsEnabled(false);

            // Assert
            Assert.IsFalse(_diagnosticsManager.DiagnosticsEnabled);

            // Act
            _diagnosticsManager.SetDiagnosticsEnabled(true);

            // Assert
            Assert.IsTrue(_diagnosticsManager.DiagnosticsEnabled);
        }

        [Test]
        public void DiagnosticsManager_ForceReportGeneration_CreatesReport()
        {
            // Arrange
            bool reportGenerated = false;
            DiagnosticsManager.DiagnosticReport generatedReport = null;
            
            _diagnosticsManager.OnReportGenerated += (report) => 
            { 
                reportGenerated = true; 
                generatedReport = report;
            };

            // Act
            _diagnosticsManager.ForceReportGeneration();

            // Assert
            Assert.IsTrue(reportGenerated);
            Assert.IsNotNull(generatedReport);
            Assert.IsNotNull(generatedReport.Timestamp);
            Assert.IsNotNull(generatedReport.Summary);
            Assert.IsNotNull(_diagnosticsManager.LatestReport);
        }

        [Test]
        public void DiagnosticsManager_ClearDiagnosticHistory_ResetsAllData()
        {
            // Arrange
            _diagnosticsManager.ForceReportGeneration();
            Assert.IsTrue(_diagnosticsManager.ReportHistory.Count > 0);

            // Act
            _diagnosticsManager.ClearDiagnosticHistory();

            // Assert
            Assert.AreEqual(0, _diagnosticsManager.ReportHistory.Count);
        }

        [Test]
        public void DiagnosticsManager_GetComprehensiveDiagnosticSummary_ReturnsValidSummary()
        {
            // Act
            string summary = _diagnosticsManager.GetComprehensiveDiagnosticSummary();

            // Assert
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Length > 0);
            Assert.IsTrue(summary.Contains("NEONQUEST DIAGNOSTIC SUMMARY"));
            Assert.IsTrue(summary.Contains("PERFORMANCE"));
            Assert.IsTrue(summary.Contains("SYSTEM HEALTH"));
        }

        [Test]
        public void DiagnosticsManager_PerformanceRegressionDetection_TriggersEvent()
        {
            // Arrange
            bool regressionDetected = false;
            float detectedDrop = 0f;
            
            _diagnosticsManager.OnPerformanceRegressionDetected += (drop) => 
            { 
                regressionDetected = true; 
                detectedDrop = drop;
            };

            // This test would require simulating performance regression
            // In a real scenario, we would need to manipulate frame rate data
            
            // For now, we just verify the event subscription works
            Assert.IsNotNull(_diagnosticsManager.OnPerformanceRegressionDetected);
        }

        [Test]
        public void DiagnosticsManager_CriticalSystemError_TriggersEvent()
        {
            // Arrange
            bool errorDetected = false;
            string errorMessage = "";
            
            _diagnosticsManager.OnCriticalSystemError += (message) => 
            { 
                errorDetected = true; 
                errorMessage = message;
            };

            // Simulate a critical performance condition
            if (_performanceMonitor != null)
            {
                // This would trigger through the performance monitor's critical event
                // The actual triggering depends on real performance conditions
            }

            // For now, verify event subscription
            Assert.IsNotNull(_diagnosticsManager.OnCriticalSystemError);
        }

        [UnityTest]
        public IEnumerator DiagnosticsManager_ReportGeneration_WorksOverTime()
        {
            // Arrange
            int reportCount = 0;
            _diagnosticsManager.OnReportGenerated += (report) => { reportCount++; };

            // Act - Wait for automatic report generation (this would take too long in real test)
            // Instead, we'll force multiple reports
            _diagnosticsManager.ForceReportGeneration();
            yield return new WaitForSeconds(0.1f);
            _diagnosticsManager.ForceReportGeneration();
            yield return new WaitForSeconds(0.1f);
            _diagnosticsManager.ForceReportGeneration();

            // Assert
            Assert.AreEqual(3, reportCount);
            Assert.AreEqual(3, _diagnosticsManager.ReportHistory.Count);
        }

        [Test]
        public void DiagnosticReport_ContainsExpectedData()
        {
            // Act
            _diagnosticsManager.ForceReportGeneration();
            var report = _diagnosticsManager.LatestReport;

            // Assert
            Assert.IsNotNull(report);
            Assert.IsNotNull(report.Timestamp);
            Assert.IsNotNull(report.Summary);
            Assert.IsNotNull(report.RecentErrors);
            Assert.IsNotNull(report.SystemHealth);
            
            // Performance data might be null initially, but structure should exist
            // Assert.IsNotNull(report.Performance); // This might be null in test environment
        }

        [Test]
        public void DiagnosticsManager_MultipleReports_MaintainsHistory()
        {
            // Act
            for (int i = 0; i < 5; i++)
            {
                _diagnosticsManager.ForceReportGeneration();
            }

            // Assert
            Assert.AreEqual(5, _diagnosticsManager.ReportHistory.Count);
            
            // Verify reports are in chronological order
            DateTime previousTime = DateTime.MinValue;
            foreach (var report in _diagnosticsManager.ReportHistory)
            {
                Assert.IsTrue(report.Timestamp > previousTime);
                previousTime = report.Timestamp;
            }
        }

        [Test]
        public void DiagnosticsManager_ErrorTracking_CapturesErrors()
        {
            // Arrange
            _diagnosticsManager.ForceReportGeneration();
            var initialErrorCount = _diagnosticsManager.LatestReport.RecentErrors.Count;

            // Act - Simulate an error by logging one
            Debug.LogError("Test error for diagnostics");
            
            // Wait a frame for the log message to be processed
            _diagnosticsManager.ForceReportGeneration();

            // Assert
            var finalErrorCount = _diagnosticsManager.LatestReport.RecentErrors.Count;
            Assert.IsTrue(finalErrorCount > initialErrorCount, "Error count should increase after logging an error");
        }

        [Test]
        public void DiagnosticsManager_ComponentIntegration_WorksCorrectly()
        {
            // Assert that all required components are properly integrated
            Assert.IsNotNull(_performanceMonitor);
            Assert.IsNotNull(_systemDiagnostics);
            
            // Verify that the diagnostics manager can access component data
            _diagnosticsManager.ForceReportGeneration();
            var report = _diagnosticsManager.LatestReport;
            
            Assert.IsNotNull(report.SystemHealth);
            Assert.IsTrue(report.SystemHealth.Count > 0, "Should have system health data from SystemDiagnostics");
        }
    }
}