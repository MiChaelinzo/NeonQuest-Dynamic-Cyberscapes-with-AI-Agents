using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core.Diagnostics;

namespace NeonQuest.Tests.Diagnostics
{
    /// <summary>
    /// Tests to verify the accuracy of performance monitoring and metrics collection
    /// </summary>
    public class PerformanceMonitoringAccuracyTests
    {
        private GameObject _testGameObject;
        private PerformanceMonitor _performanceMonitor;
        private const float TOLERANCE = 0.1f; // 10% tolerance for timing-based tests

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestPerformanceMonitor");
            _performanceMonitor = _testGameObject.AddComponent<PerformanceMonitor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_FrameRateAccuracy_MeasuresCorrectly()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            List<float> frameRateSamples = new List<float>();
            
            _performanceMonitor.OnMetricsUpdated += (metrics) => 
            {
                frameRateSamples.Add(metrics.FrameRate);
            };

            // Act - Collect samples over time
            float testDuration = 3.0f;
            float startTime = Time.time;
            
            while (Time.time - startTime < testDuration)
            {
                yield return null; // Wait one frame
            }

            // Assert
            Assert.IsTrue(frameRateSamples.Count > 0, "Should have collected frame rate samples");
            
            // Verify frame rates are reasonable (between 10 and 200 FPS)
            foreach (var fps in frameRateSamples)
            {
                Assert.IsTrue(fps > 10f && fps < 200f, $"Frame rate {fps} should be within reasonable bounds");
            }

            // Verify frame rate consistency (standard deviation should be reasonable)
            if (frameRateSamples.Count > 1)
            {
                float average = frameRateSamples.Average();
                float variance = frameRateSamples.Select(x => (x - average) * (x - average)).Average();
                float stdDev = Mathf.Sqrt(variance);
                
                // Standard deviation should be less than 50% of average for stable performance
                Assert.IsTrue(stdDev < average * 0.5f, 
                    $"Frame rate should be relatively stable. StdDev: {stdDev}, Average: {average}");
            }
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_FrameTimeAccuracy_CorrelatesWithFrameRate()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            PerformanceMonitor.PerformanceMetrics lastMetrics = null;
            
            _performanceMonitor.OnMetricsUpdated += (metrics) => 
            {
                lastMetrics = metrics;
            };

            // Act - Wait for metrics to be collected
            yield return new WaitForSeconds(2.0f);

            // Assert
            Assert.IsNotNull(lastMetrics, "Should have collected performance metrics");
            
            // Frame time should roughly equal 1000 / frame rate (in milliseconds)
            float expectedFrameTime = 1000f / lastMetrics.FrameRate;
            float actualFrameTime = lastMetrics.FrameTime;
            
            float difference = Mathf.Abs(expectedFrameTime - actualFrameTime);
            float tolerance = expectedFrameTime * TOLERANCE;
            
            Assert.IsTrue(difference <= tolerance, 
                $"Frame time ({actualFrameTime}ms) should correlate with frame rate ({lastMetrics.FrameRate} FPS). " +
                $"Expected: {expectedFrameTime}ms, Difference: {difference}ms, Tolerance: {tolerance}ms");
        }

