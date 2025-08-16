using System.Collections.Generic;
using UnityEngine;

namespace NeonQuest.Core.ErrorHandling
{
    /// <summary>
    /// Provides fallback behaviors for various system failures
    /// </summary>
    public static class FallbackBehaviors
    {
        // Asset loading fallbacks
        private static GameObject _fallbackPrefab;
        private static Material _fallbackMaterial;
        private static AudioClip _fallbackAudioClip;

        // Configuration fallbacks
        private static readonly Dictionary<string, object> _defaultConfigValues = new Dictionary<string, object>
        {
            { "generation_distance", 50.0f },
            { "cleanup_distance", 100.0f },
            { "neon_response_distance", 5.0f },
            { "brightness_multiplier_range", new float[] { 0.5f, 2.0f } },
            { "transition_duration", 2.0f },
            { "fog_density_range", new float[] { 0.1f, 0.8f } },
            { "ambient_volume_range", new float[] { 0.3f, 0.9f } },
            { "transition_speed", 0.1f }
        };

        /// <summary>
        /// Initialize fallback resources
        /// </summary>
        public static void Initialize()
        {
            CreateFallbackPrefab();
            CreateFallbackMaterial();
            CreateFallbackAudioClip();
            
            NeonQuestLogger.LogInfo("Fallback behaviors initialized", NeonQuestLogger.LogCategory.General);
        }

        /// <summary>
        /// Get fallback prefab for asset loading failures
        /// </summary>
        public static GameObject GetFallbackPrefab()
        {
            if (_fallbackPrefab == null)
            {
                CreateFallbackPrefab();
            }
            return _fallbackPrefab;
        }

        /// <summary>
        /// Get fallback material for material loading failures
        /// </summary>
        public static Material GetFallbackMaterial()
        {
            if (_fallbackMaterial == null)
            {
                CreateFallbackMaterial();
            }
            return _fallbackMaterial;
        }

        /// <summary>
        /// Get fallback audio clip for audio loading failures
        /// </summary>
        public static AudioClip GetFallbackAudioClip()
        {
            if (_fallbackAudioClip == null)
            {
                CreateFallbackAudioClip();
            }
            return _fallbackAudioClip;
        }

        /// <summary>
        /// Get default configuration value for missing config entries
        /// </summary>
        public static T GetDefaultConfigValue<T>(string key, T fallback = default(T))
        {
            if (_defaultConfigValues.ContainsKey(key))
            {
                try
                {
                    return (T)_defaultConfigValues[key];
                }
                catch (System.InvalidCastException)
                {
                    NeonQuestLogger.LogWarning($"Failed to cast default config value for key '{key}' to type {typeof(T)}", 
                        NeonQuestLogger.LogCategory.Configuration);
                    return fallback;
                }
            }
            
            NeonQuestLogger.LogWarning($"No default config value found for key '{key}', using fallback", 
                NeonQuestLogger.LogCategory.Configuration);
            return fallback;
        }

        /// <summary>
        /// Create safe generation parameters when normal generation fails
        /// </summary>
        public static GenerationParameters GetSafeGenerationParameters()
        {
            return new GenerationParameters
            {
                MaxObjectsPerFrame = 1,
                GenerationDistance = 25.0f,
                CleanupDistance = 50.0f,
                UseSimplifiedGeometry = true,
                DisableComplexEffects = true
            };
        }

        private static void CreateFallbackPrefab()
        {
            _fallbackPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _fallbackPrefab.name = "FallbackPrefab";
            
            // Make it visually distinct
            var renderer = _fallbackPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.magenta;
            }
            
            // Don't destroy this object when loading new scenes
            Object.DontDestroyOnLoad(_fallbackPrefab);
            _fallbackPrefab.SetActive(false);
        }

        private static void CreateFallbackMaterial()
        {
            _fallbackMaterial = new Material(Shader.Find("Standard"));
            _fallbackMaterial.color = Color.magenta;
            _fallbackMaterial.name = "FallbackMaterial";
        }

        private static void CreateFallbackAudioClip()
        {
            // Create a simple sine wave audio clip as fallback
            int sampleRate = 44100;
            float duration = 1.0f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            
            _fallbackAudioClip = AudioClip.Create("FallbackAudio", samples, 1, sampleRate, false);
            float[] audioData = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                audioData[i] = Mathf.Sin(2.0f * Mathf.PI * 440.0f * i / sampleRate) * 0.1f; // Quiet 440Hz tone
            }
            
            _fallbackAudioClip.SetData(audioData, 0);
        }
    }

    /// <summary>
    /// Safe generation parameters for fallback scenarios
    /// </summary>
    public class GenerationParameters
    {
        public int MaxObjectsPerFrame { get; set; } = 5;
        public float GenerationDistance { get; set; } = 50.0f;
        public float CleanupDistance { get; set; } = 100.0f;
        public bool UseSimplifiedGeometry { get; set; } = false;
        public bool DisableComplexEffects { get; set; } = false;
    }
}