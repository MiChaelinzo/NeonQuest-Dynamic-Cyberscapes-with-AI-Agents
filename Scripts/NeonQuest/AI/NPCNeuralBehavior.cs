using UnityEngine;
using System.Collections.Generic;
using NeonQuest.Core;

namespace NeonQuest.AI
{
    /// <summary>
    /// Alias class for NeuralNPCBehavior to support swarm intelligence references
    /// This ensures compatibility with swarm intelligence system
    /// </summary>
    public class NPCNeuralBehavior : NeuralNPCBehavior
    {
        // This class serves as an alias to maintain compatibility
        // All functionality is inherited from NeuralNPCBehavior
    }
    
    [System.Serializable]
    public class NPCStatus
    {
        public NPCState currentState;
        public NPCPersonality personality;
        public EmotionalState emotionalState;
        public float emotionalIntensity;
        public int swarmSize;
        public int interactionCount;
        public bool learningEnabled;
        public bool emotionalIntelligenceEnabled;
        public bool swarmIntelligenceEnabled;
        public bool quantumDecisionMakingEnabled;
    }
    
    [System.Serializable]
    public class NPCTrainingData
    {
        public float[] inputs;
        public float[] expectedOutputs;
    }
    
    [System.Serializable]
    public class BehaviorMemorySystem : System.IDisposable
    {
        private Queue<PlayerInteraction> storedInteractions;
        private int maxMemorySize;
        
        public BehaviorMemorySystem(int maxSize)
        {
            maxMemorySize = maxSize;
            storedInteractions = new Queue<PlayerInteraction>();
        }
        
        public void StoreInteraction(PlayerInteraction interaction)
        {
            storedInteractions.Enqueue(interaction);
            
            while (storedInteractions.Count > maxMemorySize)
            {
                storedInteractions.Dequeue();
            }
        }
        
        public PlayerInteraction[] GetStoredInteractions()
        {
            return storedInteractions.ToArray();
        }
        
        public void Dispose()
        {
            storedInteractions?.Clear();
        }
    }
    
    [System.Serializable]
    public class PlayerInteractionAnalyzer : System.IDisposable
    {
        public List<BehaviorPattern> AnalyzePatterns(PlayerInteraction[] interactions, NPCPersonality personality)
        {
            var patterns = new List<BehaviorPattern>();
            
            if (interactions.Length < 3) return patterns;
            
            // Analyze approach patterns
            var approachPattern = AnalyzeApproachPattern(interactions);
            if (approachPattern != null) patterns.Add(approachPattern);
            
            // Analyze interaction duration patterns
            var durationPattern = AnalyzeDurationPattern(interactions);
            if (durationPattern != null) patterns.Add(durationPattern);
            
            // Analyze emotional resonance patterns
            var emotionalPattern = AnalyzeEmotionalPattern(interactions, personality);
            if (emotionalPattern != null) patterns.Add(emotionalPattern);
            
            return patterns;
        }
        
        private BehaviorPattern AnalyzeApproachPattern(PlayerInteraction[] interactions)
        {
            var approachInteractions = System.Array.FindAll(interactions, i => i.proximityDistance < 3f && i.playerSpeed > 1f);
            
            if (approachInteractions.Length >= 2)
            {
                return new BehaviorPattern
                {
                    patternName = "player_approach",
                    inputFeatures = CalculateApproachFeatures(approachInteractions),
                    recommendedState = NPCState.Interacting,
                    confidence = CalculatePatternConfidence(approachInteractions.Length, interactions.Length),
                    suggestedPosition = CalculateOptimalPosition(approachInteractions),
                    suggestedAnimation = "friendly_wave"
                };
            }
            
            return null;
        }
        
        private BehaviorPattern AnalyzeDurationPattern(PlayerInteraction[] interactions)
        {
            var longInteractions = System.Array.FindAll(interactions, i => i.interactionDuration > 5f);
            
            if (longInteractions.Length >= 1)
            {
                return new BehaviorPattern
                {
                    patternName = "extended_interaction",
                    inputFeatures = CalculateDurationFeatures(longInteractions),
                    recommendedState = NPCState.Socializing,
                    confidence = CalculatePatternConfidence(longInteractions.Length, interactions.Length),
                    suggestedPosition = Vector3.zero,
                    suggestedAnimation = "engaged_conversation"
                };
            }
            
            return null;
        }
        
