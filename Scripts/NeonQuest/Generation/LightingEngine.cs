using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using NeonQuest.Core;
using NeonQuest.Configuration;

namespace NeonQuest.Generation
{
    /// <summary>
    /// Manages dynamic lighting responses including neon sign proximity detection,
    /// brightness adjustments, and movement-based lighting effects
    /// </summary>
    public class LightingEngine : MonoBehaviour, IProceduralGenerator
    {
        [Header("Configuration")]
        [SerializeField] private float neonResponseDistance = 5f;
        [SerializeField] private Vector2 brightnessMultiplierRange = new Vector2(0.5f, 2.0f);
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private float movementSurgeThreshold = 5f;
        [SerializeField] private float pulseIntensity = 1.5f;
        
        [Header("Performance")]
        [SerializeField] private int maxTrackedLights = 50;
        [SerializeField] private float updateInterval = 0.1f;
        
        private Dictionary<string, NeonLight> trackedLights;
        private Dictionary<string, LightingTransition> activeTransitions;
        private Vector3 lastPlayerPosition;
        private float lastPlayerSpeed;
        private EnvironmentConfiguration config;
        private bool isActive = true;
        private float currentPerformanceCost = 0f;
        private float qualityLevel = 1f;
        private Coroutine updateCoroutine;
        
        public float CurrentPerformanceCost => currentPerformanceCost;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            trackedLights = new Dictionary<string, NeonLight>();
            activeTransitions = new Dictionary<string, LightingTransition>();
            lastPlayerPosition = Vector3.zero;
        }
        
        private void Start()
        {
            if (isActive)
            {
                updateCoroutine = StartCoroutine(UpdateLightingCoroutine());
            }
        }
        
