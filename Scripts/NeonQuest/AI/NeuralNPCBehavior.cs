using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.AI
{
    /// <summary>
    /// Neural network-based NPC behavior system that learns from player patterns
    /// NPCs adapt their behavior, patrol routes, and interactions based on player preferences
    /// </summary>
    public class NeuralNPCBehavior : NeonQuestComponent
    {
        [Header("Neural Network Configuration")]
        [SerializeField] private int neuralNetworkLayers = 3;
        [SerializeField] private int neuronsPerLayer = 16;
        [SerializeField] private float learningRate = 0.01f;
        [SerializeField] private float adaptationSpeed = 0.5f;
        [SerializeField] private bool enableRealTimeLearning = true;

        [Header("NPC Behavior Settings")]
        [SerializeField] private float interactionRadius = 5f;
        [SerializeField] private float patrolRadius = 20f;
        [SerializeField] private float reactionTime = 0.3f;
        [SerializeField] private int maxMemorySize = 200;

        [Header("NPC Components")]
        [SerializeField] private NavMeshAgent navAgent;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private AudioSource npcAudioSource;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private GameObject[] interactionObjects;

        // Neural Network Components
        private NPCNeuralNetwork neuralNetwork;
        private BehaviorMemorySystem memorySystem;
        private PlayerInteractionAnalyzer interactionAnalyzer;
        
        // NPC State Management
        private NPCState currentState;
        private NPCPersonality personality;
        private Queue<PlayerInteraction> interactionHistory;
        private Dictionary<string, float> behaviorWeights;
        
        // Learning and Adaptation
        private Coroutine learningCoroutine;
        private float lastLearningUpdate;
        private Vector3 lastPlayerPosition;
        private float playerProximityTime;

        public enum NPCState
        {
            Idle,
            Patrolling,
            Investigating,
            Interacting,
            Following,
            Avoiding,
            Socializing,
            Working,
            Guarding
        }

        public enum NPCPersonalityType
        {
            Friendly,
            Suspicious,
            Aggressive,
            Curious,
            Indifferent,
            Helpful,
            Mysterious,
            Playful
        }

        [System.Serializable]
        public class NPCPersonality
        {
            public NPCPersonalityType type;
            public float friendliness;
            public float curiosity;
            public float aggression;
            public float intelligence;
            public float adaptability;
            public float socialness;
            public float loyalty;
            public float independence;
        }

        [System.Serializable]
        public class PlayerInteraction
        {
            public Vector3 playerPosition;
            public Vector3 npcPosition;
            public float interactionDuration;
            public string interactionType;
            public float playerSpeed;
            public float proximityDistance;
            public bool playerInitiated;
            public float timestamp;
            public float playerStressLevel;
            public float playerEngagement;
        }

        [System.Serializable]
        public class BehaviorPattern
        {
            public string patternName;
            public float[] inputFeatures;
            public NPCState recommendedState;
            public float confidence;
            public Vector3 suggestedPosition;
            public string suggestedAnimation;
        }

        protected override void OnInitialize()
        {
            LogDebug("Initializing Neural NPC Behavior System");

            try
            {
                // Initialize neural network
                neuralNetwork = new NPCNeuralNetwork(neuronsPerLayer, neuralNetworkLayers, learningRate);
                memorySystem = new BehaviorMemorySystem(maxMemorySize);
                interactionAnalyzer = new PlayerInteractionAnalyzer();

                // Initialize NPC components
                InitializeNPCComponents();

                // Generate random personality
                GeneratePersonality();

                // Initialize behavior tracking
                interactionHistory = new Queue<PlayerInteraction>();
                behaviorWeights = new Dictionary<string, float>();

                // Set initial state
                currentState = NPCState.Idle;

                // Initialize behavior weights
                InitializeBehaviorWeights();

                // Start learning coroutine
                if (enableRealTimeLearning)
                {
                    learningCoroutine = StartCoroutine(LearningUpdateLoop());
                }

                LogDebug($"Neural NPC initialized with {personality.type} personality");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize Neural NPC Behavior: {ex.Message}");
                OnInitializationFailed(ex);
                throw;
            }
        }

        private void InitializeNPCComponents()
        {
            // Initialize NavMesh agent if not assigned
            if (navAgent == null)
            {
                navAgent = GetComponent<NavMeshAgent>();
                if (navAgent == null)
                {
                    navAgent = gameObject.AddComponent<NavMeshAgent>();
                }
            }

            // Initialize animator if not assigned
            if (npcAnimator == null)
            {
                npcAnimator = GetComponent<Animator>();
            }

            // Initialize audio source if not assigned
            if (npcAudioSource == null)
            {
                npcAudioSource = GetComponent<AudioSource>();
                if (npcAudioSource == null)
                {
                    npcAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Setup NavMesh agent properties
            navAgent.speed = Random.Range(2f, 4f);
            navAgent.acceleration = Random.Range(6f, 10f);
            navAgent.angularSpeed = Random.Range(180f, 360f);
            navAgent.stoppingDistance = 1f;
        }

        private void GeneratePersonality()
        {
            var personalityTypes = System.Enum.GetValues(typeof(NPCPersonalityType));
            var randomType = (NPCPersonalityType)personalityTypes.GetValue(Random.Range(0, personalityTypes.Length));

            personality = new NPCPersonality
            {
                type = randomType,
                friendliness = Random.Range(0.2f, 1f),
                curiosity = Random.Range(0.1f, 0.9f),
                aggression = Random.Range(0f, 0.6f),
                intelligence = Random.Range(0.3f, 1f),
                adaptability = Random.Range(0.4f, 1f),
                socialness = Random.Range(0.2f, 0.8f),
                loyalty = Random.Range(0.3f, 0.9f),
                independence = Random.Range(0.2f, 0.8f)
            };

            // Adjust personality based on type
            AdjustPersonalityByType();
        }

        private void AdjustPersonalityByType()
        {
            switch (personality.type)
            {
                case NPCPersonalityType.Friendly:
                    personality.friendliness = Mathf.Max(0.7f, personality.friendliness);
                    personality.socialness = Mathf.Max(0.6f, personality.socialness);
                    personality.aggression = Mathf.Min(0.3f, personality.aggression);
                    break;
                case NPCPersonalityType.Suspicious:
                    personality.curiosity = Mathf.Max(0.6f, personality.curiosity);
                    personality.friendliness = Mathf.Min(0.4f, personality.friendliness);
                    personality.intelligence = Mathf.Max(0.5f, personality.intelligence);
                    break;
                case NPCPersonalityType.Aggressive:
                    personality.aggression = Mathf.Max(0.6f, personality.aggression);
                    personality.friendliness = Mathf.Min(0.3f, personality.friendliness);
                    personality.independence = Mathf.Max(0.6f, personality.independence);
                    break;
                case NPCPersonalityType.Curious:
                    personality.curiosity = Mathf.Max(0.8f, personality.curiosity);
                    personality.intelligence = Mathf.Max(0.6f, personality.intelligence);
                    personality.adaptability = Mathf.Max(0.7f, personality.adaptability);
                    break;
            }
        }

        private void InitializeBehaviorWeights()
        {
            behaviorWeights["patrol"] = 0.3f;
            behaviorWeights["investigate"] = 0.2f;
            behaviorWeights["interact"] = 0.4f;
            behaviorWeights["avoid"] = 0.1f;
            behaviorWeights["follow"] = 0.2f;
            behaviorWeights["socialize"] = 0.3f;
            behaviorWeights["work"] = 0.4f;
            behaviorWeights["guard"] = 0.2f;
        }

        private IEnumerator LearningUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(0.2f); // Update 5 times per second

                try
                {
                    // Track player interactions
                    TrackPlayerInteraction();

                    // Analyze behavior patterns
                    AnalyzeBehaviorPatterns();

                    // Update neural network
                    UpdateNeuralNetwork();

                    // Adapt behavior based on learning
                    AdaptBehavior();

                    // Update NPC state and actions
                    UpdateNPCBehavior();

                    lastLearningUpdate = Time.time;
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in NPC learning update: {ex.Message}");
                }
            }
        }

        private void TrackPlayerInteraction()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return;

            Vector3 playerPosition = playerTracker.CurrentPosition;
            float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);

            // Track proximity time
            if (distanceToPlayer <= interactionRadius)
            {
                playerProximityTime += Time.deltaTime;
            }
            else
            {
                if (playerProximityTime > 0.5f) // If player was nearby for more than 0.5 seconds
                {
                    // Record interaction
                    var interaction = new PlayerInteraction
                    {
                        playerPosition = lastPlayerPosition,
                        npcPosition = transform.position,
                        interactionDuration = playerProximityTime,
                        interactionType = DetermineInteractionType(distanceToPlayer, playerTracker.CurrentSpeed),
                        playerSpeed = playerTracker.CurrentSpeed,
                        proximityDistance = distanceToPlayer,
                        playerInitiated = playerTracker.CurrentSpeed > 1f,
                        timestamp = Time.time,
                        playerStressLevel = CalculatePlayerStress(playerTracker),
                        playerEngagement = CalculatePlayerEngagement(playerTracker)
                    };

                    interactionHistory.Enqueue(interaction);

                    // Maintain history size
                    while (interactionHistory.Count > maxMemorySize)
                    {
                        interactionHistory.Dequeue();
                    }

                    // Store in memory system
                    memorySystem.StoreInteraction(interaction);
                }
                playerProximityTime = 0f;
            }

            lastPlayerPosition = playerPosition;
        }

        private string DetermineInteractionType(float distance, float playerSpeed)
        {
            if (distance < 2f && playerSpeed < 0.5f) return "close_observation";
            if (distance < 3f && playerSpeed > 3f) return "quick_pass";
            if (distance < 5f && playerSpeed < 1f) return "casual_proximity";
            if (playerSpeed > 5f) return "chase_or_flee";
            return "general_proximity";
        }

        private float CalculatePlayerStress(PlayerMovementTracker tracker)
        {
            // Simple stress calculation based on movement patterns
            float speedVariation = Mathf.Abs(tracker.CurrentSpeed - tracker.AverageSpeed);
            float directionChanges = tracker.DirectionChangeFrequency;
            return Mathf.Clamp01((speedVariation + directionChanges) * 0.3f);
        }

        private float CalculatePlayerEngagement(PlayerMovementTracker tracker)
        {
            // Calculate engagement based on exploration and interaction patterns
            float explorationFactor = tracker.ExplorationIndex;
            float speedConsistency = 1f - Mathf.Abs(tracker.CurrentSpeed - tracker.AverageSpeed) / Mathf.Max(tracker.AverageSpeed, 1f);
            return Mathf.Clamp01((explorationFactor + speedConsistency) * 0.5f);
        }

        private void AnalyzeBehaviorPatterns()
        {
            if (interactionHistory.Count < 5) return;

            var recentInteractions = interactionHistory.TakeLast(10).ToArray();
            var patterns = interactionAnalyzer.AnalyzePatterns(recentInteractions, personality);

            // Update behavior weights based on successful patterns
            foreach (var pattern in patterns)
            {
                if (pattern.confidence > 0.7f)
                {
                    UpdateBehaviorWeight(pattern);
                }
            }
        }

        private void UpdateBehaviorWeight(BehaviorPattern pattern)
        {
            string behaviorKey = pattern.recommendedState.ToString().ToLower();
            if (behaviorWeights.ContainsKey(behaviorKey))
            {
                float adjustment = pattern.confidence * adaptationSpeed * Time.deltaTime;
                behaviorWeights[behaviorKey] = Mathf.Clamp01(behaviorWeights[behaviorKey] + adjustment);
            }
        }

        private void UpdateNeuralNetwork()
        {
            if (interactionHistory.Count < 10) return;

            var trainingData = PrepareTrainingData();
            neuralNetwork.Train(trainingData);
        }

        private NPCTrainingData[] PrepareTrainingData()
        {
            var trainingData = new List<NPCTrainingData>();
            var interactions = interactionHistory.ToArray();

            for (int i = 0; i < interactions.Length - 1; i++)
            {
                var input = CreateInputFeatures(interactions[i]);
                var output = CreateOutputTarget(interactions[i + 1]);

                trainingData.Add(new NPCTrainingData
                {
                    inputs = input,
                    expectedOutputs = output
                });
            }

            return trainingData.ToArray();
        }

        private float[] CreateInputFeatures(PlayerInteraction interaction)
        {
            return new float[]
            {
                interaction.proximityDistance / interactionRadius, // Normalized distance
                interaction.playerSpeed / 10f, // Normalized speed
                interaction.interactionDuration / 10f, // Normalized duration
                interaction.playerStressLevel,
                interaction.playerEngagement,
                personality.friendliness,
                personality.curiosity,
                personality.aggression,
                (float)currentState / 8f, // Normalized state
                Time.time % 86400f / 86400f // Time of day normalized
            };
        }

        private float[] CreateOutputTarget(PlayerInteraction nextInteraction)
        {
            // Create target output based on what happened next
            var output = new float[9]; // One for each NPCState
            
            // Determine best state based on interaction outcome
            NPCState bestState = DetermineBestStateForInteraction(nextInteraction);
            output[(int)bestState] = 1f;

            return output;
        }

        private NPCState DetermineBestStateForInteraction(PlayerInteraction interaction)
        {
            if (interaction.interactionDuration > 5f && interaction.playerSpeed < 1f)
                return NPCState.Interacting;
            if (interaction.playerSpeed > 4f && interaction.proximityDistance < 3f)
                return NPCState.Following;
            if (interaction.playerStressLevel > 0.7f)
                return NPCState.Avoiding;
            if (interaction.playerEngagement > 0.6f)
                return NPCState.Investigating;
            
            return NPCState.Patrolling;
        }

        private void AdaptBehavior()
        {
            // Get neural network recommendation
            var currentFeatures = CreateCurrentInputFeatures();
            var recommendation = neuralNetwork.Predict(currentFeatures);

            if (recommendation != null && recommendation.confidence > 0.6f)
            {
                // Gradually adapt to neural network suggestions
                float adaptationFactor = adaptationSpeed * Time.deltaTime;
                
                if (recommendation.recommendedState != currentState)
                {
                    // Consider state change based on personality and learning
                    float stateChangeThreshold = 0.7f - (personality.adaptability * 0.3f);
                    
                    if (recommendation.confidence > stateChangeThreshold)
                    {
                        TransitionToState(recommendation.recommendedState);
                    }
                }
            }
        }

        private float[] CreateCurrentInputFeatures()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return new float[10];

            float distanceToPlayer = Vector3.Distance(transform.position, playerTracker.CurrentPosition);
            
            return new float[]
            {
                distanceToPlayer / interactionRadius,
                playerTracker.CurrentSpeed / 10f,
                playerProximityTime / 10f,
                CalculatePlayerStress(playerTracker),
                CalculatePlayerEngagement(playerTracker),
                personality.friendliness,
                personality.curiosity,
                personality.aggression,
                (float)currentState / 8f,
                Time.time % 86400f / 86400f
            };
        }

        private void TransitionToState(NPCState newState)
        {
            if (currentState == newState) return;

            LogDebug($"NPC transitioning from {currentState} to {newState}");
            
            // Exit current state
            ExitState(currentState);
            
            // Enter new state
            currentState = newState;
            EnterState(newState);
        }

        private void ExitState(NPCState state)
        {
            switch (state)
            {
                case NPCState.Patrolling:
                    navAgent.ResetPath();
                    break;
                case NPCState.Following:
                    navAgent.ResetPath();
                    break;
                case NPCState.Interacting:
                    if (npcAnimator != null)
                        npcAnimator.SetBool("Interacting", false);
                    break;
            }
        }

        private void EnterState(NPCState state)
        {
            switch (state)
            {
                case NPCState.Idle:
                    navAgent.ResetPath();
                    if (npcAnimator != null)
                        npcAnimator.SetBool("Moving", false);
                    break;
                    
                case NPCState.Patrolling:
                    StartPatrolling();
                    break;
                    
                case NPCState.Investigating:
                    StartInvestigating();
                    break;
                    
                case NPCState.Interacting:
                    StartInteracting();
                    break;
                    
                case NPCState.Following:
                    StartFollowing();
                    break;
                    
                case NPCState.Avoiding:
                    StartAvoiding();
                    break;
            }
        }

        private void StartPatrolling()
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                var randomPoint = patrolPoints[Random.Range(0, patrolPoints.Length)];
                navAgent.SetDestination(randomPoint.position);
                
                if (npcAnimator != null)
                    npcAnimator.SetBool("Moving", true);
            }
        }

        private void StartInvestigating()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker != null)
            {
                Vector3 investigationPoint = playerTracker.CurrentPosition + Random.insideUnitSphere * 3f;
                investigationPoint.y = transform.position.y;
                
                navAgent.SetDestination(investigationPoint);
                
                if (npcAnimator != null)
                {
                    npcAnimator.SetBool("Moving", true);
                    npcAnimator.SetBool("Alert", true);
                }
            }
        }

        private void StartInteracting()
        {
            navAgent.ResetPath();
            
            if (npcAnimator != null)
            {
                npcAnimator.SetBool("Moving", false);
                npcAnimator.SetBool("Interacting", true);
            }

            // Look at player
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker != null)
            {
                Vector3 lookDirection = (playerTracker.CurrentPosition - transform.position).normalized;
                lookDirection.y = 0;
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        private void StartFollowing()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker != null)
            {
                Vector3 followPosition = playerTracker.CurrentPosition - playerTracker.transform.forward * 3f;
                navAgent.SetDestination(followPosition);
                
                if (npcAnimator != null)
                    npcAnimator.SetBool("Moving", true);
            }
        }

        private void StartAvoiding()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker != null)
            {
                Vector3 avoidDirection = (transform.position - playerTracker.CurrentPosition).normalized;
                Vector3 avoidPosition = transform.position + avoidDirection * 10f;
                
                navAgent.SetDestination(avoidPosition);
                
                if (npcAnimator != null)
                {
                    npcAnimator.SetBool("Moving", true);
                    npcAnimator.SetBool("Afraid", true);
                }
            }
        }

        private void UpdateNPCBehavior()
        {
            // Update behavior based on current state
            switch (currentState)
            {
                case NPCState.Patrolling:
                    UpdatePatrolling();
                    break;
                case NPCState.Following:
                    UpdateFollowing();
                    break;
                case NPCState.Investigating:
                    UpdateInvestigating();
                    break;
            }

            // Update animations based on movement
            if (npcAnimator != null)
            {
                npcAnimator.SetFloat("Speed", navAgent.velocity.magnitude);
                npcAnimator.SetBool("Moving", navAgent.velocity.magnitude > 0.1f);
            }
        }

        private void UpdatePatrolling()
        {
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
            {
                // Reached patrol point, choose next one
                StartPatrolling();
            }
        }

        private void UpdateFollowing()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTracker.CurrentPosition);
                
                if (distanceToPlayer > 5f)
                {
                    // Update follow position
                    Vector3 followPosition = playerTracker.CurrentPosition - playerTracker.transform.forward * 3f;
                    navAgent.SetDestination(followPosition);
                }
                else if (distanceToPlayer < 2f)
                {
                    // Too close, stop following
                    TransitionToState(NPCState.Idle);
                }
            }
        }

        private void UpdateInvestigating()
        {
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
            {
                // Finished investigating, return to patrol
                TransitionToState(NPCState.Patrolling);
            }
        }

        /// <summary>
        /// Get current NPC state for external systems
        /// </summary>
        public NPCState GetCurrentState()
        {
            return currentState;
        }

        /// <summary>
        /// Get NPC personality for external systems
        /// </summary>
        public NPCPersonality GetPersonality()
        {
            return personality;
        }

        /// <summary>
        /// Get interaction history for analysis
        /// </summary>
        public PlayerInteraction[] GetInteractionHistory()
        {
            return interactionHistory.ToArray();
        }

        /// <summary>
        /// Manually set NPC personality (for testing or special scenarios)
        /// </summary>
        public void SetPersonality(NPCPersonality newPersonality)
        {
            personality = newPersonality;
            LogDebug($"NPC personality manually set to {personality.type}");
        }

        protected override void OnCleanup()
        {
            if (learningCoroutine != null)
            {
                StopCoroutine(learningCoroutine);
                learningCoroutine = null;
            }

            // Clean up neural network resources
            neuralNetwork?.Dispose();
            memorySystem?.Dispose();
            interactionAnalyzer?.Dispose();

            LogDebug("Neural NPC Behavior System cleaned up");
        }
    }

    // Supporting classes for Neural NPC Behavior
    public class NPCNeuralNetwork
    {
        private int neuronsPerLayer;
        private int layerCount;
        private float learningRate;
        private float[,,] weights;
        private float[,] biases;

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
            biases = new float[layerCount - 1, neuronsPerLayer];

            // Initialize with random weights
            System.Random rand = new System.Random();
            for (int layer = 0; layer < layerCount - 1; layer++)
            {
                for (int i = 0; i < neuronsPerLayer; i++)
                {
                    biases[layer, i] = (float)(rand.NextDouble() * 2 - 1) * 0.1f;
                    for (int j = 0; j < neuronsPerLayer; j++)
                    {
                        weights[layer, i, j] = (float)(rand.NextDouble() * 2 - 1) * 0.5f;
                    }
                }
            }
        }

        public BehaviorPattern Predict(float[] inputs)
        {
            if (inputs.Length != neuronsPerLayer) return null;

            float[] currentLayer = new float[neuronsPerLayer];
            System.Array.Copy(inputs, currentLayer, inputs.Length);

            // Forward pass
            for (int layer = 0; layer < layerCount - 1; layer++)
            {
                float[] nextLayer = new float[neuronsPerLayer];
                
                for (int j = 0; j < neuronsPerLayer; j++)
                {
                    float sum = biases[layer, j];
                    for (int i = 0; i < neuronsPerLayer; i++)
                    {
                        sum += currentLayer[i] * weights[layer, i, j];
                    }
                    nextLayer[j] = Mathf.Max(0, sum); // ReLU activation
                }
                
                currentLayer = nextLayer;
            }

            // Find best state
            int bestStateIndex = 0;
            float bestScore = currentLayer[0];
            for (int i = 1; i < 9 && i < currentLayer.Length; i++) // 9 NPC states
            {
                if (currentLayer[i] > bestScore)
                {
                    bestScore = currentLayer[i];
                    bestStateIndex = i;
                }
            }

            return new BehaviorPattern
            {
                patternName = "neural_prediction",
                inputFeatures = inputs,
                recommendedState = (NeuralNPCBehavior.NPCState)bestStateIndex,
                confidence = bestScore,
                suggestedPosition = Vector3.zero,
                suggestedAnimation = "default"
            };
        }

        public void Train(NPCTrainingData[] trainingData)
        {
            // Simple training implementation
            foreach (var data in trainingData)
            {
                var prediction = Predict(data.inputs);
                if (prediction != null)
                {
                    // Calculate error and adjust weights (simplified backpropagation)
                    float error = CalculateError(prediction, data.expectedOutputs);
                    AdjustWeights(data.inputs, error);
                }
            }
        }

        private float CalculateError(BehaviorPattern prediction, float[] expected)
        {
            float error = 0f;
            int stateIndex = (int)prediction.recommendedState;
            if (stateIndex < expected.Length)
            {
                error = expected[stateIndex] - prediction.confidence;
            }
            return error;
        }

        private void AdjustWeights(float[] inputs, float error)
        {
            // Simplified weight adjustment
            float adjustment = error * learningRate;
            
            for (int layer = 0; layer < layerCount - 1; layer++)
            {
                for (int i = 0; i < neuronsPerLayer && i < inputs.Length; i++)
                {
                    for (int j = 0; j < neuronsPerLayer; j++)
                    {
                        weights[layer, i, j] += adjustment * inputs[i] * 0.01f;
                    }
                }
            }
        }

        public void Dispose()
        {
            weights = null;
            biases = null;
        }
    }

    public class NPCTrainingData
    {
        public float[] inputs;
        public float[] expectedOutputs;
    }

    public class BehaviorMemorySystem
    {
        private Queue<NeuralNPCBehavior.PlayerInteraction> memory;
        private int maxSize;

        public BehaviorMemorySystem(int maxSize)
        {
            this.maxSize = maxSize;
            memory = new Queue<NeuralNPCBehavior.PlayerInteraction>();
        }

        public void StoreInteraction(NeuralNPCBehavior.PlayerInteraction interaction)
        {
            memory.Enqueue(interaction);
            while (memory.Count > maxSize)
            {
                memory.Dequeue();
            }
        }

        public NeuralNPCBehavior.PlayerInteraction[] GetRecentInteractions(int count)
        {
            return memory.TakeLast(count).ToArray();
        }

        public void Dispose()
        {
            memory.Clear();
        }
    }

    public class PlayerInteractionAnalyzer
    {
        public BehaviorPattern[] AnalyzePatterns(NeuralNPCBehavior.PlayerInteraction[] interactions, 
            NeuralNPCBehavior.NPCPersonality personality)
        {
            var patterns = new List<BehaviorPattern>();

            if (interactions.Length < 3) return patterns.ToArray();

            // Analyze interaction patterns
            float avgDuration = interactions.Average(i => i.interactionDuration);
            float avgDistance = interactions.Average(i => i.proximityDistance);
            float avgPlayerSpeed = interactions.Average(i => i.playerSpeed);

            // Pattern 1: Player likes close interactions
            if (avgDistance < 3f && avgDuration > 2f)
            {
                patterns.Add(new BehaviorPattern
                {
                    patternName = "close_interaction_preference",
                    recommendedState = NeuralNPCBehavior.NPCState.Interacting,
                    confidence = 0.8f * personality.friendliness
                });
            }

            // Pattern 2: Player prefers to be followed
            if (avgPlayerSpeed > 2f && interactions.Count(i => i.interactionType == "quick_pass") > interactions.Length * 0.6f)
            {
                patterns.Add(new BehaviorPattern
                {
                    patternName = "following_preference",
                    recommendedState = NeuralNPCBehavior.NPCState.Following,
                    confidence = 0.7f * personality.loyalty
                });
            }

            // Pattern 3: Player avoids NPCs
            if (avgDistance > 4f && avgDuration < 1f)
            {
                patterns.Add(new BehaviorPattern
                {
                    patternName = "avoidance_detected",
                    recommendedState = NeuralNPCBehavior.NPCState.Patrolling,
                    confidence = 0.6f * personality.independence
                });
            }

            return patterns.ToArray();
        }

        public void Dispose() { }
    }
}