        private BehaviorPattern AnalyzeEmotionalPattern(PlayerInteraction[] interactions, NPCPersonality personality)
        {
            var highEngagementInteractions = System.Array.FindAll(interactions, i => i.playerEngagement > 0.7f);
            
            if (highEngagementInteractions.Length >= 2)
            {
                NPCState recommendedState = personality.socialness > 0.6f ? NPCState.Socializing : NPCState.Interacting;
                
                return new BehaviorPattern
                {
                    patternName = "high_engagement",
                    inputFeatures = CalculateEmotionalFeatures(highEngagementInteractions),
                    recommendedState = recommendedState,
                    confidence = CalculatePatternConfidence(highEngagementInteractions.Length, interactions.Length),
                    suggestedPosition = Vector3.zero,
                    suggestedAnimation = "enthusiastic_gesture"
                };
            }
            
            return null;
        }
        
        private float[] CalculateApproachFeatures(PlayerInteraction[] interactions)
        {
            if (interactions.Length == 0) return new float[5];
            
            return new float[]
            {
                interactions.Average(i => i.proximityDistance),
                interactions.Average(i => i.playerSpeed),
                interactions.Average(i => i.interactionDuration),
                interactions.Average(i => i.playerEngagement),
                interactions.Length
            };
        }
        
        private float[] CalculateDurationFeatures(PlayerInteraction[] interactions)
        {
            if (interactions.Length == 0) return new float[4];
            
            return new float[]
            {
                interactions.Average(i => i.interactionDuration),
                interactions.Max(i => i.interactionDuration),
                interactions.Average(i => i.playerEngagement),
                interactions.Length
            };
        }
        
        private float[] CalculateEmotionalFeatures(PlayerInteraction[] interactions)
        {
            if (interactions.Length == 0) return new float[4];
            
            return new float[]
            {
                interactions.Average(i => i.playerEngagement),
                interactions.Average(i => i.emotionalResonance),
                interactions.Average(i => i.playerStressLevel),
                interactions.Length
            };
        }
        
        private float CalculatePatternConfidence(int patternCount, int totalCount)
        {
            return Mathf.Clamp01((float)patternCount / totalCount * 2f);
        }
        
        private Vector3 CalculateOptimalPosition(PlayerInteraction[] interactions)
        {
            if (interactions.Length == 0) return Vector3.zero;
            
            Vector3 averagePosition = Vector3.zero;
            foreach (var interaction in interactions)
            {
                averagePosition += interaction.playerPosition;
            }
            
            return averagePosition / interactions.Length;
        }
        
        public void Dispose()
        {
            // Cleanup analyzer resources
        }
    }
    
    public class NPCNeuralNetwork : System.IDisposable
    {
        private int neuronsPerLayer;
        private int layerCount;
        private float learningRate;
        private float[,,] weights;
        private float[,] biases;
        private float[,] activations;
        
        public NPCNeuralNetwork(int neuronsPerLayer, int layerCount, float learningRate)
        {
            this.neuronsPerLayer = neuronsPerLayer;
            this.layerCount = layerCount;
            this.learningRate = learningRate;
            
            InitializeNetwork();
        }
        
        private void InitializeNetwork()
        {
            weights = new float[layerCount - 1, neuronsPerLayer, neuronsPerLayer];
            biases = new float[layerCount, neuronsPerLayer];
            activations = new float[layerCount, neuronsPerLayer];
            
            // Initialize with random weights using Xavier initialization
            System.Random rand = new System.Random();
            for (int layer = 0; layer < layerCount - 1; layer++)
            {
                float weightRange = Mathf.Sqrt(6f / (neuronsPerLayer + neuronsPerLayer));
                
                for (int i = 0; i < neuronsPerLayer; i++)
                {
                    biases[layer, i] = (float)(rand.NextDouble() * 2 - 1) * 0.1f;
                    
                    for (int j = 0; j < neuronsPerLayer; j++)
                    {
                        weights[layer, i, j] = (float)(rand.NextDouble() * 2 - 1) * weightRange;
                    }
                }
            }
        }
        
