using UnityEngine;
using System;
using System.Collections.Generic;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Represents a generated corridor or room segment in the environment
    /// </summary>
    [Serializable]
    public class GeneratedSegment
    {
        public string SegmentId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; } = Vector3.one;
        public SegmentType Type { get; set; }
        public GameObject SegmentObject { get; set; }
        public List<ConnectionPoint> ConnectionPoints { get; set; } = new List<ConnectionPoint>();
        public float GenerationTime { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        public GeneratedSegment()
        {
            SegmentId = Guid.NewGuid().ToString();
            GenerationTime = Time.time;
        }
        
        public GeneratedSegment(Vector3 position, SegmentType type) : this()
        {
            Position = position;
            Type = type;
        }
        
        /// <summary>
        /// Gets the distance from this segment to a given position
        /// </summary>
        public float DistanceTo(Vector3 position)
        {
            return Vector3.Distance(Position, position);
        }
        
        /// <summary>
        /// Checks if this segment is within a given distance of a position
        /// </summary>
        public bool IsWithinDistance(Vector3 position, float distance)
        {
            return DistanceTo(position) <= distance;
        }
        
        /// <summary>
        /// Gets available connection points that aren't already connected
        /// </summary>
        public List<ConnectionPoint> GetAvailableConnectionPoints()
        {
            return ConnectionPoints.FindAll(cp => !cp.IsConnected);
        }
        
        /// <summary>
        /// Adds a connection point to this segment
        /// </summary>
        public void AddConnectionPoint(Vector3 localPosition, Vector3 direction)
        {
            ConnectionPoints.Add(new ConnectionPoint
            {
                LocalPosition = localPosition,
                Direction = direction,
                WorldPosition = Position + Rotation * localPosition
            });
        }
        
        /// <summary>
        /// Updates world positions of connection points based on segment transform
        /// </summary>
        public void UpdateConnectionPoints()
        {
            foreach (var cp in ConnectionPoints)
            {
                cp.WorldPosition = Position + Rotation * cp.LocalPosition;
            }
        }
    }
    
    /// <summary>
    /// Types of generated segments
    /// </summary>
    public enum SegmentType
    {
        Corridor,
        Room,
        Junction,
        Intersection,
        DeadEnd
    }
    
    /// <summary>
    /// Represents a connection point where segments can be joined
    /// </summary>
    [Serializable]
    public class ConnectionPoint
    {
        public Vector3 LocalPosition { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Vector3 Direction { get; set; }
        public bool IsConnected { get; set; }
        public string ConnectedSegmentId { get; set; }
        
        /// <summary>
        /// Checks if this connection point is compatible with another
        /// </summary>
        public bool IsCompatibleWith(ConnectionPoint other, float toleranceAngle = 15f)
        {
            if (IsConnected || other.IsConnected)
                return false;
                
            // Check if directions are roughly opposite (for connecting)
            float angle = Vector3.Angle(Direction, -other.Direction);
            return angle <= toleranceAngle;
        }
        
        /// <summary>
        /// Gets the distance to another connection point
        /// </summary>
        public float DistanceTo(ConnectionPoint other)
        {
            return Vector3.Distance(WorldPosition, other.WorldPosition);
        }
    }
}