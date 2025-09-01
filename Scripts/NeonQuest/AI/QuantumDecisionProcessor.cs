using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;

namespace NeonQuest.AI
{
    /// <summary>
    /// Quantum decision-making processor for NPCs
    /// Uses quantum superposition principles for complex AI decision-making
    /// </summary>
    public class QuantumDecisionProcessor : System.IDisposable
    {
        private List<QuantumState> activeStates;
        private QuantumSuperposition currentSuperposition;
        private float coherenceTime = 5.0f;
        private float decoherenceRate = 0.1f;
        
        public QuantumDecisionProcessor()
        {
            InitializeQuantumSystem();
        }
        
        private void InitializeQuantumSystem()
        {
            activeStates = new List<QuantumState>();
            currentSuperposition = new QuantumSuperposition();
        }
        
        public List<QuantumDecisionState> CreateDecisionSuperposition(NPCState[] possibleStates, Dictionary<string, float> behaviorWeights)
        {
            var quantumStates = new List<QuantumDecisionState>();
            
            foreach (var state in possibleStates)
            {
                var quantumState = new QuantumDecisionState
                {
                    state = state,
                    amplitude = CalculateStateAmplitude(state, behaviorWeights),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    probability = 0f, // Will be calculated during collapse
                    entangled = false
                };
                
                quantumStates.Add(quantumState);
            }
            
            // Normalize amplitudes
            NormalizeAmplitudes(quantumStates);
            
            return quantumStates;
        }
        
        private float CalculateStateAmplitude(NPCState state, Dictionary<string, float> behaviorWeights)
        {
            string stateKey = state.ToString().ToLower();
            float baseAmplitude = behaviorWeights.GetValueOrDefault(stateKey, 0.1f);
            
            // Add quantum uncertainty
            float uncertainty = Random.Range(-0.1f, 0.1f);
            return Mathf.Clamp01(baseAmplitude + uncertainty);
        }
        
        private void NormalizeAmplitudes(List<QuantumDecisionState> states)
        {
            float totalAmplitude = states.Sum(s => s.amplitude * s.amplitude);
            if (totalAmplitude > 0f)
            {
                float normalizationFactor = Mathf.Sqrt(totalAmplitude);
                foreach (var state in states)
                {
                    state.amplitude /= normalizationFactor;
                }
            }
        }
        
        public QuantumDecision CollapseToOptimalDecision(List<QuantumDecisionState> quantumStates, NPCPersonality personality, EmotionalState emotionalState)
        {
            if (quantumStates.Count == 0) return null;
            
            // Calculate probabilities from amplitudes
            foreach (var state in quantumStates)
            {
                state.probability = state.amplitude * state.amplitude;
            }
            
            // Apply personality and emotional modifiers
            ApplyPersonalityModifiers(quantumStates, personality);
            ApplyEmotionalModifiers(quantumStates, emotionalState);
            
            // Perform quantum measurement (collapse)
            var collapsedState = PerformQuantumMeasurement(quantumStates);
            
            return new QuantumDecision
            {
                recommendedState = collapsedState.state,
                confidence = collapsedState.probability,
                quantumCoherence = CalculateCoherence(quantumStates),
                measurementTime = Time.time
            };
        }
        
        private void ApplyPersonalityModifiers(List<QuantumDecisionState> states, NPCPersonality personality)
        {
            foreach (var state in states)
            {
                float modifier = 1.0f;
                
                switch (state.state)
                {
                    case NPCState.Socializing:
                        modifier = personality.socialness * 1.5f;
                        break;
                    case NPCState.Avoiding:
                        modifier = (1f - personality.confidence) * 1.3f;
                        break;
                    case NPCState.Investigating:
                        modifier = personality.curiosity * 1.4f;
                        break;
                    case NPCState.Interacting:
                        modifier = personality.friendliness * 1.2f;
                        break;
                    case NPCState.Guarding:
                        modifier = personality.aggression * 1.3f;
                        break;
                }
                
                state.probability *= modifier;
            }
            
            // Renormalize probabilities
            float totalProbability = states.Sum(s => s.probability);
            if (totalProbability > 0f)
            {
                foreach (var state in states)
                {
                    state.probability /= totalProbability;
                }
            }
        }
        