        public BehaviorPattern Predict(float[] inputs)
        {
            if (inputs.Length != neuronsPerLayer) return null;
            
            // Forward propagation
            for (int i = 0; i < neuronsPerLayer; i++)
            {
                activations[0, i] = inputs[i];
            }
            
            for (int layer = 1; layer < layerCount; layer++)
            {
                for (int neuron = 0; neuron < neuronsPerLayer; neuron++)
                {
                    float sum = biases[layer, neuron];
                    
                    for (int prevNeuron = 0; prevNeuron < neuronsPerLayer; prevNeuron++)
                    {
                        sum += activations[layer - 1, prevNeuron] * weights[layer - 1, prevNeuron, neuron];
                    }
                    
                    activations[layer, neuron] = Sigmoid(sum);
                }
            }
            
            // Convert output to behavior pattern
            return InterpretOutput();
        }
        
        private BehaviorPattern InterpretOutput()
        {
            int outputLayer = layerCount - 1;
            
            // Find the neuron with highest activation
            int maxNeuron = 0;
            float maxActivation = activations[outputLayer, 0];
            
            for (int i = 1; i < neuronsPerLayer; i++)
            {
                if (activations[outputLayer, i] > maxActivation)
                {
                    maxActivation = activations[outputLayer, i];
                    maxNeuron = i;
                }
            }
            
            // Map neuron to NPC state
            NPCState recommendedState = (NPCState)(maxNeuron % System.Enum.GetValues(typeof(NPCState)).Length);
            
            return new BehaviorPattern
            {
                patternName = "neural_prediction",
                recommendedState = recommendedState,
                confidence = maxActivation,
                inputFeatures = GetCurrentInputs(),
                suggestedPosition = Vector3.zero,
                suggestedAnimation = "neural_response"
            };
        }
        
        private float[] GetCurrentInputs()
        {
            float[] inputs = new float[neuronsPerLayer];
            for (int i = 0; i < neuronsPerLayer; i++)
            {
                inputs[i] = activations[0, i];
            }
            return inputs;
        }
        
        public void Train(NPCTrainingData[] trainingData)
        {
            foreach (var data in trainingData)
            {
                TrainSample(data.inputs, data.expectedOutputs);
            }
        }
        
        private void TrainSample(float[] inputs, float[] expectedOutputs)
        {
            // Forward pass
            Predict(inputs);
            
            // Backward pass (simplified backpropagation)
            float[] outputErrors = new float[neuronsPerLayer];
            int outputLayer = layerCount - 1;
            
            // Calculate output layer errors
            for (int i = 0; i < neuronsPerLayer; i++)
            {
                float output = activations[outputLayer, i];
                float expected = i < expectedOutputs.Length ? expectedOutputs[i] : 0f;
                outputErrors[i] = (expected - output) * SigmoidDerivative(output);
            }
            
            // Update weights (simplified)
            for (int layer = layerCount - 2; layer >= 0; layer--)
            {
                for (int i = 0; i < neuronsPerLayer; i++)
                {
                    for (int j = 0; j < neuronsPerLayer; j++)
                    {
                        float delta = learningRate * outputErrors[j] * activations[layer, i];
                        weights[layer, i, j] += delta;
                    }
                }
            }
        }
        
        private float Sigmoid(float x)
        {
            return 1f / (1f + Mathf.Exp(-x));
        }
        
        private float SigmoidDerivative(float x)
        {
            return x * (1f - x);
        }
        
        public void Dispose()
        {
            weights = null;
            biases = null;
            activations = null;
        }
    }
    
    public class EntanglementNetwork
    {
        private List<EntanglementPair> entanglements;
        private int groupCount = 0;
        
        public EntanglementNetwork()
        {
            entanglements = new List<EntanglementPair>();
        }
        
        public void AddEntanglement(EntanglementPair entanglement)
        {
            entanglements.Add(entanglement);
            groupCount++;
        }
        
        public List<EntanglementPair> GetAllEntanglements()
        {
            return new List<EntanglementPair>(entanglements);
        }
        
        public int GetGroupCount()
        {
            return groupCount;
        }
    }
}