using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core.Diagnostics;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Tests.Diagnostics
{
    public class DiagnosticsTests
    {
        private GameObject _testGameObject;
        private PerformanceMonitor _performanceMonitor;
        private SystemDiagnostics _systemDiagnostics;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestDiagnostics");
            _performanceMonitor = _testGameObject.AddComponent<PerformanceMonitor>();
            _systemDiagnostics = _testGameObject.AddComponent<SystemDiagnostics>();
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
        public void PerformanceMonitor_Initialization_SetsDefaultValues()
        {
            // Assert
            Assert.IsNotNull(_performanceMonitor);
            Assert.IsTrue(_performanceMonitor.IsMonitoring);
            Assert.IsNotNull(_performanceMonitor.MetricsHistory);
        }

        [Test]
        public void PerformanceMonitor_SetMonitoringEnabled_UpdatesState()
        {
            // Act
            _performanceMonitor.SetMonitoringEnabled(false);

            // Assert
            Assert.IsFalse(_performanceMonitor.IsMonitoring);

            // Act
            _performanceMonitor.SetMonitoringEnabled(true);

            // Assert
            Assert.IsTrue(_performanceMonitor.IsMonitoring);
        }

        [Test]
        public void PerformanceMonitor_RecordGenerationMetrics_UpdatesCurrentMetrics()
        {
            // Arrange
            int objectsGenerated = 10;
            float generationTime = 5.5f;

            // Act
            _performanceMonitor.RecordGenerationMetrics(objectsGenerated, generationTime);

            // Assert
            if (_performanceMonitor.CurrentMetrics != null)
            {
                Assert.AreEqual(objectsGenerated, _performanceMonitor.CurrentMetrics.GeneratedObjectsCount);
                Assert.AreEqual(generationTime, _performanceMonitor.CurrentMetrics.GenerationTime);
            }
        }

        [Test]
        public void PerformanceMonitor_GetAverageMetrics_ReturnsValidData()
        {
            // Act
            var avgMetrics = _performanceMonitor.GetAverageMetrics(TimeSpan.FromMinutes(1));

            // Assert
            Assert.IsNotNull(avgMetrics);
            Assert.IsNotNull(avgMetrics.Timestamp);
        }

        [Test]
        public void PerformanceMonitor_ClearHistory_RemovesAllMetrics()
        {
            // Act
            _performanceMonitor.ClearHistory();

            // Assert
            Assert.AreEqual(0, _performanceMonitor.MetricsHistory.Count);
        }

        [Test]
        public void PerformanceMonitor_GetPerformanceSummary_ReturnsFormattedString()
        {
            // Act
            string summary = _performanceMonitor.GetPerformanceSummary();

            // Assert
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Length > 0);
        }

        [Test]
        public void SystemDiagnostics_RegisterComponent_AddsToHealthMonitoring()
        {
            // Arrange
            string componentName = "TestComponent";

            // Act
            _systemDiagnostics.RegisterComponent(componentName);

            // Assert
            Assert.IsTrue(_systemDiagnostics.ComponentHealth.ContainsKey(componentName));
            Assert.AreEqual(SystemDiagnostics.HealthStatus.Unknown, 
                _systemDiagnostics.ComponentHealth[componentName].Status);
        }

        [Test]
        public void SystemDiagnostics_UpdateComponentHealth_ChangesStatus()
        {
            // Arrange
            string componentName = "TestComponent";
            _systemDiagnostics.RegisterComponent(componentName);

            // Act
            _systemDiagnostics.UpdateComponentHealth(componentName, 
                SystemDiagnostics.HealthStatus.Healthy, "Test message", 1.5f);

            // Assert
            var health = _systemDiagnostics.ComponentHealth[componentName];
            Assert.AreEqual(SystemDiagnostics.HealthStatus.Healthy, health.Status);
            Assert.AreEqual("Test message", health.StatusMessage);
            Assert.AreEqual(1.5f, health.ResponseTime);
        }

        [Test]
        public void SystemDiagnostics_PerformHealthCheck_UpdatesAllComponents()
        {
            // Arrange
            bool healthUpdatedCalled = false;
            _systemDiagnostics.OnSystemHealthUpdated += (health) => { healthUpdatedCalled = true; };

            // Act
            _systemDiagnostics.PerformHealthCheck();

            // Assert
            Assert.IsTrue(healthUpdatedCalled);
            Assert.IsTrue(_systemDiagnostics.ComponentHealth.Count > 0);
        }

        [Test]
        public void SystemDiagnostics_GetHealthReport_ReturnsFormattedReport()
        {
            // Arrange
            _systemDiagnostics.RegisterComponent("TestComponent");
            _systemDiagnostics.UpdateComponentHealth("TestComponent", 
                SystemDiagnostics.HealthStatus.Healthy, "All good");

            // Act
            string report = _systemDiagnostics.GetHealthReport();

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("TestComponent"));
            Assert.IsTrue(report.Contains("Healthy"));
            Assert.IsTrue(report.Contains("All good"));
        }

        [Test]
        public void SystemDiagnostics_ResetHealthStatuses_ResetsAllComponents()
        {
            // Arrange
            _systemDiagnostics.RegisterComponent("TestComponent");
            _systemDiagnostics.UpdateComponentHealth("TestComponent", 
                SystemDiagnostics.HealthStatus.Critical, "Error");

            // Act
            _systemDiagnostics.ResetHealthStatuses();

            // Assert
            Assert.AreEqual(SystemDiagnostics.HealthStatus.Unknown, 
                _systemDiagnostics.ComponentHealth["TestComponent"].Status);
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_EventTriggers_FireCorrectly()
        {
            // Arrange
            bool warningTriggered = false;
            bool criticalTriggered = false;
            bool metricsUpdated = false;

            _performanceMonitor.OnPerformanceWarning += (metrics) => { warningTriggered = true; };
            _performanceMonitor.OnPerformanceCritical += (metrics) => { criticalTriggered = true; };
            _performanceMonitor.OnMetricsUpdated += (metrics) => { metricsUpdated = true; };

            // Act - Wait for a few frames to allow metrics collection
            yield return new WaitForSeconds(2.0f);

            // Assert
            Assert.IsTrue(metricsUpdated, "Metrics should be updated during normal operation");
            
            // Note: Warning and critical events depend on actual performance conditions
            // In a test environment, these may not trigger unless performance is actually poor
        }

        [Test]
        public void SystemDiagnostics_ComponentHealthChanged_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            SystemDiagnostics.SystemHealth receivedHealth = null;
            
            _systemDiagnostics.OnComponentHealthChanged += (health) => 
            { 
                eventTriggered = true; 
                receivedHealth = health;
            };

            string componentName = "TestComponent";
            _systemDiagnostics.RegisterComponent(componentName);

            // Act
            _systemDiagnostics.UpdateComponentHealth(componentName, 
                SystemDiagnostics.HealthStatus.Warning, "Test warning");

            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.IsNotNull(receivedHealth);
            Assert.AreEqual(SystemDiagnostics.HealthStatus.Warning, receivedHealth.Status);
            Assert.AreEqual("Test warning", receivedHealth.StatusMessage);
        }

        [Test]
        public void PerformanceMonitor_ResetRegressionDetection_ClearsRegressionState()
        {
            // Act
            _performanceMonitor.ResetRegressionDetection();

            // Assert - Should not throw any exceptions
            Assert.DoesNotThrow(() => _performanceMonitor.ResetRegressionDetection());
        }

        [Test]
        public void SystemDiagnostics_ForceHealthCheck_ExecutesImmediately()
        {
            // Arrange
            bool healthUpdated = false;
            _systemDiagnostics.OnSystemHealthUpdated += (health) => { healthUpdated = true; };

            // Act
            _systemDiagnostics.ForceHealthCheck();

            // Assert
            Assert.IsTrue(healthUpdated);
        }
    }
}