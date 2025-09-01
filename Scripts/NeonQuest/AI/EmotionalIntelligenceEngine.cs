using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.AI
{
    /// <summary>
    /// Advanced emotional intelligence engine for NPCs
    /// Analyzes player emotions and generates appropriate NPC emotional responses
    /// </summary>
    public class EmotionalIntelligenceEngine : System.IDisposable
    {
        private Dictionary<EmotionalState, float> emotionalWeights;
        private List<EmotionalTrigger> activeTriggers;
        private EmotionalMemory emotionalMemory;
        private float empathyLevel = 0.7f;
        
        public EmotionalIntelligenceEngine()
        {
            InitializeEmotionalSystem();
        }
        
        private void InitializeEmotionalSystem()
        {
            emotionalWeights = new Dictionary<EmotionalState, float>
            {
                { EmotionalState.Happy, 0.8f },
                { EmotionalState.Sad, 0.6f },
                { EmotionalState.Angry, 0.9f },
                { EmotionalState.Fearful, 0.7f },
                { EmotionalState.Surprised, 0.5f },
                { EmotionalState.Excited, 0.8f },
                { EmotionalState.Anxious, 0.6f },
                { EmotionalState.Confident, 0.7f },
                { EmotionalState.Empathetic, 0.9f },
                { EmotionalState.Nostalgic, 0.4f }
            };
            
            activeTriggers = new List<EmotionalTrigger>();
            emotionalMemory = new EmotionalMemory();
        }
        
        public EmotionalState AnalyzePlayerEmotion(PlayerMovementTracker playerTracker)
        {
            if (playerTracker == null) return EmotionalState.Neutral;
            
            // Analyze movement patterns for emotional cues
            float speed = playerTracker.CurrentSpeed;
            float directionChanges = playerTracker.DirectionChangeFrequency;
            float explorationIndex = playerTracker.ExplorationIndex;
            
            // High speed + erratic movement = anxious/excited
            if (speed > 5f && directionChanges > 0.7f)
                return Random.value > 0.5f ? EmotionalState.Anxious : EmotionalState.Excited;
            
            // Slow, steady movement = calm/confident
            if (speed < 2f && directionChanges < 0.3f)
                return EmotionalState.Confident;
            
            // High exploration = curious/happy
            if (explorationIndex > 0.7f)
                return Random.value > 0.5f ? EmotionalState.Happy : EmotionalState.Surprised;
            
            // Low exploration = sad/bored
            if (explorationIndex < 0.3f)
                return EmotionalState.Sad;
            
            return EmotionalState.Neutral;
        }
        
        public float CalculateEmotionalResonance(EmotionalState npcEmotion, EmotionalState playerEmotion, NPCPersonality personality)
        {
            // Calculate how well NPC emotion resonates with player emotion
            float baseResonance = GetEmotionalCompatibility(npcEmotion, playerEmotion);
            
            // Modify based on personality
            float personalityModifier = 1f;
            switch (npcEmotion)
            {
                case EmotionalState.Empathetic:
                    personalityModifier = personality.socialness * 1.5f;
                    break;
                case EmotionalState.Happy:
                    personalityModifier = personality.friendliness * 1.3f;
                    break;
                case EmotionalState.Fearful:
                    personalityModifier = (1f - personality.confidence) * 1.2f;
                    break;
                case EmotionalState.Angry:
                    personalityModifier = personality.aggression * 1.4f;
                    break;
            }
            
            return Mathf.Clamp01(baseResonance * personalityModifier * empathyLevel);
        }
        
        private float GetEmotionalCompatibility(EmotionalState emotion1, EmotionalState emotion2)
        {
            // Define emotional compatibility matrix
            var compatibilityMatrix = new Dictionary<(EmotionalState, EmotionalState), float>
            {
                { (EmotionalState.Happy, EmotionalState.Happy), 1.0f },
                { (EmotionalState.Happy, EmotionalState.Excited), 0.9f },
                { (EmotionalState.Happy, EmotionalState.Confident), 0.8f },
                { (EmotionalState.Sad, EmotionalState.Empathetic), 0.9f },
                { (EmotionalState.Fearful, EmotionalState.Empathetic), 0.8f },
                { (EmotionalState.Angry, EmotionalState.Angry), 0.3f }, // Anger + Anger = conflict
                { (EmotionalState.Anxious, EmotionalState.Confident), 0.7f },
                { (EmotionalState.Surprised, EmotionalState.Excited), 0.8f }
            };
            
            if (compatibilityMatrix.TryGetValue((emotion1, emotion2), out float compatibility))
                return compatibility;
            
            if (compatibilityMatrix.TryGetValue((emotion2, emotion1), out compatibility))
                return compatibility;
            
            // Default neutral compatibility
            return 0.5f;
        }
        
        public List<EmotionalTrigger> DetectEmotionalTriggers(Vector3 npcPosition, PlayerInteraction[] interactions)
        {
            var triggers = new List<EmotionalTrigger>();
            
            if (interactions.Length == 0) return triggers;
            
            var recentInteractions = interactions.TakeLast(5).ToArray();
            
            foreach (var interaction in recentInteractions)
            {
                // Detect approach patterns
                if (interaction.proximityDistance < 3f && interaction.playerSpeed > 2f)
                {
                    triggers.Add(new EmotionalTrigger
                    {
                        type = EmotionalTriggerType.PlayerApproach,
                        intensity = 1f - (interaction.proximityDistance / 3f),
                        location = interaction.playerPosition,
                        timestamp = interaction.timestamp
                    });
                }
                
                // Detect flee patterns
                if (interaction.playerSpeed > 4f && interaction.proximityDistance > 5f)
                {
                    triggers.Add(new EmotionalTrigger
                    {
                        type = EmotionalTriggerType.PlayerFlee,
                        intensity = Mathf.Clamp01(interaction.playerSpeed / 6f),
                        location = interaction.playerPosition,
                        timestamp = interaction.timestamp
                    });
                }
                
                // Detect long interactions
                if (interaction.interactionDuration > 5f)
                {
                    triggers.Add(new EmotionalTrigger
                    {
                        type = EmotionalTriggerType.LongInteraction,
                        intensity = Mathf.Clamp01(interaction.interactionDuration / 10f),
                        location = interaction.playerPosition,
                        timestamp = interaction.timestamp
                    });
                }
            }
            
            return triggers;
        }
        
        public EmotionalState DetermineEmotionalState(float emotionalIntensity, NPCPersonality personality, EmotionalState currentState)
        {
            // Use personality to influence emotional state transitions
            var stateWeights = new Dictionary<EmotionalState, float>();
            
            foreach (var state in System.Enum.GetValues(typeof(EmotionalState)).Cast<EmotionalState>())
            {
                float weight = CalculateStateWeight(state, emotionalIntensity, personality, currentState);
                stateWeights[state] = weight;
            }
            
            // Select state based on weighted probability
            return SelectWeightedRandomState(stateWeights);
        }
        
        private float CalculateStateWeight(EmotionalState state, float intensity, NPCPersonality personality, EmotionalState currentState)
        {
            float baseWeight = emotionalWeights.GetValueOrDefault(state, 0.5f);
            
            // Personality influences
            switch (state)
            {
                case EmotionalState.Happy:
                    baseWeight *= personality.friendliness * 1.5f;
                    break;
                case EmotionalState.Fearful:
                    baseWeight *= (1f - personality.confidence) * 1.3f;
                    break;
                case EmotionalState.Angry:
                    baseWeight *= personality.aggression * 1.4f;
                    break;
                case EmotionalState.Empathetic:
                    baseWeight *= personality.socialness * 1.6f;
                    break;
                case EmotionalState.Confident:
                    baseWeight *= personality.confidence * 1.2f;
                    break;
            }
            
            // Intensity influence
            baseWeight *= (0.5f + intensity * 0.5f);
            
            // Current state inertia (tendency to stay in current state)
            if (state == currentState)
                baseWeight *= 1.2f;
            
            return Mathf.Clamp01(baseWeight);
        }
        
        private EmotionalState SelectWeightedRandomState(Dictionary<EmotionalState, float> stateWeights)
        {
            float totalWeight = stateWeights.Values.Sum();
            if (totalWeight <= 0f) return EmotionalState.Neutral;
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var kvp in stateWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                    return kvp.Key;
            }
            
            return EmotionalState.Neutral;
        }
        
        public void Dispose()
        {
            emotionalWeights?.Clear();
            activeTriggers?.Clear();
            emotionalMemory?.Dispose();
        }
    }
    
    [System.Serializable]
    public class EmotionalTrigger
    {
        public EmotionalTriggerType type;
        public float intensity;
        public Vector3 location;
        public float timestamp;
    }
    
    public enum EmotionalTriggerType
    {
        PlayerApproach,
        PlayerFlee,
        LongInteraction,
        SuddenMovement,
        StationaryBehavior,
        GroupInteraction,
        EnvironmentalChange
    }
    
    public class EmotionalMemory : System.IDisposable
    {
        private Dictionary<string, EmotionalExperience> experiences;
        private const int MAX_MEMORIES = 100;
        
        public EmotionalMemory()
        {
            experiences = new Dictionary<string, EmotionalExperience>();
        }
        
        public void StoreExperience(EmotionalState emotion, float intensity, Vector3 location)
        {
            string key = $"{emotion}_{location}_{Time.time:F0}";
            experiences[key] = new EmotionalExperience
            {
                emotion = emotion,
                intensity = intensity,
                location = location,
                timestamp = Time.time
            };
            
            // Maintain memory limit
            if (experiences.Count > MAX_MEMORIES)
            {
                var oldestKey = experiences.Keys.OrderBy(k => experiences[k].timestamp).First();
                experiences.Remove(oldestKey);
            }
        }
        
        public List<EmotionalExperience> GetRecentExperiences(int count = 10)
        {
            return experiences.Values
                .OrderByDescending(e => e.timestamp)
                .Take(count)
                .ToList();
        }
        
        public void Dispose()
        {
            experiences?.Clear();
        }
    }
    
    [System.Serializable]
    public class EmotionalExperience
    {
        public EmotionalState emotion;
        public float intensity;
        public Vector3 location;
        public float timestamp;
    }
}