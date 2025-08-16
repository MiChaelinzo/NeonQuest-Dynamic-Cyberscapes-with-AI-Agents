using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace Tests.Generation
{
    /// <summary>
    /// Integration tests for LayoutManager focusing on spatial consistency and connectivity
    /// </summary>
    public class LayoutManagerIntegrationTests
    {
        private GameObject layoutManagerObject;
        private LayoutManager layoutManager;
        private EnvironmentConfiguration testConfig;
        
        [SetUp]
        public void SetUp()
        {
            layoutManagerObject = new GameObject("TestLayoutManager");
            layoutManager = layoutManagerObject.AddComponent<LayoutManager>();
            
            testConfig = new EnvironmentConfiguration
            {
                CorridorGenerationDistance = 50f,
                CorridorCleanupDistance = 100f,
                MaxActiveSegments = 20
            };
            
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
        }
        
        [TearDown]
        public void TearDown()
        {
            if (layoutManagerObject != null)
            {
                Object.DestroyImmediate(layoutManagerObject);
            }
        }
        
        [UnityTest]
        public IEnumerator SpatialConsistency_GeneratedSegmentsDoNotOverlap()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = playerPosition
            };
            
            // Act - Generate multiple segments
            for (int i = 0; i < 5; i++)
            {
                layoutManager.UpdateGeneration(Time.deltaTime, environmentState);
                yield return new WaitForEndOfFrame();
            }
            
            // Assert - Check that segments don't overlap
            var activeSegments = layoutManager.GetActiveSegments().Values.ToList();
            for (int i = 0; i < activeSegments.Count; i++)
            {
                for (int j = i + 1; j < activeSegments.Count; j++)
                {
                    var distance = Vector3.Distance(activeSegments[i].Position, activeSegments[j].Position);
                    Assert.IsTrue(distance > 5f, $"Segments {i} and {j} are too close: {distance}");
                }
            }
        }
        
        [UnityTest]
        public IEnumerator Connectivity_SegmentsFormConnectedNetwork()
        {
            // Arrange
            var positions = new[]
            {
                Vector3.zero,
                new Vector3(10f, 0f, 0f),
                new Vector3(20f, 0f, 0f)
            };
            
            // Act - Generate segments at specific positions
            foreach (var position in positions)
            {
                var task = layoutManager.GenerateAsync(new Dictionary<string, object>
                {
                    ["position"] = position,
                    ["type"] = SegmentType.Corridor
                });
                yield return new WaitUntil(() => task.IsCompleted);
            }
            
            // Assert - Check connectivity
            var activeSegments = layoutManager.GetActiveSegments().Values.ToList();
            Assert.IsTrue(activeSegments.Count >= 2, "Should have generated multiple segments");
            
            // Check that at least some segments have connections
            var connectedSegments = activeSegments.Count(s => 
                s.ConnectionPoints.Any(cp => cp.IsConnected));
            Assert.IsTrue(connectedSegments > 0, "At least some segments should be connected");
        }     
   
        [UnityTest]
        public IEnumerator GenerationDistance_OnlyGeneratesWithinRange()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = playerPosition
            };
            
            // Act - Update generation multiple times
            for (int i = 0; i < 10; i++)
            {
                layoutManager.UpdateGeneration(Time.deltaTime, environmentState);
                yield return new WaitForEndOfFrame();
            }
            
            // Assert - All segments should be within generation distance
            var activeSegments = layoutManager.GetActiveSegments().Values.ToList();
            foreach (var segment in activeSegments)
            {
                var distance = Vector3.Distance(segment.Position, playerPosition);
                Assert.IsTrue(distance <= testConfig.CorridorGenerationDistance + 10f, 
                    $"Segment at {segment.Position} is too far from player: {distance}");
            }
        }
        
        [UnityTest]
        public IEnumerator CleanupSystem_RemovesDistantSegments()
        {
            // Arrange - Generate segments around origin
            var playerPosition = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = playerPosition
            };
            
            for (int i = 0; i < 5; i++)
            {
                layoutManager.UpdateGeneration(Time.deltaTime, environmentState);
                yield return new WaitForEndOfFrame();
            }
            
            var initialSegmentCount = layoutManager.GetActiveSegments().Count;
            
            // Act - Move player far away and cleanup
            var newPlayerPosition = new Vector3(200f, 0f, 0f);
            layoutManager.CleanupDistantContent(testConfig.CorridorCleanupDistance, newPlayerPosition);
            
            // Assert - Segments should be cleaned up
            var remainingSegmentCount = layoutManager.GetActiveSegments().Count;
            Assert.IsTrue(remainingSegmentCount < initialSegmentCount, 
                "Some segments should have been cleaned up");
        }
        
        [UnityTest]
        public IEnumerator PerformanceThrottling_LimitsActiveSegments()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = playerPosition
            };
            
            // Act - Try to generate many segments
            for (int i = 0; i < 50; i++)
            {
                layoutManager.UpdateGeneration(Time.deltaTime, environmentState);
                yield return new WaitForEndOfFrame();
            }
            
            // Assert - Should not exceed max active segments
            var activeSegmentCount = layoutManager.GetActiveSegments().Count;
            Assert.IsTrue(activeSegmentCount <= testConfig.MaxActiveSegments + 5, 
                $"Too many active segments: {activeSegmentCount}");
        }
        
        [Test]
        public void JunctionGeneration_CreatesMultipleConnectionPoints()
        {
            // Arrange & Act
            var junction = new GeneratedSegment(Vector3.zero, SegmentType.Junction);
            
            // Simulate adding connection points like LayoutManager does
            junction.AddConnectionPoint(Vector3.forward * 5f, Vector3.forward);
            junction.AddConnectionPoint(Vector3.back * 5f, Vector3.back);
            junction.AddConnectionPoint(Vector3.right * 2f, Vector3.right);
            junction.AddConnectionPoint(Vector3.left * 2f, Vector3.left);
            
            // Assert
            Assert.AreEqual(4, junction.ConnectionPoints.Count);
            Assert.IsTrue(junction.ConnectionPoints.All(cp => !cp.IsConnected));
        }
        
        [Test]
        public void PerformanceCost_IncreasesWithActiveSegments()
        {
            // Arrange
            var initialCost = layoutManager.CurrentPerformanceCost;
            
            // Act - Add segments using reflection to test performance cost calculation
            var field = typeof(LayoutManager).GetField("activeSegments", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var segments = (Dictionary<string, GeneratedSegment>)field.GetValue(layoutManager);
            
            for (int i = 0; i < 5; i++)
            {
                var segment = new GeneratedSegment(new Vector3(i * 10f, 0f, 0f), SegmentType.Corridor);
                segments[segment.SegmentId] = segment;
            }
            
            // Trigger performance cost update using reflection
            var updateMethod = typeof(LayoutManager).GetMethod("UpdatePerformanceCost", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(layoutManager, null);
            
            // Assert
            var newCost = layoutManager.CurrentPerformanceCost;
            Assert.IsTrue(newCost > initialCost, "Performance cost should increase with more segments");
        }
    }
}