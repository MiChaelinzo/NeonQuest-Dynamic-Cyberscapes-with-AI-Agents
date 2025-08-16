using UnityEngine;
using System.Collections.Generic;
using NeonQuest.Configuration;

namespace NeonQuest.Core.Diagnostics
{
    public class RuntimeConfigurationAdjuster : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EnvironmentConfigurationAsset configurationAsset;
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        
        [Header("Runtime Adjustments")]
        [SerializeField] private bool enableRuntimeAdjustments = true;
        [SerializeField] private float adjustmentSensitivity = 0.1f;
        
        private bool showUI = false;
        private Vector2 scrollPosition;
        private Rect windowRect = new Rect(20, 20, 400, 600);
        
        // Runtime adjustment values
        private float runtimeGenerationDistance;
        private float runtimeCleanupDistance;
        private float runtimeNeonResponseDistance;
        private Vector2 runtimeBrightnessRange;
        private float runtimeTransitionDuration;
        private Vector2 runtimeFogRange;
        private Vector2 runtimeVolumeRange;
        private float runtimeTransitionSpeed;
        private int runtimeMaxSegments;
        private float runtimeThrottleThreshold;
        
        // Original values for reset
        private EnvironmentConfiguration originalConfiguration;
        
        private void Start()
        {
            InitializeRuntimeValues();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
            
            if (enableRuntimeAdjustments)
            {
                HandleKeyboardAdjustments();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugUI || !showUI) return;
            
            windowRect = GUILayout.Window(0, windowRect, DrawConfigurationWindow, "Runtime Configuration Adjuster");
        }
        
        private void DrawConfigurationWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Configuration Adjustments", GUI.skin.box);
            
            if (configurationAsset?.Configuration != null)
            {
                DrawCorridorAdjustments();
                DrawLightingAdjustments();
                DrawAtmosphereAdjustments();
                DrawPerformanceAdjustments();
                DrawActionButtons();
            }
            else
            {
                GUILayout.Label("No configuration asset assigned");
            }
            
            GUILayout.EndScrollView();
            
