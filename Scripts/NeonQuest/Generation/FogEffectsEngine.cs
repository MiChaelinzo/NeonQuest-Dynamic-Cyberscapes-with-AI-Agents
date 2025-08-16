using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using NeonQuest.Core;
using NeonQuest.Configuration;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Manages dynamic fog density progression, atmospheric effects coordination,
    /// and smooth blending for simultaneous atmospheric changes
    /// </summary>
    public class FogEffectsEngine : MonoBehaviour, IProceduralGenerator
    {
        [Header("Configuration")]
        [SerializeField] private Vector2 fogDensityRange = new Vector2(0.1f, 0.8f);
        [SerializeField] private float transitionDuration = 3f;
        [SerializeField] private Color baseFogColor = Color.gray;
        [SerializeField] private float timeBasedProgressionSpeed = 0.1f;
        
        [Header("Coordination")]
        [SerializeField] private bool coordinateWithLighting = true;
        [SerializeField] private bool coordinateWithAudio = true;
        [SerializeField] private float coordinationDelay = 0.5f;
        
        [Header("Performance")]
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private int maxActiveEffects = 10;
        
        private Dictionary<string, FogEffect> activeEffects;
        private Dictionary<string, FogTransition> activeTransitions;
        private FogState currentFogState;
        private EnvironmentConfiguration config;
        private bool isActive = true;
        private float currentPerformanceCost = 0f;
        private float qualityLevel = 1f;
        private Coroutine updateCoroutine;
        private float gameStartTime;
        
        // Coordination with other systems
        private LightingEngine lightingEngine;
        private AudioEngine audioEngine;
        
        public float CurrentPerformanceCost => currentPerformanceCost;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            activeEffects = new Dictionary<string, FogEffect>();
            activeTransitions = new Dictionary<string, FogTransition>();
            gameStartTime = Time.time;
            
            InitializeFogState();
        }
        
        private void Start()
        {
            FindCoordinationSystems();
            
            if (isActive)
            {
                StartFogEngine();
            }
        }
        
        private void OnDestroy()
        {
            StopFogEngine();
        }
        
        public void Initialize(Dictionary<string, object> configData)
        {
            if (configData.TryGetValue("config", out var configObj) && configObj is EnvironmentConfiguration envConfig)
            {
                config = envConfig;
                fogDensityRange = config.FogDensityRange;
                transitionDuration = 1f / config.AtmosphereTransitionSpeed;
            }
            
            ApplyInitialFogSettings();
        }
        
        public async Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            if (!isActive) return null;
            
            // Fog effects don't generate GameObjects, but can create atmospheric effects
            if (generationParams.TryGetValue("fogEffect", out var effectObj) && effectObj is FogEffect effect)
            {
                RegisterFogEffect(effect);
            }
            
            await Task.CompletedTask;
            return null;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            if (!isActive) return;
            
            // Update time-based progression
            UpdateTimeBasedProgression(deltaTime);
            
            // Process environment state changes
            ProcessEnvironmentState(environmentState);
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            // Remove distant fog effects
            var effectsToRemove = new List<string>();
            
            foreach (var kvp in activeEffects)
            {
                var effect = kvp.Value;
                if (effect.HasPosition)
                {
                    float distance = Vector3.Distance(effect.Position, playerPosition);
                    if (distance > cleanupDistance)
                    {
                        effectsToRemove.Add(kvp.Key);
                    }
                }
            }
            
            foreach (var effectId in effectsToRemove)
            {
                RemoveFogEffect(effectId);
            }
            
            UpdatePerformanceCost();
        }
        
        public void SetQualityLevel(float qualityLevel)
        {
            this.qualityLevel = Mathf.Clamp01(qualityLevel);
            maxActiveEffects = Mathf.RoundToInt(10 * this.qualityLevel);
            updateInterval = Mathf.Lerp(0.4f, 0.1f, this.qualityLevel);
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            
            if (active && updateCoroutine == null)
            {
                StartFogEngine();
            }
            else if (!active && updateCoroutine != null)
            {
                StopFogEngine();
            }
        }
        
        private void InitializeFogState()
        {
            currentFogState = new FogState
            {
                Density = fogDensityRange.x,
                Color = baseFogColor,
                StartDistance = RenderSettings.fogStartDistance,
                EndDistance = RenderSettings.fogEndDistance,
                Mode = RenderSettings.fogMode
            };
            
            ApplyFogState(currentFogState);
        }
        
        private void FindCoordinationSystems()
        {
            if (coordinateWithLighting)
            {
                lightingEngine = FindObjectOfType<LightingEngine>();
            }
            
            if (coordinateWithAudio)
            {
                audioEngine = FindObjectOfType<AudioEngine>();
            }
        }
        
        private void StartFogEngine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutine = StartCoroutine(FogUpdateLoop());
        }
        
        private void StopFogEngine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
            
            // Stop all active transitions
            foreach (var transition in activeTransitions.Values)
            {
                if (transition.Coroutine != null)
                {
                    StopCoroutine(transition.Coroutine);
                }
            }
            activeTransitions.Clear();
        }
        
        private void ApplyInitialFogSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = baseFogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensityRange.x;
        }
        
        private IEnumerator FogUpdateLoop()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(updateInterval);
                
                var startTime = Time.realtimeSinceStartup;
                
                UpdateActiveEffects();
                UpdateActiveTransitions();
                BlendSimultaneousEffects();
                
                currentPerformanceCost = (Time.realtimeSinceStartup - startTime) * 1000f;
            }
        }
        
        private void UpdateTimeBasedProgression(float deltaTime)
        {
            if (timeBasedProgressionSpeed <= 0f) return;
            
            float gameTime = Time.time - gameStartTime;
            float progressionCycle = Mathf.Sin(gameTime * timeBasedProgressionSpeed) * 0.5f + 0.5f;
            
            // Create gradual fog density changes over time
            float targetDensity = Mathf.Lerp(fogDensityRange.x, fogDensityRange.y, progressionCycle);
            
            if (Mathf.Abs(currentFogState.Density - targetDensity) > 0.05f)
            {
                TransitionToFogDensity(targetDensity, transitionDuration);
            }
        }
        
        private void ProcessEnvironmentState(Dictionary<string, object> environmentState)
        {
            // Process zone changes
            if (environmentState.TryGetValue("currentZone", out var zoneObj) && zoneObj is string zone)
            {
                ProcessZoneChange(zone);
            }
            
            // Process gameplay events
            if (environmentState.TryGetValue("gameplayEvent", out var eventObj) && eventObj is string gameEvent)
            {
                ProcessGameplayEvent(gameEvent);
            }
        }
        
        private void ProcessZoneChange(string newZone)
        {
            // Different zones can have different atmospheric characteristics
            var zoneEffect = CreateZoneBasedFogEffect(newZone);
            if (zoneEffect != null)
            {
                RegisterFogEffect(zoneEffect);
                TriggerCoordinatedAtmosphericChange(newZone);
            }
        }
        
        private void ProcessGameplayEvent(string gameEvent)
        {
            switch (gameEvent.ToLower())
            {
                case "combat_start":
                    CreateIntenseFogEffect();
                    break;
                case "exploration_mode":
                    CreateSubtleFogEffect();
                    break;
                case "story_moment":
                    CreateDramaticFogEffect();
                    break;
            }
        }
        
        private FogEffect CreateZoneBasedFogEffect(string zone)
        {
            switch (zone.ToLower())
            {
                case "industrial":
                    return new FogEffect
                    {
                        Id = $"zone_{zone}",
                        TargetDensity = fogDensityRange.y * 0.8f,
                        TargetColor = new Color(0.6f, 0.6f, 0.7f, 1f),
                        Duration = transitionDuration,
                        Priority = 5,
                        EffectType = FogEffectType.Zone
                    };
                    
                case "residential":
                    return new FogEffect
                    {
                        Id = $"zone_{zone}",
                        TargetDensity = fogDensityRange.x * 1.2f,
                        TargetColor = new Color(0.8f, 0.8f, 0.9f, 1f),
                        Duration = transitionDuration,
                        Priority = 5,
                        EffectType = FogEffectType.Zone
                    };
                    
                case "underground":
                    return new FogEffect
                    {
                        Id = $"zone_{zone}",
                        TargetDensity = fogDensityRange.y,
                        TargetColor = new Color(0.4f, 0.5f, 0.6f, 1f),
                        Duration = transitionDuration,
                        Priority = 5,
                        EffectType = FogEffectType.Zone
                    };
                    
                default:
                    return null;
            }
        }
        
        private void CreateIntenseFogEffect()
        {
            var effect = new FogEffect
            {
                Id = "combat_intensity",
                TargetDensity = fogDensityRange.y * 0.9f,
                TargetColor = new Color(0.7f, 0.5f, 0.5f, 1f), // Reddish tint
                Duration = 2f,
                Priority = 8,
                EffectType = FogEffectType.Gameplay
            };
            
            RegisterFogEffect(effect);
        }
        
        private void CreateSubtleFogEffect()
        {
            var effect = new FogEffect
            {
                Id = "exploration_calm",
                TargetDensity = fogDensityRange.x * 1.1f,
                TargetColor = baseFogColor,
                Duration = 4f,
                Priority = 3,
                EffectType = FogEffectType.Gameplay
            };
            
            RegisterFogEffect(effect);
        }
        
        private void CreateDramaticFogEffect()
        {
            var effect = new FogEffect
            {
                Id = "story_drama",
                TargetDensity = fogDensityRange.y * 0.6f,
                TargetColor = new Color(0.9f, 0.9f, 1f, 1f), // Bright, ethereal
                Duration = 3f,
                Priority = 7,
                EffectType = FogEffectType.Gameplay
            };
            
            RegisterFogEffect(effect);
        }
        
        public void RegisterFogEffect(FogEffect effect)
        {
            if (activeEffects.Count >= maxActiveEffects) return;
            if (effect == null || string.IsNullOrEmpty(effect.Id)) return;
            
            activeEffects[effect.Id] = effect;
            StartFogTransition(effect);
        }
        
        public void RemoveFogEffect(string effectId)
        {
            if (activeEffects.ContainsKey(effectId))
            {
                activeEffects.Remove(effectId);
            }
            
            if (activeTransitions.ContainsKey(effectId))
            {
                var transition = activeTransitions[effectId];
                if (transition.Coroutine != null)
                {
                    StopCoroutine(transition.Coroutine);
                }
                activeTransitions.Remove(effectId);
            }
        }
        
        private void StartFogTransition(FogEffect effect)
        {
            string transitionId = effect.Id;
            
            // Stop existing transition if any
            if (activeTransitions.ContainsKey(transitionId))
            {
                var existingTransition = activeTransitions[transitionId];
                if (existingTransition.Coroutine != null)
                {
                    StopCoroutine(existingTransition.Coroutine);
                }
            }
            
            var transition = new FogTransition
            {
                EffectId = effect.Id,
                StartState = new FogState(currentFogState),
                TargetState = new FogState
                {
                    Density = effect.TargetDensity,
                    Color = effect.TargetColor,
                    StartDistance = currentFogState.StartDistance,
                    EndDistance = currentFogState.EndDistance,
                    Mode = currentFogState.Mode
                },
                Duration = effect.Duration,
                StartTime = Time.time,
                Priority = effect.Priority
            };
            
            transition.Coroutine = StartCoroutine(ExecuteFogTransition(transition));
            activeTransitions[transitionId] = transition;
        }
        
        private IEnumerator ExecuteFogTransition(FogTransition transition)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < transition.Duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / transition.Duration);
                
                // Apply easing curve for smooth transitions
                float easedProgress = EaseInOutCubic(progress);
                
                // Interpolate fog properties
                var blendedState = BlendFogStates(transition.StartState, transition.TargetState, easedProgress);
                ApplyFogState(blendedState);
                
                yield return null;
            }
            
            // Ensure final state is applied
            ApplyFogState(transition.TargetState);
            currentFogState = new FogState(transition.TargetState);
            
            // Clean up transition
            if (activeTransitions.ContainsKey(transition.EffectId))
            {
                activeTransitions.Remove(transition.EffectId);
            }
        }
        
        private void UpdateActiveEffects()
        {
            var effectsToRemove = new List<string>();
            
            foreach (var kvp in activeEffects)
            {
                var effect = kvp.Value;
                effect.ElapsedTime += Time.deltaTime;
                
                // Remove expired effects
                if (effect.Duration > 0 && effect.ElapsedTime >= effect.Duration)
                {
                    effectsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var effectId in effectsToRemove)
            {
                RemoveFogEffect(effectId);
            }
        }
        
        private void UpdateActiveTransitions()
        {
            // Transitions are managed by their coroutines
            // This method can be used for additional transition logic if needed
        }
        
        private void BlendSimultaneousEffects()
        {
            if (activeTransitions.Count <= 1) return;
            
            // When multiple effects are active, blend them based on priority
            var sortedTransitions = activeTransitions.Values
                .OrderByDescending(t => t.Priority)
                .ToList();
            
            if (sortedTransitions.Count > 1)
            {
                var primaryTransition = sortedTransitions[0];
                var secondaryTransition = sortedTransitions[1];
                
                // Blend the two highest priority effects
                float blendFactor = 0.7f; // Primary effect gets more weight
                var blendedState = BlendFogStates(primaryTransition.TargetState, secondaryTransition.TargetState, blendFactor);
                
                // Apply the blended state
                ApplyFogState(blendedState);
                currentFogState = new FogState(blendedState);
            }
        }
        
        private FogState BlendFogStates(FogState stateA, FogState stateB, float t)
        {
            return new FogState
            {
                Density = Mathf.Lerp(stateA.Density, stateB.Density, t),
                Color = Color.Lerp(stateA.Color, stateB.Color, t),
                StartDistance = Mathf.Lerp(stateA.StartDistance, stateB.StartDistance, t),
                EndDistance = Mathf.Lerp(stateA.EndDistance, stateB.EndDistance, t),
                Mode = stateA.Mode // Mode doesn't interpolate
            };
        }
        
        private void ApplyFogState(FogState state)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = state.Color;
            RenderSettings.fogMode = state.Mode;
            
            if (state.Mode == FogMode.Linear)
            {
                RenderSettings.fogStartDistance = state.StartDistance;
                RenderSettings.fogEndDistance = state.EndDistance;
            }
            else
            {
                RenderSettings.fogDensity = state.Density;
            }
        }
        
        public void TransitionToFogDensity(float targetDensity, float duration)
        {
            var effect = new FogEffect
            {
                Id = "density_transition",
                TargetDensity = targetDensity,
                TargetColor = currentFogState.Color,
                Duration = duration,
                Priority = 4,
                EffectType = FogEffectType.Transition
            };
            
            RegisterFogEffect(effect);
        }
        
        public void TriggerCoordinatedAtmosphericChange(string context)
        {
            StartCoroutine(ExecuteCoordinatedChange(context));
        }
        
        private IEnumerator ExecuteCoordinatedChange(string context)
        {
            // Coordinate with lighting system
            if (coordinateWithLighting && lightingEngine != null)
            {
                // Trigger lighting changes first
                TriggerLightingCoordination(context);
                yield return new WaitForSeconds(coordinationDelay);
            }
            
            // Coordinate with audio system
            if (coordinateWithAudio && audioEngine != null)
            {
                TriggerAudioCoordination(context);
                yield return new WaitForSeconds(coordinationDelay);
            }
        }
        
        private void TriggerLightingCoordination(string context)
        {
            // Trigger coordinated lighting effects based on context
            switch (context.ToLower())
            {
                case "industrial":
                    lightingEngine?.TriggerPulseEffect(Vector3.zero, 50f, 1.2f);
                    break;
                case "underground":
                    lightingEngine?.TriggerPulseEffect(Vector3.zero, 30f, 0.8f);
                    break;
            }
        }
        
        private void TriggerAudioCoordination(string context)
        {
            // Trigger coordinated audio changes based on context
            switch (context.ToLower())
            {
                case "industrial":
                    audioEngine?.ModifyAmbientVolume(0.8f);
                    break;
                case "residential":
                    audioEngine?.ModifyAmbientVolume(0.4f);
                    break;
                case "underground":
                    audioEngine?.ModifyAmbientVolume(0.6f);
                    break;
            }
        }
        
        private float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
        
        private void UpdatePerformanceCost()
        {
            float baseCost = activeEffects.Count / (float)maxActiveEffects;
            float transitionCost = activeTransitions.Count * 0.15f;
            currentPerformanceCost = Mathf.Clamp01(baseCost + transitionCost);
        }
        
        /// <summary>
        /// Gets all active fog effects for debugging
        /// </summary>
        public IReadOnlyDictionary<string, FogEffect> GetActiveEffects()
        {
            return activeEffects;
        }
        
        /// <summary>
        /// Gets active transitions for debugging
        /// </summary>
        public IReadOnlyDictionary<string, FogTransition> GetActiveTransitions()
        {
            return activeTransitions;
        }
        
        /// <summary>
        /// Gets current fog state
        /// </summary>
        public FogState GetCurrentFogState()
        {
            return new FogState(currentFogState);
        }
        
        /// <summary>
        /// Manually create a custom fog effect
        /// </summary>
        public void CreateCustomFogEffect(string effectId, float targetDensity, Color targetColor, float duration, int priority = 5)
        {
            var effect = new FogEffect
            {
                Id = effectId,
                TargetDensity = targetDensity,
                TargetColor = targetColor,
                Duration = duration,
                Priority = priority,
                EffectType = FogEffectType.Custom
            };
            
            RegisterFogEffect(effect);
        }
        
        public Dictionary<string, object> GetDiagnosticInfo()
        {
            return new Dictionary<string, object>
            {
                ["ActiveEffects"] = activeEffects.Count,
                ["ActiveTransitions"] = activeTransitions.Count,
                ["CurrentDensity"] = currentFogState.Density,
                ["CurrentColor"] = currentFogState.Color,
                ["PerformanceCost"] = currentPerformanceCost,
                ["QualityLevel"] = qualityLevel,
                ["IsActive"] = isActive,
                ["CoordinateWithLighting"] = coordinateWithLighting,
                ["CoordinateWithAudio"] = coordinateWithAudio
            };
        }
    }
}    //
/ <summary>
    /// Represents the current state of fog rendering
    /// </summary>
    [System.Serializable]
    public class FogState
    {
        public float Density;
        public Color Color;
        public float StartDistance;
        public float EndDistance;
        public FogMode Mode;
        
        public FogState()
        {
            Density = 0.1f;
            Color = Color.gray;
            StartDistance = 0f;
            EndDistance = 300f;
            Mode = FogMode.ExponentialSquared;
        }
        
        public FogState(FogState other)
        {
            Density = other.Density;
            Color = other.Color;
            StartDistance = other.StartDistance;
            EndDistance = other.EndDistance;
            Mode = other.Mode;
        }
    }
    
    /// <summary>
    /// Represents a fog effect that can be applied to the environment
    /// </summary>
    [System.Serializable]
    public class FogEffect
    {
        public string Id;
        public float TargetDensity;
        public Color TargetColor;
        public float Duration;
        public int Priority;
        public FogEffectType EffectType;
        public Vector3 Position;
        public bool HasPosition;
        public float ElapsedTime;
        
        public FogEffect()
        {
            Id = "";
            TargetDensity = 0.1f;
            TargetColor = Color.gray;
            Duration = 1f;
            Priority = 5;
            EffectType = FogEffectType.Generic;
            Position = Vector3.zero;
            HasPosition = false;
            ElapsedTime = 0f;
        }
    }
    
    /// <summary>
    /// Represents an active fog transition
    /// </summary>
    [System.Serializable]
    public class FogTransition
    {
        public string EffectId;
        public FogState StartState;
        public FogState TargetState;
        public float Duration;
        public float StartTime;
        public int Priority;
        public Coroutine Coroutine;
        
        public float Progress => Mathf.Clamp01((Time.time - StartTime) / Duration);
    }
    
    /// <summary>
    /// Types of fog effects
    /// </summary>
    public enum FogEffectType
    {
        Generic,
        Zone,
        Gameplay,
        Transition,
        Custom,
        TimeProgression
    }
}