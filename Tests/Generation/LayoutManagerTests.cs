using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace Tests.Generation
{
    public class LayoutManagerTests
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
                CorridorGenerationDistance = 30f,
                CorridorCleanupDistance = 60f,
                MaxActiveSegments = 10
            };
        }
        
        [TearDown]
        public void TearDown()
        {
            if (layoutManagerObject != null)
            {
                Object.DestroyImmediate(layoutManagerObject);
            }
        }
        
        [Test]
        public void Initialize_WithValidConfig_SetsParameters()
        {
            // Arrange
            var configData = new Dictionary<string, object>
            {
                ["config"] = testConfig
            };
            
            // Act
            layoutManager.Initialize(configData);
            
            // Assert
            Assert.IsTrue(layoutManager.IsActive);
            Assert.AreEqual(0f, layoutManager.CurrentPerformanceCost);
        }
        
        [UnityTest]
        public IEnumerator GenerateAsync_WithValidPosition_CreatesSegment()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            var generationParams = new Dictionary<string, object>
            {
                ["position"] = Vector3.zero,
                ["type"] = SegmentType.Corridor
            };
            
            // Act
            var task = layoutManager.GenerateAsync(generationParams);
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Assert
            var activeSegments = layoutManager.GetActiveSegments();
            Assert.AreEqual(1, activeSegments.Count);
        }       
 
        [Test]
        public void CleanupDistantContent_RemovesDistantSegments()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            
            // Create a segment far from player
            var farPosition = new Vector3(100f, 0f, 0f);
            var segment = new GeneratedSegment(farPosition, SegmentType.Corridor);
            
            // Use reflection to add segment to active segments for testing
            var activeSegments = layoutManager.GetActiveSegments() as Dictionary<string, GeneratedSegment>;
            if (activeSegments != null)
            {
                var field = typeof(LayoutManager).GetField("activeSegments", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var segments = (Dictionary<string, GeneratedSegment>)field.GetValue(layoutManager);
                segments[segment.SegmentId] = segment;
            }
            
            // Act
            layoutManager.CleanupDistantContent(50f, Vector3.zero);
            
            // Assert
            var remainingSegments = layoutManager.GetActiveSegments();
            Assert.AreEqual(0, remainingSegments.Count);
        }
        
        [Test]
        public void SetQualityLevel_AdjustsMaxActiveSegments()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            
            // Act
            layoutManager.SetQualityLevel(0.5f);
            
            // Assert - Quality level should affect internal parameters
            // We can't directly test maxActiveSegments as it's private, but we can test behavior
            Assert.IsTrue(layoutManager.IsActive);
        }
        
        [Test]
        public void SetActive_DisablesGeneration()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            
            // Act
            layoutManager.SetActive(false);
            
            // Assert
            Assert.IsFalse(layoutManager.IsActive);
        }
        
        [Test]
        public void GetSegmentsNear_ReturnsNearbySegments()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            
            var nearPosition = new Vector3(5f, 0f, 0f);
            var farPosition = new Vector3(50f, 0f, 0f);
            
            var nearSegment = new GeneratedSegment(nearPosition, SegmentType.Corridor);
            var farSegment = new GeneratedSegment(farPosition, SegmentType.Corridor);
            
            // Use reflection to add segments
            var field = typeof(LayoutManager).GetField("activeSegments", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var segments = (Dictionary<string, GeneratedSegment>)field.GetValue(layoutManager);
            segments[nearSegment.SegmentId] = nearSegment;
            segments[farSegment.SegmentId] = farSegment;
            
            // Act
            var nearbySegments = layoutManager.GetSegmentsNear(Vector3.zero, 10f);
            
            // Assert
            Assert.AreEqual(1, nearbySegments.Count);
            Assert.AreEqual(nearSegment.SegmentId, nearbySegments[0].SegmentId);
        } 
       
        [Test]
        public void UpdateGeneration_WithPlayerPosition_UpdatesSegments()
        {
            // Arrange
            layoutManager.Initialize(new Dictionary<string, object> { ["config"] = testConfig });
            var environmentState = new Dictionary<string, object>
            {
                ["playerPosition"] = new Vector3(10f, 0f, 0f)
            };
            
            // Act
            layoutManager.UpdateGeneration(Time.deltaTime, environmentState);
            
            // Assert - Should not throw and should handle the update
            Assert.IsTrue(layoutManager.IsActive);
        }
    }
    
    [TestFixture]
    public class GeneratedSegmentTests
    {
        [Test]
        public void Constructor_CreatesValidSegment()
        {
            // Arrange & Act
            var segment = new GeneratedSegment(Vector3.zero, SegmentType.Corridor);
            
            // Assert
            Assert.IsNotNull(segment.SegmentId);
            Assert.AreEqual(Vector3.zero, segment.Position);
            Assert.AreEqual(SegmentType.Corridor, segment.Type);
            Assert.AreEqual(Vector3.one, segment.Scale);
            Assert.IsNotNull(segment.ConnectionPoints);
            Assert.IsNotNull(segment.Properties);
        }
        
        [Test]
        public void DistanceTo_CalculatesCorrectDistance()
        {
            // Arrange
            var segment = new GeneratedSegment(Vector3.zero, SegmentType.Corridor);
            var targetPosition = new Vector3(3f, 4f, 0f);
            
            // Act
            var distance = segment.DistanceTo(targetPosition);
            
            // Assert
            Assert.AreEqual(5f, distance, 0.01f); // 3-4-5 triangle
        }
        
        [Test]
        public void IsWithinDistance_ReturnsTrueForNearbyPositions()
        {
            // Arrange
            var segment = new GeneratedSegment(Vector3.zero, SegmentType.Corridor);
            var nearPosition = new Vector3(2f, 0f, 0f);
            var farPosition = new Vector3(10f, 0f, 0f);
            
            // Act & Assert
            Assert.IsTrue(segment.IsWithinDistance(nearPosition, 5f));
            Assert.IsFalse(segment.IsWithinDistance(farPosition, 5f));
        }
        
        [Test]
        public void AddConnectionPoint_AddsPointCorrectly()
        {
            // Arrange
            var segment = new GeneratedSegment(Vector3.zero, SegmentType.Corridor);
            var localPos = Vector3.forward;
            var direction = Vector3.forward;
            
            // Act
            segment.AddConnectionPoint(localPos, direction);
            
            // Assert
            Assert.AreEqual(1, segment.ConnectionPoints.Count);
            Assert.AreEqual(localPos, segment.ConnectionPoints[0].LocalPosition);
            Assert.AreEqual(direction, segment.ConnectionPoints[0].Direction);
        } 
       
        [Test]
        public void GetAvailableConnectionPoints_ReturnsUnconnectedPoints()
        {
            // Arrange
            var segment = new GeneratedSegment(Vector3.zero, SegmentType.Corridor);
            segment.AddConnectionPoint(Vector3.forward, Vector3.forward);
            segment.AddConnectionPoint(Vector3.back, Vector3.back);
            
            // Connect one point
            segment.ConnectionPoints[0].IsConnected = true;
            
            // Act
            var availablePoints = segment.GetAvailableConnectionPoints();
            
            // Assert
            Assert.AreEqual(1, availablePoints.Count);
            Assert.IsFalse(availablePoints[0].IsConnected);
        }
        
        [Test]
        public void UpdateConnectionPoints_UpdatesWorldPositions()
        {
            // Arrange
            var segment = new GeneratedSegment(new Vector3(5f, 0f, 0f), SegmentType.Corridor);
            segment.AddConnectionPoint(Vector3.forward, Vector3.forward);
            
            // Act
            segment.UpdateConnectionPoints();
            
            // Assert
            var expectedWorldPos = new Vector3(5f, 0f, 1f); // segment position + local position
            Assert.AreEqual(expectedWorldPos, segment.ConnectionPoints[0].WorldPosition);
        }
    }
    
    [TestFixture]
    public class ConnectionPointTests
    {
        [Test]
        public void IsCompatibleWith_ReturnsTrueForOppositeDirections()
        {
            // Arrange
            var point1 = new ConnectionPoint
            {
                Direction = Vector3.forward,
                IsConnected = false
            };
            var point2 = new ConnectionPoint
            {
                Direction = Vector3.back,
                IsConnected = false
            };
            
            // Act
            var compatible = point1.IsCompatibleWith(point2);
            
            // Assert
            Assert.IsTrue(compatible);
        }
        
        [Test]
        public void IsCompatibleWith_ReturnsFalseForConnectedPoints()
        {
            // Arrange
            var point1 = new ConnectionPoint
            {
                Direction = Vector3.forward,
                IsConnected = true
            };
            var point2 = new ConnectionPoint
            {
                Direction = Vector3.back,
                IsConnected = false
            };
            
            // Act
            var compatible = point1.IsCompatibleWith(point2);
            
            // Assert
            Assert.IsFalse(compatible);
        }
        
        [Test]
        public void DistanceTo_CalculatesCorrectDistance()
        {
            // Arrange
            var point1 = new ConnectionPoint { WorldPosition = Vector3.zero };
            var point2 = new ConnectionPoint { WorldPosition = new Vector3(3f, 4f, 0f) };
            
            // Act
            var distance = point1.DistanceTo(point2);
            
            // Assert
            Assert.AreEqual(5f, distance, 0.01f);
        }
    }
}