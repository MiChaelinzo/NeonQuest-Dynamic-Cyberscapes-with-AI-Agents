using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonQuest.PlayerBehavior
{
    /// <summary>
    /// Tracks player movement patterns, position changes, and spatial context
    /// for environmental trigger evaluation
    /// </summary>
    public class PlayerMovementTracker : MonoBehaviour
    {
        [Header("Tracking Configuration")]
        [SerializeField] private float trackingInterval = 0.1f;
        [SerializeField] private float dwellTimeThreshold = 30f;
        [SerializeField] private float backtrackingDistanceThreshold = 5f;
        [SerializeField] private int movementHistorySize = 100;

        // Movement data
        private Vector3 lastPosition;
        private Vector3 currentVelocity;
        private float currentSpeed;
        private Vector3 movementDirection;
        
        // Pattern detection
        private Queue<MovementSample> movementHistory;
        private float dwellStartTime;
        private bool isDwelling;
        private MovementPattern currentPattern;
        
        // Spatial context
        private Dictionary<Vector3, float> visitedPositions;
        private float lastTrackingTime;

        // Events
        public event Action<MovementData> OnMovementUpdate;
        public event Action<MovementPattern> OnPatternChanged;
        public event Action<float> OnDwellTimeUpdate;

        private void Awake()
        {
            movementHistory = new Queue<MovementSample>();
            visitedPositions = new Dictionary<Vector3, float>();
            lastPosition = transform.position;
            currentPattern = MovementPattern.Stationary;
        }

        private void Start()
        {
            InvokeRepeating(nameof(TrackMovement), 0f, trackingInterval);
        }

        private void TrackMovement()
        {
            Vector3 currentPosition = transform.position;
            float currentTime = Time.time;
            
            // Calculate velocity and speed
            Vector3 positionDelta = currentPosition - lastPosition;
            float timeDelta = currentTime - lastTrackingTime;
            
            if (timeDelta > 0)
            {
                currentVelocity = positionDelta / timeDelta;
                currentSpeed = currentVelocity.magnitude;
                movementDirection = currentSpeed > 0.1f ? currentVelocity.normalized : Vector3.zero;
            }

            // Update movement history
            var sample = new MovementSample
            {
                Position = currentPosition,
                Velocity = currentVelocity,
                Speed = currentSpeed,
                Direction = movementDirection,
                Timestamp = currentTime
            };
            
            AddMovementSample(sample);
            
            // Update spatial context
            UpdateSpatialContext(currentPosition, currentTime);
            
            // Detect movement patterns
            DetectMovementPattern();
            
            // Update dwell time
            UpdateDwellTime(currentSpeed, currentTime);
            
            // Create movement data for events
            var movementData = new MovementData
            {
                Position = currentPosition,
                Velocity = currentVelocity,
                Speed = currentSpeed,
                Direction = movementDirection,
                Pattern = currentPattern,
                DwellTime = isDwelling ? currentTime - dwellStartTime : 0f,
                IsBacktracking = IsBacktracking()
            };
            
            OnMovementUpdate?.Invoke(movementData);
            
            lastPosition = currentPosition;
            lastTrackingTime = currentTime;
        }

        private void AddMovementSample(MovementSample sample)
        {
            movementHistory.Enqueue(sample);
            
            while (movementHistory.Count > movementHistorySize)
            {
                movementHistory.Dequeue();
            }
        }

        private void UpdateSpatialContext(Vector3 position, float time)
        {
            // Round position to grid for spatial tracking
            Vector3 gridPosition = new Vector3(
                Mathf.Round(position.x / 2f) * 2f,
                Mathf.Round(position.y / 2f) * 2f,
                Mathf.Round(position.z / 2f) * 2f
            );
            
            visitedPositions[gridPosition] = time;
        }

        private void DetectMovementPattern()
        {
            MovementPattern newPattern = AnalyzeMovementPattern();
            
            if (newPattern != currentPattern)
            {
                currentPattern = newPattern;
                OnPatternChanged?.Invoke(currentPattern);
            }
        }

        private MovementPattern AnalyzeMovementPattern()
        {
            if (currentSpeed < 0.1f)
                return MovementPattern.Stationary;
            
            if (IsBacktracking())
                return MovementPattern.Backtracking;
            
            if (IsExploring())
                return MovementPattern.Exploring;
            
            return MovementPattern.Wandering;
        }

        private bool IsBacktracking()
        {
            if (movementHistory.Count < 10) return false;
            
            var samples = new MovementSample[movementHistory.Count];
            movementHistory.CopyTo(samples, 0);
            
            // Check if current movement is towards previously visited positions
            Vector3 currentPos = transform.position;
            Vector3 futurePos = currentPos + movementDirection * 5f;
            
            foreach (var kvp in visitedPositions)
            {
                if (Time.time - kvp.Value > 10f) // Only consider recent visits
                {
                    float distanceToVisited = Vector3.Distance(futurePos, kvp.Key);
                    if (distanceToVisited < backtrackingDistanceThreshold)
                        return true;
                }
            }
            
            return false;
        }

        private bool IsExploring()
        {
            if (movementHistory.Count < 5) return true; // Assume exploration initially
            
            // Check if player is moving towards unvisited areas
            Vector3 currentPos = transform.position;
            Vector3 futurePos = currentPos + movementDirection * 10f;
            
            float minDistanceToVisited = float.MaxValue;
            foreach (var kvp in visitedPositions)
            {
                float distance = Vector3.Distance(futurePos, kvp.Key);
                minDistanceToVisited = Mathf.Min(minDistanceToVisited, distance);
            }
            
            return minDistanceToVisited > backtrackingDistanceThreshold * 2f;
        }

        private void UpdateDwellTime(float speed, float currentTime)
        {
            bool wasStationary = speed < 0.1f;
            
            if (wasStationary && !isDwelling)
            {
                isDwelling = true;
                dwellStartTime = currentTime;
            }
            else if (!wasStationary && isDwelling)
            {
                isDwelling = false;
            }
            
            if (isDwelling)
            {
                float dwellTime = currentTime - dwellStartTime;
                OnDwellTimeUpdate?.Invoke(dwellTime);
            }
        }

        // Public API methods
        public MovementData GetCurrentMovementData()
        {
            return new MovementData
            {
                Position = transform.position,
                Velocity = currentVelocity,
                Speed = currentSpeed,
                Direction = movementDirection,
                Pattern = currentPattern,
                DwellTime = isDwelling ? Time.time - dwellStartTime : 0f,
                IsBacktracking = IsBacktracking()
            };
        }

        public float GetDwellTime()
        {
            return isDwelling ? Time.time - dwellStartTime : 0f;
        }

        public bool HasVisitedPosition(Vector3 position, float radius = 5f)
        {
            foreach (var kvp in visitedPositions)
            {
                if (Vector3.Distance(position, kvp.Key) <= radius)
                    return true;
            }
            return false;
        }

        public MovementSample[] GetRecentMovementHistory(int sampleCount = 10)
        {
            var samples = new MovementSample[movementHistory.Count];
            movementHistory.CopyTo(samples, 0);
            
            int startIndex = Mathf.Max(0, samples.Length - sampleCount);
            var result = new MovementSample[samples.Length - startIndex];
            Array.Copy(samples, startIndex, result, 0, result.Length);
            
            return result;
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }

    [Serializable]
    public struct MovementSample
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Speed;
        public Vector3 Direction;
        public float Timestamp;
    }

    [Serializable]
    public struct MovementData
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Speed;
        public Vector3 Direction;
        public MovementPattern Pattern;
        public float DwellTime;
        public bool IsBacktracking;
    }

    public enum MovementPattern
    {
        Stationary,
        Exploring,
        Backtracking,
        Wandering
    }
}