            GUI.DragWindow();
        }
        
        private void DrawCorridorAdjustments()
        {
            GUILayout.Label("Corridor Settings", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Generation Distance:", GUILayout.Width(150));
            runtimeGenerationDistance = GUILayout.HorizontalSlider(runtimeGenerationDistance, 10f, 200f);
            GUILayout.Label(runtimeGenerationDistance.ToString("F1"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cleanup Distance:", GUILayout.Width(150));
            runtimeCleanupDistance = GUILayout.HorizontalSlider(runtimeCleanupDistance, 20f, 400f);
            GUILayout.Label(runtimeCleanupDistance.ToString("F1"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Apply Corridor Changes"))
            {
                ApplyCorridorChanges();
            }
        }
        
        private void DrawLightingAdjustments()
        {
            GUILayout.Label("Lighting Settings", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neon Response Distance:", GUILayout.Width(150));
            runtimeNeonResponseDistance = GUILayout.HorizontalSlider(runtimeNeonResponseDistance, 1f, 20f);
            GUILayout.Label(runtimeNeonResponseDistance.ToString("F1"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Brightness Min:", GUILayout.Width(150));
            runtimeBrightnessRange.x = GUILayout.HorizontalSlider(runtimeBrightnessRange.x, 0f, 2f);
            GUILayout.Label(runtimeBrightnessRange.x.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Brightness Max:", GUILayout.Width(150));
            runtimeBrightnessRange.y = GUILayout.HorizontalSlider(runtimeBrightnessRange.y, 0f, 5f);
            GUILayout.Label(runtimeBrightnessRange.y.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Transition Duration:", GUILayout.Width(150));
            runtimeTransitionDuration = GUILayout.HorizontalSlider(runtimeTransitionDuration, 0.1f, 10f);
            GUILayout.Label(runtimeTransitionDuration.ToString("F1"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Apply Lighting Changes"))
            {
                ApplyLightingChanges();
            }
        }
        
        private void DrawAtmosphereAdjustments()
        {
            GUILayout.Label("Atmosphere Settings", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fog Density Min:", GUILayout.Width(150));
            runtimeFogRange.x = GUILayout.HorizontalSlider(runtimeFogRange.x, 0f, 1f);
            GUILayout.Label(runtimeFogRange.x.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fog Density Max:", GUILayout.Width(150));
            runtimeFogRange.y = GUILayout.HorizontalSlider(runtimeFogRange.y, 0f, 1f);
            GUILayout.Label(runtimeFogRange.y.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume Min:", GUILayout.Width(150));
            runtimeVolumeRange.x = GUILayout.HorizontalSlider(runtimeVolumeRange.x, 0f, 1f);
            GUILayout.Label(runtimeVolumeRange.x.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume Max:", GUILayout.Width(150));
            runtimeVolumeRange.y = GUILayout.HorizontalSlider(runtimeVolumeRange.y, 0f, 1f);
            GUILayout.Label(runtimeVolumeRange.y.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Transition Speed:", GUILayout.Width(150));
            runtimeTransitionSpeed = GUILayout.HorizontalSlider(runtimeTransitionSpeed, 0.01f, 1f);
            GUILayout.Label(runtimeTransitionSpeed.ToString("F3"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Apply Atmosphere Changes"))
            {
                ApplyAtmosphereChanges();
            }
        }
        
        private void DrawPerformanceAdjustments()
        {
            GUILayout.Label("Performance Settings", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Active Segments:", GUILayout.Width(150));
            runtimeMaxSegments = Mathf.RoundToInt(GUILayout.HorizontalSlider(runtimeMaxSegments, 1, 30));
            GUILayout.Label(runtimeMaxSegments.ToString(), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Throttle Threshold:", GUILayout.Width(150));
            runtimeThrottleThreshold = GUILayout.HorizontalSlider(runtimeThrottleThreshold, 30f, 120f);
            GUILayout.Label(runtimeThrottleThreshold.ToString("F0"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Apply Performance Changes"))
            {
                ApplyPerformanceChanges();
            }
        }
        
        private void DrawActionButtons()
        {
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply All Changes"))
            {
                ApplyAllChanges();
            }
            
            if (GUILayout.Button("Reset to Original"))
            {
                ResetToOriginal();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Current as Preset"))
            {
                SaveCurrentAsPreset();
            }
            
            if (GUILayout.Button("Export to YAML"))
            {
                ExportToYAML();
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Real-time performance info
            GUILayout.Label("Performance Info", GUI.skin.box);
            GUILayout.Label($"FPS: {(1f / Time.deltaTime):F0}");
            GUILayout.Label($"Frame Time: {Time.deltaTime * 1000f:F1}ms");
            GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024:F0}MB");
        }
        
        private void HandleKeyboardAdjustments()
        {
            if (!showUI) return;
            
            var adjustment = adjustmentSensitivity;
            if (Input.GetKey(KeyCode.LeftShift)) adjustment *= 5f;
            if (Input.GetKey(KeyCode.LeftControl)) adjustment *= 0.2f;
            
            // Quick adjustments with number keys
            if (Input.GetKey(KeyCode.Alpha1)) // Generation distance
            {
                if (Input.GetKey(KeyCode.Equals)) runtimeGenerationDistance += adjustment * 10f;
                if (Input.GetKey(KeyCode.Minus)) runtimeGenerationDistance -= adjustment * 10f;
                runtimeGenerationDistance = Mathf.Clamp(runtimeGenerationDistance, 10f, 200f);
            }
            
            if (Input.GetKey(KeyCode.Alpha2)) // Fog density
            {
                if (Input.GetKey(KeyCode.Equals)) runtimeFogRange.y += adjustment;
                if (Input.GetKey(KeyCode.Minus)) runtimeFogRange.y -= adjustment;
                runtimeFogRange.y = Mathf.Clamp01(runtimeFogRange.y);
            }
            
            if (Input.GetKey(KeyCode.Alpha3)) // Lighting intensity
            {
                if (Input.GetKey(KeyCode.Equals)) runtimeBrightnessRange.y += adjustment;
                if (Input.GetKey(KeyCode.Minus)) runtimeBrightnessRange.y -= adjustment;
                runtimeBrightnessRange.y = Mathf.Clamp(runtimeBrightnessRange.y, 0f, 5f);
            }
        }
        
        private void InitializeRuntimeValues()
        {
            if (configurationAsset?.Configuration != null)
            {
                var config = configurationAsset.Configuration;
                originalConfiguration = new EnvironmentConfiguration
                {
                    CorridorGenerationDistance = config.CorridorGenerationDistance,
                    CorridorCleanupDistance = config.CorridorCleanupDistance,
                    NeonResponseDistance = config.NeonResponseDistance,
                    BrightnessMultiplierRange = config.BrightnessMultiplierRange,
                    LightingTransitionDuration = config.LightingTransitionDuration,
                    FogDensityRange = config.FogDensityRange,
                    AmbientVolumeRange = config.AmbientVolumeRange,
                    AtmosphereTransitionSpeed = config.AtmosphereTransitionSpeed,
                    MaxActiveSegments = config.MaxActiveSegments,
                    PerformanceThrottleThreshold = config.PerformanceThrottleThreshold
                };
                
                LoadRuntimeValues();
            }
        }
        
        private void LoadRuntimeValues()
        {
            if (configurationAsset?.Configuration != null)
            {
                var config = configurationAsset.Configuration;
                runtimeGenerationDistance = config.CorridorGenerationDistance;
                runtimeCleanupDistance = config.CorridorCleanupDistance;
                runtimeNeonResponseDistance = config.NeonResponseDistance;
                runtimeBrightnessRange = config.BrightnessMultiplierRange;
                runtimeTransitionDuration = config.LightingTransitionDuration;
                runtimeFogRange = config.FogDensityRange;
                runtimeVolumeRange = config.AmbientVolumeRange;
                runtimeTransitionSpeed = config.AtmosphereTransitionSpeed;
                runtimeMaxSegments = config.MaxActiveSegments;
                runtimeThrottleThreshold = config.PerformanceThrottleThreshold;
            }
        }
        
        private void ApplyCorridorChanges()
        {
            if (configurationAsset?.Configuration != null)
            {
                configurationAsset.Configuration.CorridorGenerationDistance = runtimeGenerationDistance;
                configurationAsset.Configuration.CorridorCleanupDistance = runtimeCleanupDistance;
                NotifyConfigurationChanged();
            }
        }
        
        private void ApplyLightingChanges()
        {
            if (configurationAsset?.Configuration != null)
            {
                configurationAsset.Configuration.NeonResponseDistance = runtimeNeonResponseDistance;
                configurationAsset.Configuration.BrightnessMultiplierRange = runtimeBrightnessRange;
                configurationAsset.Configuration.LightingTransitionDuration = runtimeTransitionDuration;
                NotifyConfigurationChanged();
            }
        }
        
        private void ApplyAtmosphereChanges()
        {
            if (configurationAsset?.Configuration != null)
            {
                configurationAsset.Configuration.FogDensityRange = runtimeFogRange;
                configurationAsset.Configuration.AmbientVolumeRange = runtimeVolumeRange;
                configurationAsset.Configuration.AtmosphereTransitionSpeed = runtimeTransitionSpeed;
                NotifyConfigurationChanged();
            }
        }
        
        private void ApplyPerformanceChanges()
        {
            if (configurationAsset?.Configuration != null)
            {
                configurationAsset.Configuration.MaxActiveSegments = runtimeMaxSegments;
                configurationAsset.Configuration.PerformanceThrottleThreshold = runtimeThrottleThreshold;
                NotifyConfigurationChanged();
            }
        }
        
        private void ApplyAllChanges()
        {
            ApplyCorridorChanges();
            ApplyLightingChanges();
            ApplyAtmosphereChanges();
            ApplyPerformanceChanges();
        }
        
        private void ResetToOriginal()
        {
            if (originalConfiguration != null && configurationAsset?.Configuration != null)
            {
                var config = configurationAsset.Configuration;
                config.CorridorGenerationDistance = originalConfiguration.CorridorGenerationDistance;
                config.CorridorCleanupDistance = originalConfiguration.CorridorCleanupDistance;
                config.NeonResponseDistance = originalConfiguration.NeonResponseDistance;
                config.BrightnessMultiplierRange = originalConfiguration.BrightnessMultiplierRange;
                config.LightingTransitionDuration = originalConfiguration.LightingTransitionDuration;
                config.FogDensityRange = originalConfiguration.FogDensityRange;
                config.AmbientVolumeRange = originalConfiguration.AmbientVolumeRange;
                config.AtmosphereTransitionSpeed = originalConfiguration.AtmosphereTransitionSpeed;
                config.MaxActiveSegments = originalConfiguration.MaxActiveSegments;
                config.PerformanceThrottleThreshold = originalConfiguration.PerformanceThrottleThreshold;
                
                LoadRuntimeValues();
                NotifyConfigurationChanged();
            }
        }
        
        private void SaveCurrentAsPreset()
        {
            // This would save the current configuration as a new preset
            Debug.Log("Save current configuration as preset - functionality would be implemented here");
        }
        
        private void ExportToYAML()
        {
            // This would export the current configuration to a YAML file
            Debug.Log("Export to YAML - functionality would be implemented here");
        }
        
        private void NotifyConfigurationChanged()
        {
            // Notify other systems that configuration has changed
            var neonQuestManager = FindObjectOfType<NeonQuestManager>();
            if (neonQuestManager != null)
            {
                // Trigger configuration reload
                Debug.Log("Configuration updated - notifying NeonQuest systems");
            }
        }
        
        public void SetConfigurationAsset(EnvironmentConfigurationAsset asset)
        {
            configurationAsset = asset;
            InitializeRuntimeValues();
        }
        
        public EnvironmentConfiguration GetCurrentConfiguration()
        {
            return configurationAsset?.Configuration;
        }
    }
}