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
        [Header("🧠 Neural Network Configuration")]
        [SerializeField] private int neuralNetworkLayers = 3;
        [SerializeField] private int neuronsPerLayer = 16;
        [SerializeField] private float learningRate = 0.01f;
        [SerializeField] private float adaptationSpeed = 0.5f;
        [SerializeField] private bool enableRealTimeLearning = true;
        [SerializeField] private bool enableEmotionalIntelligence = true;
        [SerializeField] private bool enableSwarmIntelligence = true;
        [SerializeField] private bool enableQuantumDecisionMaking = false;
        [SerializeField] private bool enablePredictiveAnalytics = true;

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
        private EmotionalIntelligenceEngine emotionalEngine;
        private SwarmIntelligenceCore swarmCore;
        private QuantumDecisionProcessor quantumProcessor;
        private PredictiveAnalyticsEngine predictiveEngine;
        
        // NPC State Management
        private NPCState currentState;
        private NPCPersonality personality;
        private Queue<PlayerInteraction> interactionHistory;
        private Dictionary<string, float> behaviorWeights;
        private EmotionalState currentEmotionalState;
        private Dictionary<string, float> emotionalMemory;
        private List<NPCNeuralBehavior> nearbyNPCs;
        private SwarmBehaviorData swarmData;
        
        // Learning and Adaptation
        private Coroutine learningCoroutine;
        private Coroutine emotionalUpdateCoroutine;
        private Coroutine swarmUpdateCoroutine;
        private float lastLearningUpdate;
        private Vector3 lastPlayerPosition;
        private float playerProximityTime;
        private float emotionalIntensity = 0.5f;
        private float swarmInfluence = 0.3f;

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
            Guarding,
            EmotionalResponse,
            SwarmBehavior,
            QuantumDecision,
            PredictiveAction
        }
        
        public enum EmotionalState
        {
            Neutral,
            Happy,
            Sad,
            Angry,
            Fearful,
            Surprised,
            Disgusted,
            Excited,
            Anxious,
            Confident,
            Empathetic,
            Nostalgic
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
            public EmotionalState detectedPlayerEmotion;
            public float emotionalResonance;
        }
        
        [System.Serializable]
        public class EmotionalResponse
        {
            public EmotionalState emotion;
            public float intensity;
            public float duration;
            public string trigger;
            public Vector3 triggerLocation;
            public float timestamp;
        }
        
        [System.Serializable]
        public class SwarmBehaviorData
        {
            public Vector3 swarmCenter;
            public float swarmRadius;
            public int swarmSize;
            public NPCState dominantBehavior;
            public float cohesionStrength;
            public float separationStrength;
            public float alignmentStrength;
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
                
                // Initialize advanced AI systems
                if (enableEmotionalIntelligence)
                {
                    emotionalEngine = new EmotionalIntelligenceEngine();
                    currentEmotionalState = EmotionalState.Neutral;
                    emotionalMemory = new Dictionary<string, float>();
                }
                
                if (enableSwarmIntelligence)
                {
                    swarmCore = new SwarmIntelligenceCore();
                    nearbyNPCs = new List<NPCNeuralBehavior>();
                    swarmData = new SwarmBehaviorData();
                }
                
                if (enableQuantumDecisionMaking)
                {
                    quantumProcessor = new QuantumDecisionProcessor();
                }
                
                if (enablePredictiveAnalytics)
                {
                    predictiveEngine = new PredictiveAnalyticsEngine();
                }

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
                
                // Start emotional intelligence updates
                if (enableEmotionalIntelligence)
                {
                    emotionalUpdateCoroutine = StartCoroutine(EmotionalUpdateLoop());
                }
                
                // Start swarm intelligence updates
                if (enableSwarmIntelligence)
                {
                    swarmUpdateCoroutine = StartCoroutine(SwarmUpdateLoop());
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
                    
                    // Process quantum decisions if enabled
                    if (enableQuantumDecisionMaking && quantumProcessor != null)
                    {
                        ProcessQuantumDecisions();
                    }
                    
                    // Update predictive analytics
                    if (enablePredictiveAnalytics && predictiveEngine != null)
                    {
                        UpdatePredictiveAnalytics();
                    }

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
        
        #region Emotional Intelligence System
        
        private IEnumerator EmotionalUpdateLoop()
        {
            while (isInitialized && enableEmotionalIntelligence)
            {
                yield return new WaitForSeconds(0.5f); // Update twice per second
                
                try
                {
                    // Analyze emotional context
                    AnalyzeEmotionalContext();
                    
                    // Update emotional state
                    UpdateEmotionalState();
                    
                    // Apply emotional influence to behavior
                    ApplyEmotionalInfluence();
                    
                    // Update emotional memory
                    UpdateEmotionalMemory();
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in emotional intelligence update: {ex.Message}");
                }
            }
        }
        
        private void AnalyzeEmotionalContext()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return;
            
            // Analyze player's emotional state based on behavior
            EmotionalState detectedPlayerEmotion = emotionalEngine.AnalyzePlayerEmotion(playerTracker);
            
            // Calculate emotional resonance
            float resonance = emotionalEngine.CalculateEmotionalResonance(currentEmotionalState, detectedPlayerEmotion, personality);
            
            // Store emotional context
            if (interactionHistory.Count > 0)
            {
                var lastInteraction = interactionHistory.Last();
                lastInteraction.detectedPlayerEmotion = detectedPlayerEmotion;
                lastInteraction.emotionalResonance = resonance;
            }
        }
        
        private void UpdateEmotionalState()
        {
            // Get emotional triggers from environment
            var triggers = emotionalEngine.DetectEmotionalTriggers(transform.position, interactionHistory.ToArray());
            
            foreach (var trigger in triggers)
            {
                ProcessEmotionalTrigger(trigger);
            }
            
            // Natural emotional decay towards neutral
            emotionalIntensity = Mathf.Lerp(emotionalIntensity, 0.5f, Time.deltaTime * 0.1f);
            
            // Update emotional state based on intensity and personality
            currentEmotionalState = emotionalEngine.DetermineEmotionalState(emotionalIntensity, personality, currentEmotionalState);
        }
        
        private void ProcessEmotionalTrigger(EmotionalTrigger trigger)
        {
            // Apply emotional response based on personality
            float emotionalImpact = trigger.intensity * personality.adaptability;
            
            switch (trigger.type)
            {
                case EmotionalTriggerType.PlayerApproach:
                    if (personality.friendliness > 0.6f)
                        emotionalIntensity += emotionalImpact * 0.3f;
                    else if (personality.aggression > 0.6f)
                        emotionalIntensity += emotionalImpact * 0.5f;
                    break;
                    
                case EmotionalTriggerType.PlayerFlee:
                    if (personality.curiosity > 0.7f)
                        emotionalIntensity += emotionalImpact * 0.4f;
                    break;
                    
                case EmotionalTriggerType.LongInteraction:
                    if (personality.socialness > 0.5f)
                        emotionalIntensity += emotionalImpact * 0.6f;
                    break;
            }
            
            emotionalIntensity = Mathf.Clamp01(emotionalIntensity);
        }
        
        private void ApplyEmotionalInfluence()
        {
            // Modify behavior weights based on emotional state
            float emotionalModifier = GetEmotionalBehaviorModifier();
            
            switch (currentEmotionalState)
            {
                case EmotionalState.Happy:
                    behaviorWeights["interact"] *= 1.3f;
                    behaviorWeights["socialize"] *= 1.4f;
                    behaviorWeights["avoid"] *= 0.7f;
                    break;
                    
                case EmotionalState.Fearful:
                    behaviorWeights["avoid"] *= 1.5f;
                    behaviorWeights["interact"] *= 0.6f;
                    behaviorWeights["guard"] *= 1.2f;
                    break;
                    
                case EmotionalState.Curious:
                    behaviorWeights["investigate"] *= 1.4f;
                    behaviorWeights["follow"] *= 1.2f;
                    behaviorWeights["patrol"] *= 0.8f;
                    break;
                    
                case EmotionalState.Angry:
                    behaviorWeights["avoid"] *= 1.3f;
                    behaviorWeights["guard"] *= 1.4f;
                    behaviorWeights["socialize"] *= 0.5f;
                    break;
            }
            
            // Normalize behavior weights
            NormalizeBehaviorWeights();
        }
        
        private float GetEmotionalBehaviorModifier()
        {
            return 0.5f + (emotionalIntensity * 0.5f);
        }
        
        private void UpdateEmotionalMemory()
        {
            string emotionalKey = $"{currentEmotionalState}_{Time.time:F0}";
            emotionalMemory[emotionalKey] = emotionalIntensity;
            
            // Maintain memory size
            if (emotionalMemory.Count > 100)
            {
                var oldestKey = emotionalMemory.Keys.First();
                emotionalMemory.Remove(oldestKey);
            }
        }
        
        #endregion
        
        #region Swarm Intelligence System
        
        private IEnumerator SwarmUpdateLoop()
        {
            while (isInitialized && enableSwarmIntelligence)
            {
                yield return new WaitForSeconds(1f); // Update once per second
                
                try
                {
                    // Find nearby NPCs
                    UpdateNearbyNPCs();
                    
                    // Calculate swarm behavior
                    CalculateSwarmBehavior();
                    
                    // Apply swarm influence
                    ApplySwarmInfluence();
                    
                    // Share information with swarm
                    ShareSwarmInformation();
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in swarm intelligence update: {ex.Message}");
                }
            }
        }
        
        private void UpdateNearbyNPCs()
        {
            nearbyNPCs.Clear();
            
            var allNPCs = FindObjectsOfType<NPCNeuralBehavior>();
            foreach (var npc in allNPCs)
            {
                if (npc != this && Vector3.Distance(transform.position, npc.transform.position) <= 15f)
                {
                    nearbyNPCs.Add(npc);
                }
            }
        }
        
        private void CalculateSwarmBehavior()
        {
            if (nearbyNPCs.Count == 0) return;
            
            // Calculate swarm center
            Vector3 center = Vector3.zero;
            foreach (var npc in nearbyNPCs)
            {
                center += npc.transform.position;
            }
            center /= nearbyNPCs.Count;
            swarmData.swarmCenter = center;
            
            // Calculate swarm properties
            swarmData.swarmSize = nearbyNPCs.Count + 1; // Include self
            swarmData.swarmRadius = CalculateSwarmRadius();
            swarmData.dominantBehavior = CalculateDominantBehavior();
            
            // Calculate swarm forces
            swarmData.cohesionStrength = CalculateCohesion();
            swarmData.separationStrength = CalculateSeparation();
            swarmData.alignmentStrength = CalculateAlignment();
        }
        
        private float CalculateSwarmRadius()
        {
            float maxDistance = 0f;
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(swarmData.swarmCenter, npc.transform.position);
                if (distance > maxDistance)
                    maxDistance = distance;
            }
            return maxDistance;
        }
        
        private NPCState CalculateDominantBehavior()
        {
            var behaviorCounts = new Dictionary<NPCState, int>();
            
            // Count current behaviors
            behaviorCounts[currentState] = behaviorCounts.GetValueOrDefault(currentState, 0) + 1;
            foreach (var npc in nearbyNPCs)
            {
                var npcState = npc.GetCurrentState();
                behaviorCounts[npcState] = behaviorCounts.GetValueOrDefault(npcState, 0) + 1;
            }
            
            // Find most common behavior
            return behaviorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        
        private float CalculateCohesion()
        {
            // Tendency to move toward swarm center
            float distance = Vector3.Distance(transform.position, swarmData.swarmCenter);
            return Mathf.Clamp01(distance / 10f) * personality.socialness;
        }
        
        private float CalculateSeparation()
        {
            // Tendency to avoid crowding
            float crowdingFactor = 0f;
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                if (distance < 3f)
                {
                    crowdingFactor += (3f - distance) / 3f;
                }
            }
            return Mathf.Clamp01(crowdingFactor) * personality.independence;
        }
        
        private float CalculateAlignment()
        {
            // Tendency to align with swarm movement
            if (nearbyNPCs.Count == 0) return 0f;
            
            Vector3 averageVelocity = Vector3.zero;
            foreach (var npc in nearbyNPCs)
            {
                if (npc.navAgent != null)
                    averageVelocity += npc.navAgent.velocity;
            }
            averageVelocity /= nearbyNPCs.Count;
            
            float alignment = Vector3.Dot(navAgent.velocity.normalized, averageVelocity.normalized);
            return Mathf.Clamp01(alignment) * personality.adaptability;
        }
        
        private void ApplySwarmInfluence()
        {
            if (nearbyNPCs.Count == 0) return;
            
            // Influence behavior based on swarm
            float swarmBehaviorInfluence = swarmInfluence * personality.socialness;
            
            if (swarmData.dominantBehavior != currentState && Random.value < swarmBehaviorInfluence)
            {
                // Consider adopting swarm behavior
                if (ShouldAdoptSwarmBehavior(swarmData.dominantBehavior))
                {
                    TransitionToState(swarmData.dominantBehavior);
                }
            }
            
            // Apply swarm movement forces
            ApplySwarmMovement();
        }
        
        private bool ShouldAdoptSwarmBehavior(NPCState swarmBehavior)
        {
            // Don't adopt behavior if it conflicts with personality
            switch (swarmBehavior)
            {
                case NPCState.Aggressive:
                    return personality.aggression > 0.4f;
                case NPCState.Socializing:
                    return personality.socialness > 0.5f;
                case NPCState.Avoiding:
                    return personality.independence < 0.7f;
                default:
                    return true;
            }
        }
        
        private void ApplySwarmMovement()
        {
            if (currentState != NPCState.Patrolling && currentState != NPCState.Idle) return;
            
            Vector3 swarmForce = Vector3.zero;
            
            // Cohesion force
            Vector3 cohesionForce = (swarmData.swarmCenter - transform.position).normalized * swarmData.cohesionStrength;
            
            // Separation force
            Vector3 separationForce = Vector3.zero;
            foreach (var npc in nearbyNPCs)
            {
                Vector3 diff = transform.position - npc.transform.position;
                float distance = diff.magnitude;
                if (distance < 3f && distance > 0f)
                {
                    separationForce += diff.normalized / distance;
                }
            }
            separationForce *= swarmData.separationStrength;
            
            // Combine forces
            swarmForce = cohesionForce + separationForce;
            swarmForce.y = 0; // Keep on ground plane
            
            // Apply to movement
            if (swarmForce.magnitude > 0.1f)
            {
                Vector3 targetPosition = transform.position + swarmForce.normalized * 5f;
                navAgent.SetDestination(targetPosition);
            }
        }
        
        private void ShareSwarmInformation()
        {
            // Share emotional state and behavior patterns with nearby NPCs
            var sharedInfo = new SwarmInformation
            {
                sourceNPC = this,
                emotionalState = currentEmotionalState,
                behaviorWeights = new Dictionary<string, float>(behaviorWeights),
                playerInteractionQuality = CalculatePlayerInteractionQuality(),
                timestamp = Time.time
            };
            
            foreach (var npc in nearbyNPCs)
            {
                npc.ReceiveSwarmInformation(sharedInfo);
            }
        }
        
        private float CalculatePlayerInteractionQuality()
        {
            if (interactionHistory.Count == 0) return 0.5f;
            
            var recentInteractions = interactionHistory.TakeLast(5);
            float totalQuality = 0f;
            
            foreach (var interaction in recentInteractions)
            {
                float quality = (interaction.playerEngagement + interaction.emotionalResonance) * 0.5f;
                totalQuality += quality;
            }
            
            return totalQuality / recentInteractions.Count();
        }
        
        public void ReceiveSwarmInformation(SwarmInformation info)
        {
            if (!enableSwarmIntelligence) return;
            
            // Learn from other NPCs' experiences
            float influenceStrength = 0.1f * personality.adaptability;
            
            // Adjust behavior weights based on successful patterns
            if (info.playerInteractionQuality > 0.7f)
            {
                foreach (var weight in info.behaviorWeights)
                {
                    if (behaviorWeights.ContainsKey(weight.Key))
                    {
                        behaviorWeights[weight.Key] = Mathf.Lerp(behaviorWeights[weight.Key], weight.Value, influenceStrength);
                    }
                }
            }
            
            // Emotional contagion
            if (info.emotionalState != EmotionalState.Neutral && personality.socialness > 0.6f)
            {
                float emotionalInfluence = influenceStrength * 0.5f;
                emotionalIntensity = Mathf.Lerp(emotionalIntensity, 0.7f, emotionalInfluence);
            }
        }
        
        #endregion
        
        #region Quantum Decision Making
        
        private void ProcessQuantumDecisions()
        {
            if (quantumProcessor == null) return;
            
            // Create quantum superposition of possible decisions
            var possibleStates = System.Enum.GetValues(typeof(NPCState)).Cast<NPCState>().ToArray();
            var quantumStates = quantumProcessor.CreateDecisionSuperposition(possibleStates, behaviorWeights);
            
            // Process quantum decision
            var quantumDecision = quantumProcessor.CollapseToOptimalDecision(quantumStates, personality, currentEmotionalState);
            
            if (quantumDecision != null && quantumDecision.confidence > 0.8f)
            {
                // Apply quantum decision with high confidence
                if (quantumDecision.recommendedState != currentState)
                {
                    LogDebug($"Quantum decision: {currentState} -> {quantumDecision.recommendedState} (confidence: {quantumDecision.confidence:F2})");
                    TransitionToState(quantumDecision.recommendedState);
                }
            }
        }
        
        #endregion
        
        #region Predictive Analytics
        
        private void UpdatePredictiveAnalytics()
        {
            if (predictiveEngine == null || interactionHistory.Count < 10) return;
            
            // Predict future player behavior
            var predictions = predictiveEngine.PredictPlayerBehavior(interactionHistory.ToArray(), personality);
            
            foreach (var prediction in predictions)
            {
                if (prediction.confidence > 0.7f)
                {
                    PrepareForPredictedBehavior(prediction);
                }
            }
        }
        
        private void PrepareForPredictedBehavior(BehaviorPrediction prediction)
        {
            switch (prediction.predictedBehavior)
            {
                case "player_approach":
                    if (personality.friendliness > 0.6f)
                    {
                        // Prepare friendly interaction
                        behaviorWeights["interact"] *= 1.2f;
                    }
                    break;
                    
                case "player_avoid":
                    // Reduce interaction attempts
                    behaviorWeights["follow"] *= 0.8f;
                    behaviorWeights["interact"] *= 0.7f;
                    break;
                    
                case "player_explore":
                    // Increase patrol and investigation
                    behaviorWeights["patrol"] *= 1.1f;
                    behaviorWeights["investigate"] *= 1.2f;
                    break;
            }
            
            NormalizeBehaviorWeights();
        }
        
        #endregion
        
        private void NormalizeBehaviorWeights()
        {
            float sum = behaviorWeights.Values.Sum();
            if (sum > 0)
            {
                var keys = behaviorWeights.Keys.ToArray();
                foreach (var key in keys)
                {
                    behaviorWeights[key] /= sum;
                }
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
        
        /// <summary>
        /// Get current emotional state
        /// </summary>
        public EmotionalState GetEmotionalState()
        {
            return currentEmotionalState;
        }
        
        /// <summary>
        /// Get emotional intensity (0-1)
        /// </summary>
        public float GetEmotionalIntensity()
        {
            return emotionalIntensity;
        }
        
        /// <summary>
        /// Get swarm behavior data
        /// </summary>
        public SwarmBehaviorData GetSwarmData()
        {
            return swarmData;
        }
        
        /// <summary>
        /// Force emotional state change (for testing)
        /// </summary>
        public void SetEmotionalState(EmotionalState newState, float intensity = 0.7f)
        {
            currentEmotionalState = newState;
            emotionalIntensity = Mathf.Clamp01(intensity);
            LogDebug($"NPC emotional state set to {newState} with intensity {intensity:F2}");
        }
        
        /// <summary>
        /// Get comprehensive NPC status for debugging
        /// </summary>
        public NPCStatus GetNPCStatus()
        {
            return new NPCStatus
            {
                currentState = currentState,
                personality = personality,
                emotionalState = currentEmotionalState,
                emotionalIntensity = emotionalIntensity,
                swarmSize = nearbyNPCs?.Count ?? 0,
                interactionCount = interactionHistory.Count,
                learningEnabled = enableRealTimeLearning,
                emotionalIntelligenceEnabled = enableEmotionalIntelligence,
                swarmIntelligenceEnabled = enableSwarmIntelligence,
                quantumDecisionMakingEnabled = enableQuantumDecisionMaking
            };
        }

        protected override void OnCleanup()
        {
            // Stop all coroutines
            if (learningCoroutine != null)
            {
                StopCoroutine(learningCoroutine);
                learningCoroutine = null;
            }
            
            if (emotionalUpdateCoroutine != null)
            {
                StopCoroutine(emotionalUpdateCoroutine);
                emotionalUpdateCoroutine = null;
            }
            
            if (swarmUpdateCoroutine != null)
            {
                StopCoroutine(swarmUpdateCoroutine);
                swarmUpdateCoroutine = null;
            }

            // Clean up neural network resources
            neuralNetwork?.Dispose();
            memorySystem?.Dispose();
            interactionAnalyzer?.Dispose();
            emotionalEngine?.Dispose();
            swarmCore?.Dispose();
            quantumProcessor?.Dispose();
            predictiveEngine?.Dispose();

            LogDebug("Enhanced Neural NPC Behavior System cleaned up");
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