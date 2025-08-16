using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.Configuration;
using NeonQuest.Assets;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Manages procedural generation of corridors, rooms, and junction points
    /// with spatial consistency and connectivity
    /// </summary>
    public class LayoutManager : MonoBehaviour, IProceduralGenerator
    {
        [Header("Configuration")]
        [SerializeField] private float generationDistance = 50f;
        [SerializeField] private float cleanupDistance = 100f;
        [SerializeField] private int maxActiveSegments = 20;
        [SerializeField] private float segmentLength = 10f;
        [SerializeField] private float segmentWidth = 4f;
        
        [Header("Asset References")]
        [SerializeField] private GameObject corridorPrefab;
        [SerializeField] private GameObject roomPrefab;
        [SerializeField] private GameObject junctionPrefab;
        
        private Dictionary<string, GeneratedSegment> activeSegments;
        private List<Vector3> generatedPositions;
        private IAssetIntegrator assetIntegrator;
        private EnvironmentConfiguration config;
        private bool isActive = true;
        private float currentPerformanceCost = 0f;
        private float qualityLevel = 1f;
        
        public float CurrentPerformanceCost => currentPerformanceCost;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            activeSegments = new Dictionary<string, GeneratedSegment>();
            generatedPositions = new List<Vector3>();
            assetIntegrator = FindObjectOfType<AssetIntegrator>();
        }
        
        public void Initialize(Dictionary<string, object> configData)
        {
            if (configData.TryGetValue("config", out var configObj) && configObj is EnvironmentConfiguration envConfig)
            {
                config = envConfig;
                generationDistance = config.CorridorGenerationDistance;
                cleanupDistance = config.CorridorCleanupDistance;
                maxActiveSegments = config.MaxActiveSegments;
            }
            
            if (assetIntegrator == null)
            {
                Debug.LogWarning("LayoutManager: No AssetIntegrator found, using fallback prefabs");
            }
        }      
  
        public async Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            if (!isActive) return null;
            
            Vector3 targetPosition = Vector3.zero;
            SegmentType segmentType = SegmentType.Corridor;
            
            if (generationParams.TryGetValue("position", out var posObj) && posObj is Vector3 pos)
                targetPosition = pos;
            if (generationParams.TryGetValue("type", out var typeObj) && typeObj is SegmentType type)
                segmentType = type;
            
            // Check if we should generate at this position
            if (!ShouldGenerateAt(targetPosition))
                return null;
            
            // Generate the segment
            var segment = await GenerateSegmentAsync(targetPosition, segmentType);
            if (segment != null)
            {
                activeSegments[segment.SegmentId] = segment;
                generatedPositions.Add(targetPosition);
                UpdatePerformanceCost();
                return segment.SegmentObject;
            }
            
            return null;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            if (!isActive) return;
            
            Vector3 playerPosition = Vector3.zero;
            if (environmentState.TryGetValue("playerPosition", out var posObj) && posObj is Vector3 pos)
                playerPosition = pos;
            
            // Generate new segments around player
            GenerateAroundPlayer(playerPosition);
            
            // Update connection points for all segments
            foreach (var segment in activeSegments.Values)
            {
                segment.UpdateConnectionPoints();
            }
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            var segmentsToRemove = new List<string>();
            
            foreach (var kvp in activeSegments)
            {
                if (kvp.Value.DistanceTo(playerPosition) > cleanupDistance)
                {
                    segmentsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var segmentId in segmentsToRemove)
            {
                RemoveSegment(segmentId);
            }
            
            UpdatePerformanceCost();
        } 
       
        public void SetQualityLevel(float qualityLevel)
        {
            this.qualityLevel = Mathf.Clamp01(qualityLevel);
            // Adjust generation parameters based on quality
            maxActiveSegments = Mathf.RoundToInt(config?.MaxActiveSegments ?? 20 * this.qualityLevel);
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }
        
        private bool ShouldGenerateAt(Vector3 position)
        {
            // Don't generate if too close to existing segments
            float minDistance = segmentLength * 0.8f;
            return !generatedPositions.Any(pos => Vector3.Distance(pos, position) < minDistance);
        }
        
        private async Task<GeneratedSegment> GenerateSegmentAsync(Vector3 position, SegmentType type)
        {
            GameObject prefab = GetPrefabForType(type);
            if (prefab == null) return null;
            
            // Create the segment
            var segment = new GeneratedSegment(position, type);
            
            // Instantiate the prefab
            GameObject segmentObject;
            if (assetIntegrator != null)
            {
                var assetRef = new AssetReference { Prefab = prefab };
                segmentObject = await assetIntegrator.InstantiateAssetAsync(assetRef, position, Quaternion.identity);
            }
            else
            {
                segmentObject = Instantiate(prefab, position, Quaternion.identity);
            }
            
            if (segmentObject == null) return null;
            
            segment.SegmentObject = segmentObject;
            segment.Rotation = segmentObject.transform.rotation;
            
            // Add connection points based on segment type
            AddConnectionPoints(segment);
            
            // Try to connect to nearby segments
            ConnectToNearbySegments(segment);
            
            return segment;
        }
        
        private GameObject GetPrefabForType(SegmentType type)
        {
            return type switch
            {
                SegmentType.Corridor => corridorPrefab,
                SegmentType.Room => roomPrefab,
                SegmentType.Junction => junctionPrefab,
                _ => corridorPrefab
            };
        }   
     
        private void AddConnectionPoints(GeneratedSegment segment)
        {
            switch (segment.Type)
            {
                case SegmentType.Corridor:
                    // Corridors have connection points at both ends
                    segment.AddConnectionPoint(Vector3.forward * segmentLength * 0.5f, Vector3.forward);
                    segment.AddConnectionPoint(Vector3.back * segmentLength * 0.5f, Vector3.back);
                    break;
                    
                case SegmentType.Room:
                    // Rooms can have multiple connection points
                    segment.AddConnectionPoint(Vector3.forward * segmentLength * 0.5f, Vector3.forward);
                    segment.AddConnectionPoint(Vector3.back * segmentLength * 0.5f, Vector3.back);
                    segment.AddConnectionPoint(Vector3.right * segmentWidth * 0.5f, Vector3.right);
                    segment.AddConnectionPoint(Vector3.left * segmentWidth * 0.5f, Vector3.left);
                    break;
                    
                case SegmentType.Junction:
                    // Junctions have connection points in multiple directions
                    segment.AddConnectionPoint(Vector3.forward * segmentLength * 0.5f, Vector3.forward);
                    segment.AddConnectionPoint(Vector3.back * segmentLength * 0.5f, Vector3.back);
                    segment.AddConnectionPoint(Vector3.right * segmentWidth * 0.5f, Vector3.right);
                    segment.AddConnectionPoint(Vector3.left * segmentWidth * 0.5f, Vector3.left);
                    break;
            }
        }
        
        private void ConnectToNearbySegments(GeneratedSegment newSegment)
        {
            float connectionDistance = segmentLength * 1.2f;
            
            foreach (var existingSegment in activeSegments.Values)
            {
                if (existingSegment.DistanceTo(newSegment.Position) > connectionDistance)
                    continue;
                
                // Try to connect compatible connection points
                var newPoints = newSegment.GetAvailableConnectionPoints();
                var existingPoints = existingSegment.GetAvailableConnectionPoints();
                
                foreach (var newPoint in newPoints)
                {
                    foreach (var existingPoint in existingPoints)
                    {
                        if (newPoint.IsCompatibleWith(existingPoint) && 
                            newPoint.DistanceTo(existingPoint) < segmentLength * 0.6f)
                        {
                            // Create connection
                            newPoint.IsConnected = true;
                            newPoint.ConnectedSegmentId = existingSegment.SegmentId;
                            existingPoint.IsConnected = true;
                            existingPoint.ConnectedSegmentId = newSegment.SegmentId;
                            return; // Only connect one point per segment for now
                        }
                    }
                }
            }
        }        

        private void GenerateAroundPlayer(Vector3 playerPosition)
        {
            if (activeSegments.Count >= maxActiveSegments)
                return;
            
            // Generate segments in a grid pattern around the player
            int gridSize = Mathf.RoundToInt(generationDistance / segmentLength);
            
            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int z = -gridSize; z <= gridSize; z++)
                {
                    Vector3 gridPosition = playerPosition + new Vector3(x * segmentLength, 0, z * segmentLength);
                    
                    if (Vector3.Distance(gridPosition, playerPosition) > generationDistance)
                        continue;
                    
                    if (ShouldGenerateAt(gridPosition))
                    {
                        // Determine segment type based on position and context
                        SegmentType type = DetermineSegmentType(gridPosition, playerPosition);
                        
                        // Generate asynchronously (fire and forget for performance)
                        _ = GenerateAsync(new Dictionary<string, object>
                        {
                            ["position"] = gridPosition,
                            ["type"] = type
                        });
                        
                        // Limit generation per frame for performance
                        if (activeSegments.Count >= maxActiveSegments)
                            break;
                    }
                }
                if (activeSegments.Count >= maxActiveSegments)
                    break;
            }
        }
        
        private SegmentType DetermineSegmentType(Vector3 position, Vector3 playerPosition)
        {
            // Simple heuristic: create junctions at intersections, rooms occasionally
            float distanceFromPlayer = Vector3.Distance(position, playerPosition);
            
            // Create junctions at regular intervals
            if (Mathf.Abs(position.x % (segmentLength * 3)) < 0.1f && 
                Mathf.Abs(position.z % (segmentLength * 3)) < 0.1f)
            {
                return SegmentType.Junction;
            }
            
            // Create rooms occasionally, more likely further from player
            if (distanceFromPlayer > generationDistance * 0.5f && Random.value < 0.2f)
            {
                return SegmentType.Room;
            }
            
            return SegmentType.Corridor;
        } 
       
        private void RemoveSegment(string segmentId)
        {
            if (activeSegments.TryGetValue(segmentId, out var segment))
            {
                // Disconnect from other segments
                foreach (var connectionPoint in segment.ConnectionPoints)
                {
                    if (connectionPoint.IsConnected && 
                        activeSegments.TryGetValue(connectionPoint.ConnectedSegmentId, out var connectedSegment))
                    {
                        var connectedPoint = connectedSegment.ConnectionPoints
                            .FirstOrDefault(cp => cp.ConnectedSegmentId == segmentId);
                        if (connectedPoint != null)
                        {
                            connectedPoint.IsConnected = false;
                            connectedPoint.ConnectedSegmentId = null;
                        }
                    }
                }
                
                // Destroy the game object
                if (segment.SegmentObject != null)
                {
                    if (assetIntegrator != null)
                    {
                        assetIntegrator.ReturnToPool(segment.SegmentObject);
                    }
                    else
                    {
                        Destroy(segment.SegmentObject);
                    }
                }
                
                // Remove from collections
                activeSegments.Remove(segmentId);
                generatedPositions.Remove(segment.Position);
            }
        }
        
        private void UpdatePerformanceCost()
        {
            // Calculate performance cost based on active segments and their complexity
            float baseCost = activeSegments.Count / (float)maxActiveSegments;
            float complexityCost = 0f;
            
            foreach (var segment in activeSegments.Values)
            {
                // Add cost based on segment type complexity
                complexityCost += segment.Type switch
                {
                    SegmentType.Corridor => 0.1f,
                    SegmentType.Room => 0.2f,
                    SegmentType.Junction => 0.3f,
                    _ => 0.1f
                };
            }
            
            currentPerformanceCost = Mathf.Clamp01(baseCost + complexityCost / maxActiveSegments);
        }
        
        /// <summary>
        /// Gets all active segments for debugging or external access
        /// </summary>
        public IReadOnlyDictionary<string, GeneratedSegment> GetActiveSegments()
        {
            return activeSegments;
        }
        
        /// <summary>
        /// Gets segments within a certain distance of a position
        /// </summary>
        public List<GeneratedSegment> GetSegmentsNear(Vector3 position, float distance)
        {
            return activeSegments.Values
                .Where(segment => segment.IsWithinDistance(position, distance))
                .ToList();
        }
    }
}