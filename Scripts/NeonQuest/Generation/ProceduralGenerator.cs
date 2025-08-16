using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using NeonQuest.Core;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Central coordinator for all procedural generation systems.
    /// Manages generation queue, priority system, and coordination between
    /// layout, lighting, audio, and fog systems.
    /// </summary>
    public class ProceduralGenerator : MonoBehaviour, IProceduralGenerator
    {
        [Header("Generation Systems")]
        [SerializeField] private LayoutManager layoutManager;
        [SerializeField] private LightingEngine lightingEngine;
        [SerializeField] private AudioEngine audioEngine;
        [SerializeField] private FogEffectsEngine fogEffectsEngine;
        
        [Header("Configuration")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private int maxConcurrentGenerations = 5;
        [SerializeField] private float performanceThreshold = 0.8f;
        [SerializeField] private bool enablePerformanceThrottling = true;
        
        [Header("Coordination")]
        [SerializeField] private float coordinationDelay = 0.2f;
        [SerializeField] private bool enableSystemCoordination = true;
        
        private Queue<GenerationRequest> generationQueue;
        private List<GenerationRequest> activeGenerations;
        private Dictionary<string, IProceduralGenerator> registeredSystems;
        private EnvironmentConfiguration config;
        private PlayerBehaviorAnalyzer behaviorAnalyzer;
        private bool isActive = true;
        private float currentPerformanceCost = 0f;
        private float qualityLevel = 1f;
        private Coroutine coordinatorCoroutine;
        
        // Performance tracking
        private float lastFrameTime;
        private float averageFrameTime;
        private int frameCount;
        
        public float CurrentPerformanceCost => currentPerformanceCost;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            generationQueue = new Queue<GenerationRequest>();
            activeGenerations = new List<GenerationRequest>();
            registeredSystems = new Dictionary<string, IProceduralGenerator>();
            
            InitializeSystems();
        }
        
        private void Start()
        {
            behaviorAnalyzer = FindObjectOfType<PlayerBehaviorAnalyzer>();
            
            if (isActive)
            {
                StartCoordinator();
            }
        }
        
        private void OnDestroy()
        {
            StopCoordinator();
        }
        
        public void Initialize(Dictionary<string, object> configData)
        {
            if (configData.TryGetValue("config", out var configObj) && configObj is EnvironmentConfiguration envConfig)
            {
                config = envConfig;
                performanceThreshold = config.PerformanceThreshold;
            }
            
            // Initialize all registered systems
            foreach (var system in registeredSystems.Values)
            {
                system.Initialize(configData);
            }
        }
        
        public async Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            if (!isActive) return null;
            
            // Create generation request
            var request = new GenerationRequest
            {
                Id = System.Guid.NewGuid().ToString(),
                Parameters = generationParams,
                Priority = GetRequestPriority(generationParams),
                RequestTime = Time.time,
                Status = GenerationStatus.Queued
            };
            
            // Add to queue
            EnqueueGeneration(request);
            
            // Wait for completion
            while (request.Status == GenerationStatus.Queued || request.Status == GenerationStatus.Processing)
            {
                await Task.Yield();
            }
            
            return request.Result;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            if (!isActive) return;
            
            // Update performance tracking
            UpdatePerformanceTracking(deltaTime);
            
            // Update all registered systems
            foreach (var system in registeredSystems.Values)
            {
                if (system.IsActive)
                {
                    system.UpdateGeneration(deltaTime, environmentState);
                }
            }
            
            // Update performance cost
            UpdatePerformanceCost();
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            // Cleanup all registered systems
            foreach (var system in registeredSystems.Values)
            {
                system.CleanupDistantContent(cleanupDistance, playerPosition);
            }
            
            // Cleanup completed generation requests
            CleanupCompletedRequests();
        }
        
        public void SetQualityLevel(float qualityLevel)
        {
            this.qualityLevel = Mathf.Clamp01(qualityLevel);
            
            // Propagate quality level to all systems
            foreach (var system in registeredSystems.Values)
            {
                system.SetQualityLevel(this.qualityLevel);
            }
            
            // Adjust coordinator settings based on quality
            maxConcurrentGenerations = Mathf.RoundToInt(5 * this.qualityLevel);
            updateInterval = Mathf.Lerp(0.2f, 0.05f, this.qualityLevel);
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            
            if (active && coordinatorCoroutine == null)
            {
                StartCoordinator();
            }
            else if (!active && coordinatorCoroutine != null)
            {
                StopCoordinator();
            }
            
            // Propagate active state to all systems
            foreach (var system in registeredSystems.Values)
            {
                system.SetActive(active);
            }
        }
        
        private void InitializeSystems()
        {
            // Find and register generation systems
            if (layoutManager == null)
                layoutManager = FindObjectOfType<LayoutManager>();
            if (lightingEngine == null)
                lightingEngine = FindObjectOfType<LightingEngine>();
            if (audioEngine == null)
                audioEngine = FindObjectOfType<AudioEngine>();
            if (fogEffectsEngine == null)
                fogEffectsEngine = FindObjectOfType<FogEffectsEngine>();
            
            // Register systems
            RegisterSystem("layout", layoutManager);
            RegisterSystem("lighting", lightingEngine);
            RegisterSystem("audio", audioEngine);
            RegisterSystem("fog", fogEffectsEngine);
        }
        
        private void RegisterSystem(string systemId, IProceduralGenerator system)
        {
            if (system != null)
            {
                registeredSystems[systemId] = system;
                Debug.Log($"ProceduralGenerator: Registered {systemId} system");
            }
            else
            {
                Debug.LogWarning($"ProceduralGenerator: {systemId} system not found");
            }
        }
        
        private void StartCoordinator()
        {
            if (coordinatorCoroutine != null)
            {
                StopCoroutine(coordinatorCoroutine);
            }
            coordinatorCoroutine = StartCoroutine(CoordinatorLoop());
        }
        
        private void StopCoordinator()
        {
            if (coordinatorCoroutine != null)
            {
                StopCoroutine(coordinatorCoroutine);
                coordinatorCoroutine = null;
            }
        }
        
        private IEnumerator CoordinatorLoop()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(updateInterval);
                
                ProcessGenerationQueue();
                UpdateActiveGenerations();
                ApplyPerformanceThrottling();
                
                if (enableSystemCoordination)
                {
                    CoordinateSystemEffects();
                }
            }
        }
        
        private void ProcessGenerationQueue()
        {
            // Process queued generation requests based on priority and capacity
            while (generationQueue.Count > 0 && 
                   activeGenerations.Count < maxConcurrentGenerations &&
                   currentPerformanceCost < performanceThreshold)
            {
                var request = generationQueue.Dequeue();
                StartGenerationRequest(request);
            }
        }
        
        private void StartGenerationRequest(GenerationRequest request)
        {
            request.Status = GenerationStatus.Processing;
            request.StartTime = Time.time;
            activeGenerations.Add(request);
            
            // Start async generation
            StartCoroutine(ExecuteGenerationRequest(request));
        }
        
        private IEnumerator ExecuteGenerationRequest(GenerationRequest request)
        {
            try
            {
                // Determine which system should handle this request
                var targetSystem = DetermineTargetSystem(request.Parameters);
                
                if (targetSystem != null)
                {
                    // Execute generation
                    var generationTask = targetSystem.GenerateAsync(request.Parameters);
                    
                    // Wait for completion
                    while (!generationTask.IsCompleted)
                    {
                        yield return null;
                    }
                    
                    request.Result = generationTask.Result;
                    request.Status = GenerationStatus.Completed;
                    
                    // Trigger coordinated effects if needed
                    if (enableSystemCoordination)
                    {
                        yield return StartCoroutine(TriggerCoordinatedEffects(request));
                    }
                }
                else
                {
                    request.Status = GenerationStatus.Failed;
                    Debug.LogWarning($"ProceduralGenerator: No suitable system found for request {request.Id}");
                }
            }
            catch (System.Exception ex)
            {
                request.Status = GenerationStatus.Failed;
                request.ErrorMessage = ex.Message;
                Debug.LogError($"ProceduralGenerator: Generation request {request.Id} failed: {ex.Message}");
            }
            
            request.CompletionTime = Time.time;
        }
        
        private IProceduralGenerator DetermineTargetSystem(Dictionary<string, object> parameters)
        {
            // Determine which system should handle the request based on parameters
            if (parameters.ContainsKey("layoutType") || parameters.ContainsKey("segmentType"))
            {
                return registeredSystems.GetValueOrDefault("layout");
            }
            
            if (parameters.ContainsKey("lightingEffect") || parameters.ContainsKey("lightObject"))
            {
                return registeredSystems.GetValueOrDefault("lighting");
            }
            
            if (parameters.ContainsKey("audioZone") || parameters.ContainsKey("spatialAudio"))
            {
                return registeredSystems.GetValueOrDefault("audio");
            }
            
            if (parameters.ContainsKey("fogEffect") || parameters.ContainsKey("atmosphericChange"))
            {
                return registeredSystems.GetValueOrDefault("fog");
            }
            
            // Default to layout manager for general generation requests
            return registeredSystems.GetValueOrDefault("layout");
        }
        
        private IEnumerator TriggerCoordinatedEffects(GenerationRequest request)
        {
            // Coordinate effects between systems based on the generation type
            var parameters = request.Parameters;
            
            if (parameters.ContainsKey("position") && parameters["position"] is Vector3 position)
            {
                // Coordinate lighting effects
                if (registeredSystems.TryGetValue("lighting", out var lightingSystem) && 
                    lightingSystem is LightingEngine lighting)
                {
                    lighting.TriggerPulseEffect(position, 10f, 1.2f);
                    yield return new WaitForSeconds(coordinationDelay);
                }
                
                // Coordinate audio effects
                if (registeredSystems.TryGetValue("audio", out var audioSystem) && 
                    audioSystem is AudioEngine audio)
                {
                    // Create spatial audio for new content
                    string audioId = $"generated_{request.Id}";
                    // Note: Would need audio clip reference in real implementation
                    yield return new WaitForSeconds(coordinationDelay);
                }
                
                // Coordinate fog effects
                if (registeredSystems.TryGetValue("fog", out var fogSystem) && 
                    fogSystem is FogEffectsEngine fog)
                {
                    fog.CreateCustomFogEffect($"generation_{request.Id}", 0.3f, Color.gray, 2f, 3);
                    yield return new WaitForSeconds(coordinationDelay);
                }
            }
        }
        
        private void UpdateActiveGenerations()
        {
            // Remove completed generations
            activeGenerations.RemoveAll(request => 
                request.Status == GenerationStatus.Completed || 
                request.Status == GenerationStatus.Failed);
        }
        
        private void ApplyPerformanceThrottling()
        {
            if (!enablePerformanceThrottling) return;
            
            // If performance cost is too high, reduce quality or pause generation
            if (currentPerformanceCost > performanceThreshold)
            {
                // Reduce quality level temporarily
                float targetQuality = qualityLevel * 0.8f;
                SetQualityLevel(targetQuality);
                
                // Clear queue if performance is critically low
                if (currentPerformanceCost > 0.95f)
                {
                    generationQueue.Clear();
                    Debug.LogWarning("ProceduralGenerator: Performance critically low, clearing generation queue");
                }
            }
            else if (currentPerformanceCost < performanceThreshold * 0.5f && qualityLevel < 1f)
            {
                // Restore quality level if performance improves
                float targetQuality = Mathf.Min(1f, qualityLevel * 1.1f);
                SetQualityLevel(targetQuality);
            }
        }
        
        private void CoordinateSystemEffects()
        {
            // Coordinate effects between systems based on player behavior
            if (behaviorAnalyzer != null)
            {
                var behaviorData = behaviorAnalyzer.GetCurrentBehaviorData();
                
                // Coordinate based on movement patterns
                if (behaviorData.ContainsKey("movementSpeed") && 
                    behaviorData["movementSpeed"] is float speed && speed > 5f)
                {
                    // Player is moving fast, trigger coordinated surge effects
                    TriggerMovementSurgeEffects();
                }
                
                // Coordinate based on exploration patterns
                if (behaviorData.ContainsKey("explorationState") && 
                    behaviorData["explorationState"] is string state)
                {
                    CoordinateExplorationEffects(state);
                }
            }
        }
        
        private void TriggerMovementSurgeEffects()
        {
            // Get player position for coordinated effects
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            Vector3 playerPosition = player.transform.position;
            
            // Trigger lighting surge
            if (registeredSystems.TryGetValue("lighting", out var lightingSystem) && 
                lightingSystem is LightingEngine lighting)
            {
                lighting.TriggerPulseEffect(playerPosition, 15f, 1.5f);
            }
            
            // Trigger fog intensity
            if (registeredSystems.TryGetValue("fog", out var fogSystem) && 
                fogSystem is FogEffectsEngine fog)
            {
                fog.CreateCustomFogEffect("movement_surge", 0.4f, new Color(0.9f, 0.9f, 1f), 1f, 6);
            }
        }
        
        private void CoordinateExplorationEffects(string explorationState)
        {
            switch (explorationState.ToLower())
            {
                case "exploring":
                    // Subtle atmospheric enhancement
                    if (registeredSystems.TryGetValue("fog", out var fogSystem) && 
                        fogSystem is FogEffectsEngine fog)
                    {
                        fog.CreateCustomFogEffect("exploration", 0.2f, Color.white, 3f, 4);
                    }
                    break;
                    
                case "backtracking":
                    // Dimmer, more subdued effects
                    if (registeredSystems.TryGetValue("audio", out var audioSystem) && 
                        audioSystem is AudioEngine audio)
                    {
                        audio.ModifyAmbientVolume(0.6f);
                    }
                    break;
            }
        }
        
        private void EnqueueGeneration(GenerationRequest request)
        {
            // Insert request based on priority (higher priority first)
            var queueList = generationQueue.ToList();
            queueList.Add(request);
            queueList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            generationQueue.Clear();
            foreach (var req in queueList)
            {
                generationQueue.Enqueue(req);
            }
        }
        
        private int GetRequestPriority(Dictionary<string, object> parameters)
        {
            // Determine priority based on request type and context
            int priority = 5; // Default priority
            
            if (parameters.ContainsKey("priority") && parameters["priority"] is int explicitPriority)
            {
                return explicitPriority;
            }
            
            // Layout generation has high priority
            if (parameters.ContainsKey("layoutType"))
                priority = 8;
            
            // Player-triggered effects have higher priority
            if (parameters.ContainsKey("playerTriggered") && (bool)parameters["playerTriggered"])
                priority += 2;
            
            // Emergency or performance-critical requests
            if (parameters.ContainsKey("urgent") && (bool)parameters["urgent"])
                priority = 10;
            
            return priority;
        }
        
        private void UpdatePerformanceTracking(float deltaTime)
        {
            lastFrameTime = deltaTime;
            averageFrameTime = (averageFrameTime * frameCount + deltaTime) / (frameCount + 1);
            frameCount++;
            
            // Reset average every 100 frames to keep it current
            if (frameCount >= 100)
            {
                frameCount = 0;
                averageFrameTime = deltaTime;
            }
        }
        
        private void UpdatePerformanceCost()
        {
            // Calculate combined performance cost from all systems
            float totalCost = 0f;
            int activeSystems = 0;
            
            foreach (var system in registeredSystems.Values)
            {
                if (system.IsActive)
                {
                    totalCost += system.CurrentPerformanceCost;
                    activeSystems++;
                }
            }
            
            // Add coordinator overhead
            float coordinatorCost = activeGenerations.Count / (float)maxConcurrentGenerations;
            float queueCost = generationQueue.Count * 0.01f;
            
            currentPerformanceCost = activeSystems > 0 ? 
                (totalCost / activeSystems) + coordinatorCost + queueCost : 
                coordinatorCost + queueCost;
            
            currentPerformanceCost = Mathf.Clamp01(currentPerformanceCost);
        }
        
        private void CleanupCompletedRequests()
        {
            // Remove old completed requests to prevent memory leaks
            float cleanupTime = Time.time - 60f; // Keep requests for 1 minute
            
            activeGenerations.RemoveAll(request => 
                (request.Status == GenerationStatus.Completed || request.Status == GenerationStatus.Failed) &&
                request.CompletionTime < cleanupTime);
        }
        
        /// <summary>
        /// Gets diagnostic information about the coordinator
        /// </summary>
        public Dictionary<string, object> GetDiagnosticInfo()
        {
            return new Dictionary<string, object>
            {
                ["QueuedRequests"] = generationQueue.Count,
                ["ActiveGenerations"] = activeGenerations.Count,
                ["RegisteredSystems"] = registeredSystems.Count,
                ["PerformanceCost"] = currentPerformanceCost,
                ["QualityLevel"] = qualityLevel,
                ["AverageFrameTime"] = averageFrameTime,
                ["IsActive"] = isActive,
                ["PerformanceThrottling"] = enablePerformanceThrottling,
                ["SystemCoordination"] = enableSystemCoordination
            };
        }
        
        /// <summary>
        /// Gets information about all registered systems
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> GetSystemsInfo()
        {
            var systemsInfo = new Dictionary<string, Dictionary<string, object>>();
            
            foreach (var kvp in registeredSystems)
            {
                systemsInfo[kvp.Key] = new Dictionary<string, object>
                {
                    ["IsActive"] = kvp.Value.IsActive,
                    ["PerformanceCost"] = kvp.Value.CurrentPerformanceCost,
                    ["Type"] = kvp.Value.GetType().Name
                };
            }
            
            return systemsInfo;
        }
        
        /// <summary>
        /// Manually trigger a coordinated atmospheric change
        /// </summary>
        public void TriggerCoordinatedAtmosphericChange(string context, Vector3 position, float intensity = 1f)
        {
            StartCoroutine(ExecuteCoordinatedAtmosphericChange(context, position, intensity));
        }
        
        private IEnumerator ExecuteCoordinatedAtmosphericChange(string context, Vector3 position, float intensity)
        {
            // Coordinate all systems for a dramatic atmospheric change
            
            // Lighting first
            if (registeredSystems.TryGetValue("lighting", out var lightingSystem) && 
                lightingSystem is LightingEngine lighting)
            {
                lighting.TriggerPulseEffect(position, 20f * intensity, 1.5f * intensity);
                yield return new WaitForSeconds(coordinationDelay);
            }
            
            // Fog effects
            if (registeredSystems.TryGetValue("fog", out var fogSystem) && 
                fogSystem is FogEffectsEngine fog)
            {
                fog.TriggerCoordinatedAtmosphericChange(context);
                yield return new WaitForSeconds(coordinationDelay);
            }
            
            // Audio changes
            if (registeredSystems.TryGetValue("audio", out var audioSystem) && 
                audioSystem is AudioEngine audio)
            {
                audio.ModifyAmbientVolume(0.8f * intensity);
                yield return new WaitForSeconds(coordinationDelay);
            }
        }
    }
    
    /// <summary>
    /// Represents a generation request in the queue
    /// </summary>
    [System.Serializable]
    public class GenerationRequest
    {
        public string Id;
        public Dictionary<string, object> Parameters;
        public int Priority;
        public float RequestTime;
        public float StartTime;
        public float CompletionTime;
        public GenerationStatus Status;
        public GameObject Result;
        public string ErrorMessage;
        
        public float ProcessingTime => CompletionTime - StartTime;
        public float WaitTime => StartTime - RequestTime;
    }
    
    /// <summary>
    /// Status of a generation request
    /// </summary>
    public enum GenerationStatus
    {
        Queued,
        Processing,
        Completed,
        Failed
    }
}