        private void ApplyEmotionalModifiers(List<QuantumDecisionState> states, EmotionalState emotionalState)
        {
            foreach (var state in states)
            {
                float emotionalModifier = GetEmotionalStateModifier(state.state, emotionalState);
                state.probability *= emotionalModifier;
            }
            
            // Renormalize again
            float totalProbability = states.Sum(s => s.probability);
            if (totalProbability > 0f)
            {
                foreach (var state in states)
                {
                    state.probability /= totalProbability;
                }
            }
        }
        
        private float GetEmotionalStateModifier(NPCState npcState, EmotionalState emotionalState)
        {
            var modifierMatrix = new Dictionary<(NPCState, EmotionalState), float>
            {
                { (NPCState.Socializing, EmotionalState.Happy), 1.5f },
                { (NPCState.Socializing, EmotionalState.Excited), 1.4f },
                { (NPCState.Avoiding, EmotionalState.Fearful), 1.6f },
                { (NPCState.Avoiding, EmotionalState.Anxious), 1.3f },
                { (NPCState.Investigating, EmotionalState.Curious), 1.5f },
                { (NPCState.Investigating, EmotionalState.Surprised), 1.2f },
                { (NPCState.Interacting, EmotionalState.Empathetic), 1.4f },
                { (NPCState.Interacting, EmotionalState.Confident), 1.2f },
                { (NPCState.Guarding, EmotionalState.Angry), 1.5f },
                { (NPCState.Patrolling, EmotionalState.Neutral), 1.1f }
            };
            
            return modifierMatrix.GetValueOrDefault((npcState, emotionalState), 1.0f);
        }
        
        private QuantumDecisionState PerformQuantumMeasurement(List<QuantumDecisionState> states)
        {
            // Weighted random selection based on quantum probabilities
            float randomValue = Random.Range(0f, 1f);
            float cumulativeProbability = 0f;
            
            foreach (var state in states)
            {
                cumulativeProbability += state.probability;
                if (randomValue <= cumulativeProbability)
                {
                    return state;
                }
            }
            
            // Fallback to highest probability state
            return states.OrderByDescending(s => s.probability).First();
        }
        
        private float CalculateCoherence(List<QuantumDecisionState> states)
        {
            // Calculate quantum coherence based on state distribution
            float entropy = 0f;
            foreach (var state in states)
            {
                if (state.probability > 0f)
                {
                    entropy -= state.probability * Mathf.Log(state.probability);
                }
            }
            
            // Normalize entropy to coherence (0 = fully coherent, 1 = fully decoherent)
            float maxEntropy = Mathf.Log(states.Count);
            return maxEntropy > 0f ? 1f - (entropy / maxEntropy) : 1f;
        }
        
        public void UpdateQuantumCoherence(float deltaTime)
        {
            // Simulate quantum decoherence over time
            coherenceTime -= deltaTime * decoherenceRate;
            
            if (coherenceTime <= 0f)
            {
                // Reset coherence
                coherenceTime = Random.Range(3f, 8f);
                
                // Clear quantum states to force new superposition
                activeStates.Clear();
            }
        }
        
        public bool IsQuantumCoherent()
        {
            return coherenceTime > 1f;
        }
        
        public void Dispose()
        {
            activeStates?.Clear();
            currentSuperposition = null;
        }
    }
    
    [System.Serializable]
    public class QuantumDecisionState
    {
        public NPCState state;
        public float amplitude;
        public float phase;
        public float probability;
        public bool entangled;
    }
    
    [System.Serializable]
    public class QuantumDecision
    {
        public NPCState recommendedState;
        public float confidence;
        public float quantumCoherence;
        public float measurementTime;
    }
    
    [System.Serializable]
    public class QuantumSuperposition
    {
        public List<QuantumDecisionState> states;
        public float coherenceLevel;
        public float lastMeasurement;
        
        public QuantumSuperposition()
        {
            states = new List<QuantumDecisionState>();
            coherenceLevel = 1.0f;
            lastMeasurement = 0f;
        }
    }
    
    [System.Serializable]
    public class QuantumState
    {
        public int id;
        public Vector3 position;
        public float energy;
        public float waveFunction;
        public bool measured;
    }
}