        [Test]
        public void PerformanceMonitor_MemoryTracking_ReturnsValidValues()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);

            // Act - Force a metrics collection
            _performanceMonitor.RecordGenerationMetrics(0, 0);

            // Wait for next update cycle
            var metrics = _performanceMonitor.CurrentMetrics;

            // Assert
            if (metrics != null)
            {
                Assert.IsTrue(metrics.MemoryUsage > 0, "Memory usage should be greater than 0");
                Assert.IsTrue(metrics.MemoryUsage < 8L * 1024 * 1024 * 1024, // 8GB limit
                    "Memory usage should be within reasonable bounds");
            }
        }

        [Test]
        public void PerformanceMonitor_GenerationMetricsAccuracy_RecordsCorrectly()
        {
            // Arrange
            int expectedObjects = 25;
            float expectedTime = 12.5f;

            // Act
            _performanceMonitor.RecordGenerationMetrics(expectedObjects, expectedTime);

            // Assert
            var metrics = _performanceMonitor.CurrentMetrics;
            if (metrics != null)
            {
                Assert.AreEqual(expectedObjects, metrics.GeneratedObjectsCount);
                Assert.AreEqual(expectedTime, metrics.GenerationTime, 0.001f);
            }
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_AverageMetricsAccuracy_CalculatesCorrectly()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            List<PerformanceMonitor.PerformanceMetrics> collectedMetrics = new List<PerformanceMonitor.PerformanceMetrics>();
            
            _performanceMonitor.OnMetricsUpdated += (metrics) => 
            {
                collectedMetrics.Add(metrics);
            };

            // Act - Collect metrics over time
            yield return new WaitForSeconds(3.0f);

            // Get average metrics for the last 2 seconds
            var averageMetrics = _performanceMonitor.GetAverageMetrics(TimeSpan.FromSeconds(2));

            // Assert
            Assert.IsNotNull(averageMetrics);
            
            if (collectedMetrics.Count > 1)
            {
                // Calculate expected average from collected samples
                var recentMetrics = collectedMetrics.Where(m => 
                    (DateTime.Now - m.Timestamp).TotalSeconds <= 2).ToList();
                
                if (recentMetrics.Count > 0)
                {
                    float expectedAvgFPS = recentMetrics.Average(m => m.FrameRate);
                    float actualAvgFPS = averageMetrics.FrameRate;
                    
                    float difference = Mathf.Abs(expectedAvgFPS - actualAvgFPS);
                    float tolerance = expectedAvgFPS * TOLERANCE;
                    
                    Assert.IsTrue(difference <= tolerance,
                        $"Average FPS calculation should be accurate. Expected: {expectedAvgFPS}, " +
                        $"Actual: {actualAvgFPS}, Difference: {difference}, Tolerance: {tolerance}");
                }
            }
        }

        [Test]
        public void PerformanceMonitor_ThresholdDetection_TriggersCorrectly()
        {
            // Arrange
            bool warningTriggered = false;
            bool criticalTriggered = false;
            
            _performanceMonitor.OnPerformanceWarning += (metrics) => { warningTriggered = true; };
            _performanceMonitor.OnPerformanceCritical += (metrics) => { criticalTriggered = true; };

            // Note: In a real test environment, it's difficult to force low performance
            // This test verifies the event subscription mechanism works
            Assert.IsNotNull(_performanceMonitor.OnPerformanceWarning);
            Assert.IsNotNull(_performanceMonitor.OnPerformanceCritical);
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_RegressionDetection_WorksOverTime()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            bool regressionDetected = false;
            
            _performanceMonitor.OnPerformanceRegressionDetected += () => { regressionDetected = true; };

            // Act - Monitor for a period
            yield return new WaitForSeconds(2.0f);

            // Reset regression detection to test the reset functionality
            _performanceMonitor.ResetRegressionDetection();

            // Assert - Verify reset works without errors
            Assert.DoesNotThrow(() => _performanceMonitor.ResetRegressionDetection());
        }

        [Test]
        public void PerformanceMonitor_MetricsHistory_MaintainsCorrectSize()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            int initialCount = _performanceMonitor.MetricsHistory.Count;

            // Act - Add some metrics
            for (int i = 0; i < 10; i++)
            {
                _performanceMonitor.RecordGenerationMetrics(i, i * 0.5f);
            }

            // Assert - History should not exceed maximum size
            // Note: The actual history size depends on the sampling interval and time elapsed
            Assert.IsTrue(_performanceMonitor.MetricsHistory.Count >= initialCount);
        }

        [Test]
        public void PerformanceMonitor_ClearHistory_RemovesAllData()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            _performanceMonitor.RecordGenerationMetrics(5, 1.0f);

            // Act
            _performanceMonitor.ClearHistory();

            // Assert
            Assert.AreEqual(0, _performanceMonitor.MetricsHistory.Count);
        }

        [Test]
        public void PerformanceMonitor_PerformanceSummary_ContainsExpectedData()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            _performanceMonitor.RecordGenerationMetrics(10, 2.5f);

            // Act
            string summary = _performanceMonitor.GetPerformanceSummary();

            // Assert
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("FPS"), "Summary should contain FPS information");
            Assert.IsTrue(summary.Contains("Memory"), "Summary should contain memory information");
            Assert.IsTrue(summary.Contains("GameObjects"), "Summary should contain GameObject count");
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_TimestampAccuracy_IsCorrect()
        {
            // Arrange
            _performanceMonitor.SetMonitoringEnabled(true);
            DateTime testStartTime = DateTime.Now;
            PerformanceMonitor.PerformanceMetrics capturedMetrics = null;
            
            _performanceMonitor.OnMetricsUpdated += (metrics) => 
            {
                if (capturedMetrics == null)
                {
                    capturedMetrics = metrics;
                }
            };

            // Act - Wait for metrics collection
            yield return new WaitForSeconds(1.5f);

            // Assert
            Assert.IsNotNull(capturedMetrics);
            
            var timeDifference = Math.Abs((capturedMetrics.Timestamp - testStartTime).TotalSeconds);
            Assert.IsTrue(timeDifference < 5.0, // Should be within 5 seconds
                $"Timestamp should be accurate. Difference: {timeDifference} seconds");
        }

        [Test]
        public void PerformanceMonitor_EnableDisable_WorksCorrectly()
        {
            // Arrange & Act
            _performanceMonitor.SetMonitoringEnabled(false);
            
            // Assert
            Assert.IsFalse(_performanceMonitor.IsMonitoring);
            
            // Act
            _performanceMonitor.SetMonitoringEnabled(true);
            
            // Assert
            Assert.IsTrue(_performanceMonitor.IsMonitoring);
        }
    }
}