using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeonQuest.PlayerBehavior
{
    /// <summary>
    /// Analyzes player behavior patterns to predict intentions and provide context
    /// for environmental trigger evaluation
    /// </summary>
    public class PlayerBehaviorAnalyzer : MonoBehaviour
    {
        [Header("Analysis Configuration")]
        [SerializeField] private float analysisInterval = 0.5f;
        [SerializeField] private int behaviorHistorySize = 50;
        [SerializeField] private float intentionConfidenceThreshold = 0.7f;
        [SerializeField] private float patternCacheExpiration = 300f; // 5 minutes

        // Dependencies
        private PlayerMovementTracker movementTracker;

        // Behavior analysis data
        private Queue<BehaviorSnapshot> behaviorHistory;
        private Dictionary<PlayerIntention, float> intentionConfidences;
        private Dictionary<string, CachedPattern> patternCache;
        private BehaviorContext currentContext;

        // Analysis results
        private PlayerIntention predictedIntention;
        private float predictionConfidence;
        private Dictionary<string, float> environmentalFactors;

        // Events
        public event Action<PlayerIntention, float> OnIntentionPredicted;
        public event Action<BehaviorContext> OnContextUpdated;
        public event Action<Dictionary<string, float>> OnEnvironmentalFactorsUpdated;

        private void Awake()
        {
            behaviorHistory = new Queue<BehaviorSnapshot>();
            intentionConfidences = new Dictionary<PlayerIntention, float>();
            patternCache = new Dictionary<string, CachedPattern>();
            environmentalFactors = new Dictionary<string, float>();
            
            InitializeIntentionConfidences();
            InitializeEnvironmentalFactors();
        }

        private void Start()
        {
            movementTracker = GetComponent<PlayerMovementTracker>();
            if (movementTracker == null)
            {
                Debug.LogError("PlayerBehaviorAnalyzer requires PlayerMovementTracker component");
                enabled = false;
                return;
            }

            // Subscribe to movement tracker events
            movementTracker.OnMovementUpdate += OnMovementUpdate;
            movementTracker.OnPatternChanged += OnMovementPatternChanged;
            movementTracker.OnDwellTimeUpdate += OnDwellTimeUpdate;

            InvokeRepeating(nameof(AnalyzeBehavior), 0f, analysisInterval);
            InvokeRepeating(nameof(CleanupPatternCache), 60f, 60f);
        }

        private void InitializeIntentionConfidences()
        {
            foreach (PlayerIntention intention in Enum.GetValues(typeof(PlayerIntention)))
            {
                intentionConfidences[intention] = 0f;
            }
        }

        private void InitializeEnvironmentalFactors()
        {
            environmentalFactors["exploration_tendency"] = 0.5f;
            environmentalFactors["backtracking_frequency"] = 0f;
            environmentalFactors["dwell_preference"] = 0.3f;
            environmentalFactors["movement_consistency"] = 0.5f;
            environmentalFactors["spatial_awareness"] = 0.5f;
        }

        private void OnMovementUpdate(MovementData movementData)
        {
            // Create behavior snapshot
            var snapshot = new BehaviorSnapshot
            {
                Timestamp = Time.time,
                MovementData = movementData,
                EnvironmentalContext = GetCurrentEnvironmentalContext(),
                GameplayContext = GetCurrentGameplayContext()
            };

            AddBehaviorSnapshot(snapshot);
        }

        private void OnMovementPatternChanged(MovementPattern newPattern)
        {
            UpdateEnvironmentalFactors();
            UpdateBehaviorContext();
        }

        private void OnDwellTimeUpdate(float dwellTime)
        {
            // Update dwell preference factor
            float dwellFactor = Mathf.Clamp01(dwellTime / 60f); // Normalize to 1 minute
            environmentalFactors["dwell_preference"] = 
                Mathf.Lerp(environmentalFactors["dwell_preference"], dwellFactor, 0.1f);
        }

        private void AnalyzeBehavior()
        {
            if (behaviorHistory.Count < 5) return; // Need minimum data for analysis

            // Analyze movement patterns
            AnalyzeMovementPatterns();
            
            // Predict player intentions
            PredictPlayerIntention();
            
            // Update environmental factors
            UpdateEnvironmentalFactors();
            
            // Update behavior context
            UpdateBehaviorContext();
            
            // Cache patterns for future reference
            CacheCurrentPattern();
        }

        private void AnalyzeMovementPatterns()
        {
            var recentSnapshots = GetRecentSnapshots(10);
            if (recentSnapshots.Length == 0) return;

            // Analyze movement consistency
            float consistencyScore = CalculateMovementConsistency(recentSnapshots);
            environmentalFactors["movement_consistency"] = consistencyScore;

            // Analyze exploration tendency
            float explorationScore = CalculateExplorationTendency(recentSnapshots);
            environmentalFactors["exploration_tendency"] = explorationScore;

            // Analyze backtracking frequency
            float backtrackingScore = CalculateBacktrackingFrequency(recentSnapshots);
            environmentalFactors["backtracking_frequency"] = backtrackingScore;

            // Analyze spatial awareness
            float spatialScore = CalculateSpatialAwareness(recentSnapshots);
            environmentalFactors["spatial_awareness"] = spatialScore;
        }

        private void PredictPlayerIntention()
        {
            var recentSnapshots = GetRecentSnapshots(15);
            if (recentSnapshots.Length == 0) return;

            // Reset intention confidences
            foreach (var key in intentionConfidences.Keys.ToList())
            {
                intentionConfidences[key] = 0f;
            }

            // Calculate confidence for each intention
            intentionConfidences[PlayerIntention.Exploring] = CalculateExplorationIntention(recentSnapshots);
            intentionConfidences[PlayerIntention.Backtracking] = CalculateBacktrackingIntention(recentSnapshots);
            intentionConfidences[PlayerIntention.Searching] = CalculateSearchingIntention(recentSnapshots);
            intentionConfidences[PlayerIntention.Resting] = CalculateRestingIntention(recentSnapshots);
            intentionConfidences[PlayerIntention.Wandering] = CalculateWanderingIntention(recentSnapshots);

            // Find highest confidence intention
            var highestConfidence = intentionConfidences.OrderByDescending(kvp => kvp.Value).First();
            
            if (highestConfidence.Value >= intentionConfidenceThreshold)
            {
                if (predictedIntention != highestConfidence.Key || 
                    Mathf.Abs(predictionConfidence - highestConfidence.Value) > 0.1f)
                {
                    predictedIntention = highestConfidence.Key;
                    predictionConfidence = highestConfidence.Value;
                    OnIntentionPredicted?.Invoke(predictedIntention, predictionConfidence);
                }
            }
        }

        private float CalculateExplorationIntention(BehaviorSnapshot[] snapshots)
        {
            float explorationScore = 0f;
            int exploringCount = 0;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.Pattern == MovementPattern.Exploring)
                    exploringCount++;
                
                if (snapshot.MovementData.Speed > 2f && !snapshot.MovementData.IsBacktracking)
                    explorationScore += 0.1f;
            }
            
            float patternScore = (float)exploringCount / snapshots.Length;
            return Mathf.Clamp01((explorationScore + patternScore) / 2f);
        }

        private float CalculateBacktrackingIntention(BehaviorSnapshot[] snapshots)
        {
            int backtrackingCount = 0;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.IsBacktracking || 
                    snapshot.MovementData.Pattern == MovementPattern.Backtracking)
                    backtrackingCount++;
            }
            
            return Mathf.Clamp01((float)backtrackingCount / snapshots.Length);
        }

        private float CalculateSearchingIntention(BehaviorSnapshot[] snapshots)
        {
            float searchScore = 0f;
            int dwellingCount = 0;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.DwellTime > 5f && snapshot.MovementData.DwellTime < 30f)
                    dwellingCount++;
                
                if (snapshot.MovementData.Speed < 1f && snapshot.MovementData.Speed > 0.1f)
                    searchScore += 0.1f;
            }
            
            float dwellScore = (float)dwellingCount / snapshots.Length;
            return Mathf.Clamp01((searchScore + dwellScore) / 2f);
        }

        private float CalculateRestingIntention(BehaviorSnapshot[] snapshots)
        {
            int stationaryCount = 0;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.Pattern == MovementPattern.Stationary ||
                    snapshot.MovementData.DwellTime > 30f)
                    stationaryCount++;
            }
            
            return Mathf.Clamp01((float)stationaryCount / snapshots.Length);
        }

        private float CalculateWanderingIntention(BehaviorSnapshot[] snapshots)
        {
            int wanderingCount = 0;
            float directionChanges = 0f;
            Vector3 lastDirection = Vector3.zero;
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.Pattern == MovementPattern.Wandering)
                    wanderingCount++;
                
                if (lastDirection != Vector3.zero)
                {
                    float angle = Vector3.Angle(lastDirection, snapshot.MovementData.Direction);
                    if (angle > 45f) directionChanges++;
                }
                lastDirection = snapshot.MovementData.Direction;
            }
            
            float patternScore = (float)wanderingCount / snapshots.Length;
            float changeScore = directionChanges / snapshots.Length;
            return Mathf.Clamp01((patternScore + changeScore * 0.5f) / 1.5f);
        }

        private float CalculateMovementConsistency(BehaviorSnapshot[] snapshots)
        {
            if (snapshots.Length < 2) return 0.5f;
            
            float totalVariation = 0f;
            for (int i = 1; i < snapshots.Length; i++)
            {
                float speedDiff = Mathf.Abs(snapshots[i].MovementData.Speed - snapshots[i-1].MovementData.Speed);
                totalVariation += speedDiff;
            }
            
            float averageVariation = totalVariation / (snapshots.Length - 1);
            return Mathf.Clamp01(1f - (averageVariation / 10f)); // Normalize against max expected variation
        }

        private float CalculateExplorationTendency(BehaviorSnapshot[] snapshots)
        {
            int explorationCount = 0;
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.Pattern == MovementPattern.Exploring)
                    explorationCount++;
            }
            return (float)explorationCount / snapshots.Length;
        }

        private float CalculateBacktrackingFrequency(BehaviorSnapshot[] snapshots)
        {
            int backtrackingCount = 0;
            foreach (var snapshot in snapshots)
            {
                if (snapshot.MovementData.IsBacktracking)
                    backtrackingCount++;
            }
            return (float)backtrackingCount / snapshots.Length;
        }

        private float CalculateSpatialAwareness(BehaviorSnapshot[] snapshots)
        {
            // Simple heuristic: players with higher spatial awareness tend to have more consistent movement patterns
            // and less random direction changes
            return environmentalFactors["movement_consistency"];
        }

        private void UpdateBehaviorContext()
        {
            currentContext = new BehaviorContext
            {
                PredictedIntention = predictedIntention,
                IntentionConfidence = predictionConfidence,
                EnvironmentalFactors = new Dictionary<string, float>(environmentalFactors),
                CurrentMovementData = movementTracker.GetCurrentMovementData(),
                AnalysisTimestamp = Time.time
            };
            
            OnContextUpdated?.Invoke(currentContext);
        }

        private void UpdateEnvironmentalFactors()
        {
            OnEnvironmentalFactorsUpdated?.Invoke(new Dictionary<string, float>(environmentalFactors));
        }

        private void AddBehaviorSnapshot(BehaviorSnapshot snapshot)
        {
            behaviorHistory.Enqueue(snapshot);
            
            while (behaviorHistory.Count > behaviorHistorySize)
            {
                behaviorHistory.Dequeue();
            }
        }

        private BehaviorSnapshot[] GetRecentSnapshots(int count)
        {
            var snapshots = behaviorHistory.ToArray();
            int startIndex = Mathf.Max(0, snapshots.Length - count);
            var result = new BehaviorSnapshot[snapshots.Length - startIndex];
            Array.Copy(snapshots, startIndex, result, 0, result.Length);
            return result;
        }

        private EnvironmentalContext GetCurrentEnvironmentalContext()
        {
            return new EnvironmentalContext
            {
                Position = transform.position,
                NearbyObjects = GetNearbyObjectTypes(),
                LightingLevel = GetCurrentLightingLevel(),
                AmbientSoundLevel = GetCurrentAmbientSoundLevel()
            };
        }

        private GameplayContext GetCurrentGameplayContext()
        {
            return new GameplayContext
            {
                GameTime = Time.time,
                SessionDuration = Time.time, // Simplified - would need proper session tracking
                CurrentObjective = "explore" // Simplified - would need proper objective tracking
            };
        }

        private List<string> GetNearbyObjectTypes()
        {
            // Simplified implementation - would use proper object detection
            return new List<string> { "corridor", "junction", "neon_sign" };
        }

        private float GetCurrentLightingLevel()
        {
            // Simplified implementation - would use proper lighting detection
            return 0.5f;
        }

        private float GetCurrentAmbientSoundLevel()
        {
            // Simplified implementation - would use proper audio level detection
            return 0.3f;
        }

        private void CacheCurrentPattern()
        {
            string patternKey = GeneratePatternKey();
            var cachedPattern = new CachedPattern
            {
                IntentionConfidences = new Dictionary<PlayerIntention, float>(intentionConfidences),
                EnvironmentalFactors = new Dictionary<string, float>(environmentalFactors),
                CacheTime = Time.time
            };
            
            patternCache[patternKey] = cachedPattern;
        }

        private string GeneratePatternKey()
        {
            // Generate a key based on current context for pattern caching
            var context = GetCurrentEnvironmentalContext();
            return $"{context.Position.x:F0}_{context.Position.z:F0}_{predictedIntention}";
        }

        private void CleanupPatternCache()
        {
            var expiredKeys = patternCache.Where(kvp => 
                Time.time - kvp.Value.CacheTime > patternCacheExpiration)
                .Select(kvp => kvp.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                patternCache.Remove(key);
            }
        }

        // Public API methods
        public BehaviorContext GetCurrentBehaviorContext()
        {
            return currentContext;
        }

        public PlayerIntention GetPredictedIntention()
        {
            return predictedIntention;
        }

        public float GetIntentionConfidence()
        {
            return predictionConfidence;
        }

        public Dictionary<string, float> GetEnvironmentalFactors()
        {
            return new Dictionary<string, float>(environmentalFactors);
        }

        public BehaviorSnapshot[] GetBehaviorHistory(int count = -1)
        {
            var snapshots = behaviorHistory.ToArray();
            if (count <= 0 || count >= snapshots.Length)
                return snapshots;
            
            int startIndex = snapshots.Length - count;
            var result = new BehaviorSnapshot[count];
            Array.Copy(snapshots, startIndex, result, 0, count);
            return result;
        }

        private void OnDestroy()
        {
            CancelInvoke();
            
            if (movementTracker != null)
            {
                movementTracker.OnMovementUpdate -= OnMovementUpdate;
                movementTracker.OnPatternChanged -= OnMovementPatternChanged;
                movementTracker.OnDwellTimeUpdate -= OnDwellTimeUpdate;
            }
        }
    }

    [Serializable]
    public struct BehaviorSnapshot
    {
        public float Timestamp;
        public MovementData MovementData;
        public EnvironmentalContext EnvironmentalContext;
        public GameplayContext GameplayContext;
    }

    [Serializable]
    public struct BehaviorContext
    {
        public PlayerIntention PredictedIntention;
        public float IntentionConfidence;
        public Dictionary<string, float> EnvironmentalFactors;
        public MovementData CurrentMovementData;
        public float AnalysisTimestamp;
    }

    [Serializable]
    public struct EnvironmentalContext
    {
        public Vector3 Position;
        public List<string> NearbyObjects;
        public float LightingLevel;
        public float AmbientSoundLevel;
    }

    [Serializable]
    public struct GameplayContext
    {
        public float GameTime;
        public float SessionDuration;
        public string CurrentObjective;
    }

    [Serializable]
    public struct CachedPattern
    {
        public Dictionary<PlayerIntention, float> IntentionConfidences;
        public Dictionary<string, float> EnvironmentalFactors;
        public float CacheTime;
    }

    public enum PlayerIntention
    {
        Exploring,
        Backtracking,
        Searching,
        Resting,
        Wandering
    }
}