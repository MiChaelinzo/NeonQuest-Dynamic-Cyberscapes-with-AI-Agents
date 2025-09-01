using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.AI
{
    /// <summary>
    /// Advanced predictive analytics engine for NPCs
    /// Analyzes player behavior patterns to predict future actions
    /// </summary>
    public class PredictiveAnalyticsEngine : System.IDisposable
    {
        private List<BehaviorPattern> learnedPatterns;
        private Dictionary<string, PredictionModel> predictionModels;
        private TemporalAnalyzer temporalAnalyzer;
        private PatternRecognitionSystem patternRecognition;
        
        public PredictiveAnalyticsEngine()
        {
            InitializePredictiveSystem();
        }
        
        private void InitializePredictiveSystem()
        {
            learnedPatterns = new List<BehaviorPattern>();
            predictionModels = new Dictionary<string, PredictionModel>();
            temporalAnalyzer = new TemporalAnalyzer();
            patternRecognition = new PatternRecognitionSystem();
            
            // Initialize prediction models
            InitializePredictionModels();
        }
        
        private void InitializePredictionModels()
        {
            predictionModels["movement"] = new PredictionModel("PlayerMovement", 10);
            predictionModels["interaction"] = new PredictionModel("PlayerInteraction", 8);
            predictionModels["exploration"] = new PredictionModel("PlayerExploration", 12);
            predictionModels["emotional"] = new PredictionModel("PlayerEmotion", 6);
        }
        
        public List<BehaviorPrediction> PredictPlayerBehavior(PlayerInteraction[] interactions, NPCPersonality personality)
        {
            var predictions = new List<BehaviorPrediction>();
            
            if (interactions.Length < 5) return predictions;
            
            // Analyze temporal patterns
            var temporalPatterns = temporalAnalyzer.AnalyzeTemporalPatterns(interactions);
            
            // Generate movement predictions
            var movementPredictions = PredictMovementBehavior(interactions, temporalPatterns);
            predictions.AddRange(movementPredictions);
            
            // Generate interaction predictions
            var interactionPredictions = PredictInteractionBehavior(interactions, personality);
            predictions.AddRange(interactionPredictions);
            
            // Generate exploration predictions
            var explorationPredictions = PredictExplorationBehavior(interactions);
            predictions.AddRange(explorationPredictions);
            
            // Generate emotional predictions
            var emotionalPredictions = PredictEmotionalBehavior(interactions);
            predictions.AddRange(emotionalPredictions);
            
            // Filter and rank predictions by confidence
            return predictions.Where(p => p.confidence > 0.6f)
                           .OrderByDescending(p => p.confidence)
                           .Take(5)
                           .ToList();
        }
        
        private List<BehaviorPrediction> PredictMovementBehavior(PlayerInteraction[] interactions, List<TemporalPattern> patterns)
        {
            var predictions = new List<BehaviorPrediction>();
            var movementModel = predictionModels["movement"];
            
            // Analyze recent movement patterns
            var recentInteractions = interactions.TakeLast(5).ToArray();
            float averageSpeed = recentInteractions.Average(i => i.playerSpeed);
            float speedVariance = CalculateVariance(recentInteractions.Select(i => i.playerSpeed).ToArray());
            
            // Predict approach behavior
            if (averageSpeed > 3f && speedVariance < 1f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "player_approach",
                    confidence = CalculateMovementConfidence(averageSpeed, speedVariance, patterns),
                    timeframe = 3f,
                    parameters = new Dictionary<string, object>
                    {
                        { "expectedSpeed", averageSpeed },
                        { "approachVector", CalculateApproachVector(recentInteractions) }
                    }
                });
            }
            
            // Predict avoidance behavior
            if (speedVariance > 2f && averageSpeed > 4f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "player_avoid",
                    confidence = CalculateAvoidanceConfidence(speedVariance, patterns),
                    timeframe = 2f,
                    parameters = new Dictionary<string, object>
                    {
                        { "avoidanceIntensity", speedVariance },
                        { "escapeDirection", CalculateEscapeDirection(recentInteractions) }
                    }
                });
            }
            
            // Predict stationary behavior
            if (averageSpeed < 1f && speedVariance < 0.5f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "player_stationary",
                    confidence = 0.8f,
                    timeframe = 5f,
                    parameters = new Dictionary<string, object>
                    {
                        { "stationaryDuration", EstimateStationaryDuration(recentInteractions) }
                    }
                });
            }
            
            return predictions;
        }
        
        private List<BehaviorPrediction> PredictInteractionBehavior(PlayerInteraction[] interactions, NPCPersonality personality)
        {
            var predictions = new List<BehaviorPrediction>();
            var interactionModel = predictionModels["interaction"];
            
            // Analyze interaction quality trends
            var qualityTrend = CalculateInteractionQualityTrend(interactions);
            var engagementLevel = interactions.TakeLast(3).Average(i => i.playerEngagement);
            
            // Predict positive interaction
            if (qualityTrend > 0.1f && engagementLevel > 0.6f)
            {
                float confidence = CalculateInteractionConfidence(qualityTrend, engagementLevel, personality);
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "positive_interaction",
                    confidence = confidence,
                    timeframe = 4f,
                    parameters = new Dictionary<string, object>
                    {
                        { "expectedEngagement", engagementLevel },
                        { "interactionType", DetermineInteractionType(personality) }
                    }
                });
            }
            
            // Predict interaction avoidance
            if (qualityTrend < -0.1f || engagementLevel < 0.3f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "interaction_avoidance",
                    confidence = 0.7f,
                    timeframe = 6f,
                    parameters = new Dictionary<string, object>
                    {
                        { "avoidanceReason", qualityTrend < -0.1f ? "negative_trend" : "low_engagement" }
                    }
                });
            }
            
            return predictions;
        }
        
        private List<BehaviorPrediction> PredictExplorationBehavior(PlayerInteraction[] interactions)
        {
            var predictions = new List<BehaviorPrediction>();
            var explorationModel = predictionModels["exploration"];
            
            // Analyze exploration patterns
            var explorationIndices = interactions.Select(i => CalculateExplorationIndex(i)).ToArray();
            var explorationTrend = CalculateTrend(explorationIndices);
            var currentExploration = explorationIndices.LastOrDefault();
            
            // Predict exploration increase
            if (explorationTrend > 0.05f && currentExploration > 0.5f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "player_explore",
                    confidence = Mathf.Clamp01(explorationTrend * 10f),
                    timeframe = 8f,
                    parameters = new Dictionary<string, object>
                    {
                        { "explorationIntensity", currentExploration },
                        { "preferredAreas", IdentifyPreferredExplorationAreas(interactions) }
                    }
                });
            }
            
            // Predict routine behavior
            if (explorationTrend < -0.05f && currentExploration < 0.3f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "routine_behavior",
                    confidence = 0.75f,
                    timeframe = 10f,
                    parameters = new Dictionary<string, object>
                    {
                        { "routineType", "familiar_path" },
                        { "predictedPath", PredictRoutinePath(interactions) }
                    }
                });
            }
            
            return predictions;
        }
        
        private List<BehaviorPrediction> PredictEmotionalBehavior(PlayerInteraction[] interactions)
        {
            var predictions = new List<BehaviorPrediction>();
            var emotionalModel = predictionModels["emotional"];
            
            // Analyze emotional patterns
            var stressLevels = interactions.Select(i => i.playerStressLevel).ToArray();
            var stressTrend = CalculateTrend(stressLevels);
            var currentStress = stressLevels.LastOrDefault();
            
            // Predict stress increase
            if (stressTrend > 0.1f && currentStress > 0.6f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "stress_increase",
                    confidence = Mathf.Clamp01(stressTrend * 5f + currentStress * 0.5f),
                    timeframe = 3f,
                    parameters = new Dictionary<string, object>
                    {
                        { "stressLevel", currentStress },
                        { "stressTriggers", IdentifyStressTriggers(interactions) }
                    }
                });
            }
            
            // Predict relaxation
            if (stressTrend < -0.1f && currentStress < 0.4f)
            {
                predictions.Add(new BehaviorPrediction
                {
                    predictedBehavior = "player_relaxed",
                    confidence = 0.8f,
                    timeframe = 5f,
                    parameters = new Dictionary<string, object>
                    {
                        { "relaxationLevel", 1f - currentStress }
                    }
                });
            }
            
            return predictions;
        }
        
        #region Helper Methods
        
        private float CalculateVariance(float[] values)
        {
            if (values.Length == 0) return 0f;
            
            float mean = values.Average();
            float variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
            return variance;
        }
        
        private Vector3 CalculateApproachVector(PlayerInteraction[] interactions)
        {
            if (interactions.Length < 2) return Vector3.zero;
            
            Vector3 direction = Vector3.zero;
            for (int i = 1; i < interactions.Length; i++)
            {
                direction += (interactions[i].playerPosition - interactions[i-1].playerPosition).normalized;
            }
            
            return direction / (interactions.Length - 1);
        }
        
        private Vector3 CalculateEscapeDirection(PlayerInteraction[] interactions)
        {
            // Calculate direction away from NPC positions
            Vector3 escapeDirection = Vector3.zero;
            foreach (var interaction in interactions)
            {
                escapeDirection += (interaction.playerPosition - interaction.npcPosition).normalized;
            }
            
            return escapeDirection / interactions.Length;
        }
        
        private float EstimateStationaryDuration(PlayerInteraction[] interactions)
        {
            // Estimate how long player will remain stationary based on past patterns
            var stationaryPeriods = new List<float>();
            
            // Analyze historical stationary periods
            // This is a simplified estimation
            return stationaryPeriods.Count > 0 ? stationaryPeriods.Average() : 5f;
        }
        
        private float CalculateInteractionQualityTrend(PlayerInteraction[] interactions)
        {
            if (interactions.Length < 3) return 0f;
            
            var qualities = interactions.Select(i => i.playerEngagement * i.emotionalResonance).ToArray();
            return CalculateTrend(qualities);
        }
        
        private float CalculateTrend(float[] values)
        {
            if (values.Length < 2) return 0f;
            
            // Simple linear trend calculation
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            int n = values.Length;
            
            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += values[i];
                sumXY += i * values[i];
                sumX2 += i * i;
            }
            
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }
        
        private float CalculateExplorationIndex(PlayerInteraction interaction)
        {
            // Simple exploration index based on movement and engagement
            return (interaction.playerSpeed * 0.3f + interaction.playerEngagement * 0.7f) / 2f;
        }
        
        private float CalculateMovementConfidence(float speed, float variance, List<TemporalPattern> patterns)
        {
            float baseConfidence = Mathf.Clamp01(speed / 5f);
            float varianceModifier = 1f - Mathf.Clamp01(variance / 3f);
            float patternModifier = patterns.Count > 0 ? 1.2f : 1f;
            
            return Mathf.Clamp01(baseConfidence * varianceModifier * patternModifier);
        }
        
        private float CalculateAvoidanceConfidence(float variance, List<TemporalPattern> patterns)
        {
            return Mathf.Clamp01(variance / 3f + (patterns.Count * 0.1f));
        }
        
        private float CalculateInteractionConfidence(float qualityTrend, float engagement, NPCPersonality personality)
        {
            float baseConfidence = Mathf.Clamp01(qualityTrend * 5f + engagement);
            float personalityModifier = personality.socialness * 0.3f + personality.friendliness * 0.7f;
            
            return Mathf.Clamp01(baseConfidence * personalityModifier);
        }
        
        private string DetermineInteractionType(NPCPersonality personality)
        {
            if (personality.friendliness > 0.7f) return "friendly_chat";
            if (personality.curiosity > 0.7f) return "information_exchange";
            if (personality.socialness > 0.7f) return "social_interaction";
            return "casual_interaction";
        }
        
        private List<string> IdentifyPreferredExplorationAreas(PlayerInteraction[] interactions)
        {
            // Analyze where player spends most time exploring
            var areas = new List<string> { "unknown_areas", "high_activity_zones", "scenic_locations" };
            return areas;
        }
        
        private List<Vector3> PredictRoutinePath(PlayerInteraction[] interactions)
        {
            // Predict likely path based on historical movement
            return interactions.Select(i => i.playerPosition).ToList();
        }
        
        private List<string> IdentifyStressTriggers(PlayerInteraction[] interactions)
        {
            var triggers = new List<string>();
            
            var highStressInteractions = interactions.Where(i => i.playerStressLevel > 0.7f);
            foreach (var interaction in highStressInteractions)
            {
                if (interaction.playerSpeed > 4f) triggers.Add("high_speed_movement");
                if (interaction.proximityDistance < 2f) triggers.Add("close_proximity");
                if (interaction.interactionDuration > 10f) triggers.Add("prolonged_interaction");
            }
            
            return triggers.Distinct().ToList();
        }
        
        #endregion
        
        public void Dispose()
        {
            learnedPatterns?.Clear();
            predictionModels?.Clear();
            temporalAnalyzer?.Dispose();
            patternRecognition?.Dispose();
        }
    }
    
    [System.Serializable]
    public class BehaviorPrediction
    {
        public string predictedBehavior;
        public float confidence;
        public float timeframe;
        public Dictionary<string, object> parameters;
    }
    
    [System.Serializable]
    public class PredictionModel
    {
        public string name;
        public int historySize;
        public List<float> historicalData;
        
        public PredictionModel(string modelName, int size)
        {
            name = modelName;
            historySize = size;
            historicalData = new List<float>();
        }
    }
    
    public class TemporalAnalyzer : System.IDisposable
    {
        public List<TemporalPattern> AnalyzeTemporalPatterns(PlayerInteraction[] interactions)
        {
            var patterns = new List<TemporalPattern>();
            
            // Analyze time-based patterns in player behavior
            // This is a simplified implementation
            if (interactions.Length > 5)
            {
                patterns.Add(new TemporalPattern
                {
                    Type = TemporalPatternType.CyclicalOptimization,
                    OptimizationFactor = 1.1f,
                    ConsciousnessImpact = 0.05f
                });
            }
            
            return patterns;
        }
        
        public void Dispose()
        {
            // Cleanup temporal analysis resources
        }
    }
    
    public class PatternRecognitionSystem : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup pattern recognition resources
        }
    }
}