        private void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        } 
       
        public void Initialize(Dictionary<string, object> configData)
        {
            if (configData.TryGetValue("config", out var configObj) && configObj is EnvironmentConfiguration envConfig)
            {
                config = envConfig;
                neonResponseDistance = config.NeonResponseDistance;
                brightnessMultiplierRange = config.BrightnessMultiplierRange;
                transitionDuration = config.LightingTransitionDuration;
            }
            
            // Discover existing lights in the scene
            DiscoverSceneLights();
        }
        
        public async Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            if (!isActive) return null;
            
            // This engine doesn't generate new objects, but can modify existing lighting
            if (generationParams.TryGetValue("lightObject", out var lightObj) && lightObj is GameObject lightGameObject)
            {
                RegisterLight(lightGameObject);
                return lightGameObject;
            }
            
            return null;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            if (!isActive) return;
            
            Vector3 playerPosition = Vector3.zero;
            float playerSpeed = 0f;
            
            if (environmentState.TryGetValue("playerPosition", out var posObj) && posObj is Vector3 pos)
                playerPosition = pos;
            if (environmentState.TryGetValue("playerSpeed", out var speedObj) && speedObj is float speed)
                playerSpeed = speed;
            
            // Update player tracking
            lastPlayerSpeed = playerSpeed;
            lastPlayerPosition = playerPosition;
            
            // Process movement-based effects
            ProcessMovementEffects(playerPosition, playerSpeed);
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            var lightsToRemove = new List<string>();
            
            foreach (var kvp in trackedLights)
            {
                if (kvp.Value.Light != null)
                {
                    float distance = Vector3.Distance(kvp.Value.Light.transform.position, playerPosition);
                    if (distance > cleanupDistance)
                    {
                        lightsToRemove.Add(kvp.Key);
                    }
                }
                else
                {
                    // Light was destroyed, remove from tracking
                    lightsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var lightId in lightsToRemove)
            {
                UnregisterLight(lightId);
            }
            
            UpdatePerformanceCost();
        }    
    
        public void SetQualityLevel(float qualityLevel)
        {
            this.qualityLevel = Mathf.Clamp01(qualityLevel);
            maxTrackedLights = Mathf.RoundToInt(50 * this.qualityLevel);
            updateInterval = Mathf.Lerp(0.2f, 0.05f, this.qualityLevel);
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            
            if (active && updateCoroutine == null)
            {
                updateCoroutine = StartCoroutine(UpdateLightingCoroutine());
            }
            else if (!active && updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }
        
        private void DiscoverSceneLights()
        {
            var allLights = FindObjectsOfType<Light>();
            foreach (var light in allLights)
            {
                if (IsNeonLight(light))
                {
                    RegisterLight(light.gameObject);
                }
            }
        }
        
        private bool IsNeonLight(Light light)
        {
            // Check if this is a neon light based on naming convention or tags
            return light.name.ToLower().Contains("neon") || 
                   light.gameObject.CompareTag("NeonLight") ||
                   light.color.r > 0.8f || light.color.g > 0.8f || light.color.b > 0.8f;
        }
        
        private void RegisterLight(GameObject lightObject)
        {
            if (trackedLights.Count >= maxTrackedLights) return;
            
            var light = lightObject.GetComponent<Light>();
            if (light == null) return;
            
            string lightId = lightObject.GetInstanceID().ToString();
            if (trackedLights.ContainsKey(lightId)) return;
            
            var neonLight = new NeonLight
            {
                Light = light,
                OriginalIntensity = light.intensity,
                OriginalColor = light.color,
                OriginalRange = light.range,
                LastPlayerDistance = float.MaxValue,
                IsResponding = false
            };
            
            trackedLights[lightId] = neonLight;
        }        

        private void UnregisterLight(string lightId)
        {
            if (trackedLights.TryGetValue(lightId, out var neonLight))
            {
                // Restore original values if light still exists
                if (neonLight.Light != null)
                {
                    neonLight.Light.intensity = neonLight.OriginalIntensity;
                    neonLight.Light.color = neonLight.OriginalColor;
                    neonLight.Light.range = neonLight.OriginalRange;
                }
                
                trackedLights.Remove(lightId);
            }
            
            // Remove any active transitions
            if (activeTransitions.ContainsKey(lightId))
            {
                activeTransitions.Remove(lightId);
            }
        }
        
        private IEnumerator UpdateLightingCoroutine()
        {
            while (isActive)
            {
                UpdateProximityEffects();
                UpdateActiveTransitions();
                UpdatePerformanceCost();
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void UpdateProximityEffects()
        {
            foreach (var kvp in trackedLights)
            {
                var lightId = kvp.Key;
                var neonLight = kvp.Value;
                
                if (neonLight.Light == null) continue;
                
                float distance = Vector3.Distance(neonLight.Light.transform.position, lastPlayerPosition);
                bool shouldRespond = distance <= neonResponseDistance;
                
                if (shouldRespond && !neonLight.IsResponding)
                {
                    // Start proximity response
                    StartProximityResponse(lightId, neonLight, distance);
                }
                else if (!shouldRespond && neonLight.IsResponding)
                {
                    // End proximity response
                    EndProximityResponse(lightId, neonLight);
                }
                else if (shouldRespond && neonLight.IsResponding)
                {
                    // Update ongoing response
                    UpdateProximityResponse(lightId, neonLight, distance);
                }
                
                neonLight.LastPlayerDistance = distance;
            }
        }  
      
        private void StartProximityResponse(string lightId, NeonLight neonLight, float distance)
        {
            neonLight.IsResponding = true;
            
            // Calculate target values based on distance
            float proximityFactor = 1f - (distance / neonResponseDistance);
            float targetIntensity = Mathf.Lerp(neonLight.OriginalIntensity, 
                neonLight.OriginalIntensity * brightnessMultiplierRange.y, proximityFactor);
            
            // Create transition
            var transition = new LightingTransition
            {
                LightId = lightId,
                StartIntensity = neonLight.Light.intensity,
                TargetIntensity = targetIntensity,
                StartColor = neonLight.Light.color,
                TargetColor = Color.Lerp(neonLight.OriginalColor, Color.white, proximityFactor * 0.3f),
                Duration = transitionDuration,
                ElapsedTime = 0f,
                TransitionType = LightingTransitionType.ProximityResponse
            };
            
            activeTransitions[lightId] = transition;
        }
        
        private void EndProximityResponse(string lightId, NeonLight neonLight)
        {
            neonLight.IsResponding = false;
            
            // Create transition back to original values
            var transition = new LightingTransition
            {
                LightId = lightId,
                StartIntensity = neonLight.Light.intensity,
                TargetIntensity = neonLight.OriginalIntensity,
                StartColor = neonLight.Light.color,
                TargetColor = neonLight.OriginalColor,
                Duration = transitionDuration,
                ElapsedTime = 0f,
                TransitionType = LightingTransitionType.ProximityEnd
            };
            
            activeTransitions[lightId] = transition;
        }
        
        private void UpdateProximityResponse(string lightId, NeonLight neonLight, float distance)
        {
            // Update target values based on current distance
            float proximityFactor = 1f - (distance / neonResponseDistance);
            float targetIntensity = Mathf.Lerp(neonLight.OriginalIntensity, 
                neonLight.OriginalIntensity * brightnessMultiplierRange.y, proximityFactor);
            
            // If there's an active transition, update its target
            if (activeTransitions.TryGetValue(lightId, out var transition))
            {
                transition.TargetIntensity = targetIntensity;
                transition.TargetColor = Color.Lerp(neonLight.OriginalColor, Color.white, proximityFactor * 0.3f);
            }
            else
            {
                // Apply directly if no transition is active
                neonLight.Light.intensity = targetIntensity;
                neonLight.Light.color = Color.Lerp(neonLight.OriginalColor, Color.white, proximityFactor * 0.3f);
            }
        }        

        private void ProcessMovementEffects(Vector3 playerPosition, float playerSpeed)
        {
            if (playerSpeed > movementSurgeThreshold)
            {
                // Create surge effect for nearby lights
                var nearbyLights = trackedLights.Values
                    .Where(nl => nl.Light != null && 
                                Vector3.Distance(nl.Light.transform.position, playerPosition) <= neonResponseDistance * 2f)
                    .ToList();
                
                foreach (var neonLight in nearbyLights)
                {
                    CreateSurgeEffect(neonLight);
                }
            }
        }
        
        private void CreateSurgeEffect(NeonLight neonLight)
        {
            string lightId = neonLight.Light.GetInstanceID().ToString();
            
            // Don't create surge if already surging
            if (activeTransitions.ContainsKey(lightId) && 
                activeTransitions[lightId].TransitionType == LightingTransitionType.Surge)
                return;
            
            var transition = new LightingTransition
            {
                LightId = lightId,
                StartIntensity = neonLight.Light.intensity,
                TargetIntensity = neonLight.OriginalIntensity * pulseIntensity,
                StartColor = neonLight.Light.color,
                TargetColor = neonLight.Light.color,
                Duration = 0.3f,
                ElapsedTime = 0f,
                TransitionType = LightingTransitionType.Surge,
                IsReversing = false
            };
            
            activeTransitions[lightId] = transition;
        }
        
        private void UpdateActiveTransitions()
        {
            var transitionsToRemove = new List<string>();
            
            foreach (var kvp in activeTransitions)
            {
                var lightId = kvp.Key;
                var transition = kvp.Value;
                
                if (!trackedLights.TryGetValue(lightId, out var neonLight) || neonLight.Light == null)
                {
                    transitionsToRemove.Add(lightId);
                    continue;
                }
                
                transition.ElapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(transition.ElapsedTime / transition.Duration);
                
                // Apply easing curve
                float easedProgress = EaseInOutCubic(progress);
                
                // Update light properties
                neonLight.Light.intensity = Mathf.Lerp(transition.StartIntensity, transition.TargetIntensity, easedProgress);
                neonLight.Light.color = Color.Lerp(transition.StartColor, transition.TargetColor, easedProgress);
                
                // Handle transition completion
                if (progress >= 1f)
                {
                    HandleTransitionComplete(lightId, transition, neonLight);
                    transitionsToRemove.Add(lightId);
                }
            }
            
            foreach (var lightId in transitionsToRemove)
            {
                activeTransitions.Remove(lightId);
            }
        }        

        private void HandleTransitionComplete(string lightId, LightingTransition transition, NeonLight neonLight)
        {
            switch (transition.TransitionType)
            {
                case LightingTransitionType.Surge:
                    if (!transition.IsReversing)
                    {
                        // Start reverse transition
                        var reverseTransition = new LightingTransition
                        {
                            LightId = lightId,
                            StartIntensity = transition.TargetIntensity,
                            TargetIntensity = neonLight.OriginalIntensity,
                            StartColor = transition.TargetColor,
                            TargetColor = neonLight.OriginalColor,
                            Duration = 0.5f,
                            ElapsedTime = 0f,
                            TransitionType = LightingTransitionType.Surge,
                            IsReversing = true
                        };
                        activeTransitions[lightId] = reverseTransition;
                    }
                    break;
                    
                case LightingTransitionType.ProximityResponse:
                case LightingTransitionType.ProximityEnd:
                    // Transition complete, no further action needed
                    break;
            }
        }
        
        private float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
        
        private void UpdatePerformanceCost()
        {
            float baseCost = trackedLights.Count / (float)maxTrackedLights;
            float transitionCost = activeTransitions.Count * 0.1f;
            currentPerformanceCost = Mathf.Clamp01(baseCost + transitionCost);
        }
        
        /// <summary>
        /// Gets all tracked lights for debugging or external access
        /// </summary>
        public IReadOnlyDictionary<string, NeonLight> GetTrackedLights()
        {
            return trackedLights;
        }
        
        /// <summary>
        /// Gets active transitions for debugging
        /// </summary>
        public IReadOnlyDictionary<string, LightingTransition> GetActiveTransitions()
        {
            return activeTransitions;
        }
        
        /// <summary>
        /// Manually trigger a pulse effect on all nearby lights
        /// </summary>
        public void TriggerPulseEffect(Vector3 center, float radius, float intensity = 1.5f)
        {
            var nearbyLights = trackedLights.Values
                .Where(nl => nl.Light != null && 
                            Vector3.Distance(nl.Light.transform.position, center) <= radius)
                .ToList();
            
            foreach (var neonLight in nearbyLights)
            {
                CreatePulseEffect(neonLight, intensity);
            }
        }     
   
        private void CreatePulseEffect(NeonLight neonLight, float intensity)
        {
            string lightId = neonLight.Light.GetInstanceID().ToString();
            
            var transition = new LightingTransition
            {
                LightId = lightId,
                StartIntensity = neonLight.Light.intensity,
                TargetIntensity = neonLight.OriginalIntensity * intensity,
                StartColor = neonLight.Light.color,
                TargetColor = Color.Lerp(neonLight.OriginalColor, Color.white, 0.2f),
                Duration = 0.2f,
                ElapsedTime = 0f,
                TransitionType = LightingTransitionType.Pulse,
                IsReversing = false
            };
            
            activeTransitions[lightId] = transition;
        }
    }
    
    /// <summary>
    /// Represents a tracked neon light with its original properties
    /// </summary>
    [System.Serializable]
    public class NeonLight
    {
        public Light Light;
        public float OriginalIntensity;
        public Color OriginalColor;
        public float OriginalRange;
        public float LastPlayerDistance;
        public bool IsResponding;
        public float LastResponseTime;
    }
    
    /// <summary>
    /// Represents an active lighting transition
    /// </summary>
    [System.Serializable]
    public class LightingTransition
    {
        public string LightId;
        public float StartIntensity;
        public float TargetIntensity;
        public Color StartColor;
        public Color TargetColor;
        public float Duration;
        public float ElapsedTime;
        public LightingTransitionType TransitionType;
        public bool IsReversing;
    }
    
    /// <summary>
    /// Types of lighting transitions
    /// </summary>
    public enum LightingTransitionType
    {
        ProximityResponse,
        ProximityEnd,
        Surge,
        Pulse,
        Flicker,
        Fade
    }
}