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
    /// Manages dynamic ambient sound zones, audio transitions, and spatial audio positioning
    /// for procedurally generated content
    /// </summary>
    public class AudioEngine : MonoBehaviour, IProceduralGenerator
    {
        [Header("Configuration")]
        [SerializeField] private float zoneTransitionDistance = 10f;
        [SerializeField] private float transitionDuration = 3f;
        [SerializeField] private Vector2 ambientVolumeRange = new Vector2(0.3f, 0.9f);
        [SerializeField] private float spatialAudioRange = 20f;
        [SerializeField] private int maxAudioSources = 20;
        
        [Header("Performance")]
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private float cullingDistance = 50f;
        
        private Dictionary<string, AudioZone> audioZones;
        private Dictionary<string, AudioTransition> activeTransitions;
        private Dictionary<string, AudioSource> spatialAudioSources;
        private AudioSource ambientAudioSource;
        private string currentZone;
        private Vector3 lastPlayerPosition;
        private EnvironmentConfiguration config;
        private bool isActive = true;
        private float currentPerformanceCost = 0f;
        private float qualityLevel = 1f;
        private Coroutine updateCoroutine;
        
        public float CurrentPerformanceCost => currentPerformanceCost;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            audioZones = new Dictionary<string, AudioZone>();
            activeTransitions = new Dictionary<string, AudioTransition>();
            spatialAudioSources = new Dictionary<string, AudioSource>();
            lastPlayerPosition = Vector3.zero;
            currentZone = "default";
            
            // Create main ambient audio source
            CreateAmbientAudioSource();
        }
        
        private void Start()
        {
            if (isActive)
            {
                StartAudioEngine();
            }
        }
        
        private void OnDestroy()
        {
            StopAudioEngine();
        }
        
        public void Initialize(Dictionary<string, object> config)
        {
            // Convert dictionary config to EnvironmentConfiguration if needed
            // For now, we'll use the existing configuration system
            LoadAudioConfiguration();
        }
        
        public void Initialize(EnvironmentConfiguration configuration)
        {
            config = configuration;
            LoadAudioConfiguration();
        }
        
        public async Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams)
        {
            // Audio generation doesn't create GameObjects, so return null
            // This method is required by interface but not applicable to audio
            await Task.CompletedTask;
            return null;
        }
        
        public void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState)
        {
            // Update audio based on environment state
            if (environmentState.ContainsKey("PlayerPosition"))
            {
                lastPlayerPosition = (Vector3)environmentState["PlayerPosition"];
            }
        }
        
        public void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition)
        {
            // Clean up distant spatial audio sources
            lastPlayerPosition = playerPosition;
            CullDistantAudioSources();
        }
        
        public void StartGeneration()
        {
            if (!isActive) return;
            StartAudioEngine();
        }
        
        public void StopGeneration()
        {
            StopAudioEngine();
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            if (active)
            {
                StartAudioEngine();
            }
            else
            {
                StopAudioEngine();
            }
        }
        
        public void SetQualityLevel(float quality)
        {
            qualityLevel = Mathf.Clamp01(quality);
            AdjustQualitySettings();
        }
        
        private void StartAudioEngine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutine = StartCoroutine(AudioUpdateLoop());
        }
        
        private void StopAudioEngine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
            
            // Stop all audio transitions
            foreach (var transition in activeTransitions.Values)
            {
                if (transition.Coroutine != null)
                {
                    StopCoroutine(transition.Coroutine);
                }
            }
            activeTransitions.Clear();
        }
        
        private void CreateAmbientAudioSource()
        {
            GameObject ambientObject = new GameObject("AmbientAudio");
            ambientObject.transform.SetParent(transform);
            ambientAudioSource = ambientObject.AddComponent<AudioSource>();
            ambientAudioSource.loop = true;
            ambientAudioSource.spatialBlend = 0f; // 2D audio
            ambientAudioSource.volume = ambientVolumeRange.x;
        }
        
        private void LoadAudioConfiguration()
        {
            if (config == null) return;
            
            // Update settings from configuration
            ambientVolumeRange = config.AmbientVolumeRange;
            transitionDuration = 1f / config.AtmosphereTransitionSpeed;
        }
        
        private IEnumerator AudioUpdateLoop()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(updateInterval);
                
                var startTime = Time.realtimeSinceStartup;
                
                UpdatePlayerPosition();
                UpdateAudioZones();
                UpdateSpatialAudio();
                CullDistantAudioSources();
                
                currentPerformanceCost = (Time.realtimeSinceStartup - startTime) * 1000f;
            }
        }
        
        private void UpdatePlayerPosition()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                lastPlayerPosition = player.transform.position;
            }
        }
        
        private void UpdateAudioZones()
        {
            string nearestZone = FindNearestAudioZone();
            
            if (nearestZone != currentZone && !string.IsNullOrEmpty(nearestZone))
            {
                TransitionToZone(nearestZone);
            }
        }
        
        private string FindNearestAudioZone()
        {
            string nearestZone = currentZone;
            float nearestDistance = float.MaxValue;
            
            foreach (var zone in audioZones)
            {
                float distance = Vector3.Distance(lastPlayerPosition, zone.Value.Position);
                if (distance < zone.Value.Radius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestZone = zone.Key;
                }
            }
            
            return nearestZone;
        }
        
        private void UpdateSpatialAudio()
        {
            var sourcesToRemove = new List<string>();
            
            foreach (var kvp in spatialAudioSources)
            {
                var audioSource = kvp.Value;
                if (audioSource == null)
                {
                    sourcesToRemove.Add(kvp.Key);
                    continue;
                }
                
                // Update spatial audio based on distance
                float distance = Vector3.Distance(lastPlayerPosition, audioSource.transform.position);
                if (distance > spatialAudioRange)
                {
                    audioSource.volume = 0f;
                }
                else
                {
                    float volumeMultiplier = 1f - (distance / spatialAudioRange);
                    audioSource.volume = volumeMultiplier * qualityLevel;
                }
            }
            
            // Clean up null references
            foreach (var key in sourcesToRemove)
            {
                spatialAudioSources.Remove(key);
            }
        }
        
        private void CullDistantAudioSources()
        {
            var sourcesToCull = new List<string>();
            
            foreach (var kvp in spatialAudioSources)
            {
                var audioSource = kvp.Value;
                if (audioSource != null)
                {
                    float distance = Vector3.Distance(lastPlayerPosition, audioSource.transform.position);
                    if (distance > cullingDistance)
                    {
                        sourcesToCull.Add(kvp.Key);
                    }
                }
            }
            
            foreach (var key in sourcesToCull)
            {
                if (spatialAudioSources.ContainsKey(key))
                {
                    var audioSource = spatialAudioSources[key];
                    if (audioSource != null)
                    {
                        Destroy(audioSource.gameObject);
                    }
                    spatialAudioSources.Remove(key);
                }
            }
        }
        
        public void RegisterAudioZone(string zoneId, Vector3 position, float radius, AudioClip ambientClip)
        {
            if (audioZones.ContainsKey(zoneId))
            {
                audioZones[zoneId].AmbientClip = ambientClip;
                audioZones[zoneId].Position = position;
                audioZones[zoneId].Radius = radius;
            }
            else
            {
                audioZones[zoneId] = new AudioZone
                {
                    Id = zoneId,
                    Position = position,
                    Radius = radius,
                    AmbientClip = ambientClip,
                    Volume = ambientVolumeRange.x
                };
            }
        }
        
        public void UnregisterAudioZone(string zoneId)
        {
            if (audioZones.ContainsKey(zoneId))
            {
                audioZones.Remove(zoneId);
            }
        }
        
        public void TransitionToZone(string zoneId)
        {
            if (!audioZones.ContainsKey(zoneId) || zoneId == currentZone) return;
            
            var targetZone = audioZones[zoneId];
            string transitionId = $"{currentZone}_to_{zoneId}";
            
            // Stop existing transition if any
            if (activeTransitions.ContainsKey(transitionId))
            {
                if (activeTransitions[transitionId].Coroutine != null)
                {
                    StopCoroutine(activeTransitions[transitionId].Coroutine);
                }
                activeTransitions.Remove(transitionId);
            }
            
            // Start new transition
            var transition = new AudioTransition
            {
                FromZone = currentZone,
                ToZone = zoneId,
                StartTime = Time.time,
                Duration = transitionDuration
            };
            
            transition.Coroutine = StartCoroutine(ExecuteAudioTransition(transition));
            activeTransitions[transitionId] = transition;
            
            currentZone = zoneId;
        }
        
        private IEnumerator ExecuteAudioTransition(AudioTransition transition)
        {
            var targetZone = audioZones[transition.ToZone];
            var originalClip = ambientAudioSource.clip;
            var originalVolume = ambientAudioSource.volume;
            
            float elapsedTime = 0f;
            
            // Fade out current audio
            while (elapsedTime < transition.Duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float fadeProgress = elapsedTime / (transition.Duration * 0.5f);
                ambientAudioSource.volume = Mathf.Lerp(originalVolume, 0f, fadeProgress);
                yield return null;
            }
            
            // Switch to new audio clip
            ambientAudioSource.clip = targetZone.AmbientClip;
            if (targetZone.AmbientClip != null)
            {
                ambientAudioSource.Play();
            }
            
            // Fade in new audio
            elapsedTime = 0f;
            while (elapsedTime < transition.Duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float fadeProgress = elapsedTime / (transition.Duration * 0.5f);
                float targetVolume = Mathf.Lerp(ambientVolumeRange.x, ambientVolumeRange.y, targetZone.Volume);
                ambientAudioSource.volume = Mathf.Lerp(0f, targetVolume, fadeProgress);
                yield return null;
            }
            
            // Clean up transition
            string transitionId = $"{transition.FromZone}_to_{transition.ToZone}";
            if (activeTransitions.ContainsKey(transitionId))
            {
                activeTransitions.Remove(transitionId);
            }
        }
        
        public void CreateSpatialAudioSource(string sourceId, Vector3 position, AudioClip clip, bool loop = true)
        {
            if (spatialAudioSources.Count >= maxAudioSources) return;
            
            if (spatialAudioSources.ContainsKey(sourceId))
            {
                // Update existing source
                var existingSource = spatialAudioSources[sourceId];
                if (existingSource != null)
                {
                    existingSource.transform.position = position;
                    existingSource.clip = clip;
                    existingSource.loop = loop;
                    if (!existingSource.isPlaying && clip != null)
                    {
                        existingSource.Play();
                    }
                }
                return;
            }
            
            // Create new spatial audio source
            GameObject audioObject = new GameObject($"SpatialAudio_{sourceId}");
            audioObject.transform.position = position;
            audioObject.transform.SetParent(transform);
            
            var audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = spatialAudioRange;
            audioSource.volume = qualityLevel;
            
            if (clip != null)
            {
                audioSource.Play();
            }
            
            spatialAudioSources[sourceId] = audioSource;
        }
        
        public void RemoveSpatialAudioSource(string sourceId)
        {
            if (spatialAudioSources.ContainsKey(sourceId))
            {
                var audioSource = spatialAudioSources[sourceId];
                if (audioSource != null)
                {
                    Destroy(audioSource.gameObject);
                }
                spatialAudioSources.Remove(sourceId);
            }
        }
        
        public void ModifyAmbientVolume(float intensity)
        {
            if (ambientAudioSource != null)
            {
                float targetVolume = Mathf.Lerp(ambientVolumeRange.x, ambientVolumeRange.y, intensity);
                StartCoroutine(SmoothVolumeTransition(ambientAudioSource, targetVolume, 1f));
            }
        }
        
        private IEnumerator SmoothVolumeTransition(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                source.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                yield return null;
            }
            
            source.volume = targetVolume;
        }
        
        private void AdjustQualitySettings()
        {
            // Adjust audio quality based on performance
            foreach (var audioSource in spatialAudioSources.Values)
            {
                if (audioSource != null)
                {
                    audioSource.volume *= qualityLevel;
                }
            }
            
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume *= qualityLevel;
            }
        }
        
        public Dictionary<string, object> GetDiagnosticInfo()
        {
            return new Dictionary<string, object>
            {
                ["ActiveZones"] = audioZones.Count,
                ["ActiveTransitions"] = activeTransitions.Count,
                ["SpatialAudioSources"] = spatialAudioSources.Count,
                ["CurrentZone"] = currentZone,
                ["PerformanceCost"] = currentPerformanceCost,
                ["QualityLevel"] = qualityLevel,
                ["IsActive"] = isActive
            };
        }
    }
    
    [System.Serializable]
    public class AudioZone
    {
        public string Id;
        public Vector3 Position;
        public float Radius;
        public AudioClip AmbientClip;
        public float Volume = 1f;
    }
    
    [System.Serializable]
    public class AudioTransition
    {
        public string FromZone;
        public string ToZone;
        public float StartTime;
        public float Duration;
        public Coroutine Coroutine;
    }
}
