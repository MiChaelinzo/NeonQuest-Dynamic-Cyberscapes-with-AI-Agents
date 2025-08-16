using UnityEngine;
using System.Collections.Generic;

namespace NeonQuest.Core.SceneSetup
{
    /// <summary>
    /// Template for Unity scene configuration with NeonQuest and Neon Underground assets
    /// This ScriptableObject can be used to store scene-specific settings
    /// </summary>
    [CreateAssetMenu(fileName = "NeonQuestSceneConfig", menuName = "NeonQuest/Scene Configuration")]
    public class SceneConfigurationTemplate : ScriptableObject
    {
        [Header("Scene Identity")]
        [SerializeField] private string sceneName;
        [SerializeField] private string sceneDescription;
        
        [Header("Configuration Files")]
        [SerializeField] private string yamlConfigurationPath = "Scripts/NeonQuest/Configuration/Examples/default_environment.yaml";
        [SerializeField] private bool enableHotReload = true;
        
        [Header("Neon Underground Assets")]
        [SerializeField] private GameObject[] neonUndergroundPrefabs;
        [SerializeField] private bool preserveOriginalStructure = true;
        [SerializeField] private bool createProceduralVariants = true;
        
        [Header("System Settings")]
        [SerializeField] private bool autoInitializeOnSceneLoad = true;
        [SerializeField] private bool createDefaultSystemsIfMissing = true;
        [SerializeField] private bool enableDiagnostics = true;
        
        [Header("Performance Settings")]
        [SerializeField] private float systemUpdateInterval = 0.1f;
        [SerializeField] private int maxConcurrentGenerations = 3;
        [SerializeField] private float performanceThrottleThreshold = 60f;
        
        [Header("Asset Organization")]
        [SerializeField] private string assetContainerName = "NeonUndergroundAssets";
        [SerializeField] private bool organizeAssetsByType = true;
        
        /// <summary>
        /// Gets the scene name
        /// </summary>
        public string SceneName => sceneName;
        
        /// <summary>
        /// Gets the scene description
        /// </summary>
        public string SceneDescription => sceneDescription;
        
        /// <summary>
        /// Gets the YAML configuration path
        /// </summary>
        public string YamlConfigurationPath => yamlConfigurationPath;
        
        /// <summary>
        /// Gets whether hot reload is enabled
        /// </summary>
        public bool EnableHotReload => enableHotReload;
        
        /// <summary>
        /// Gets the Neon Underground prefabs
        /// </summary>
        public GameObject[] NeonUndergroundPrefabs => neonUndergroundPrefabs;
        
        /// <summary>
        /// Gets whether to preserve original structure
        /// </summary>
        public bool PreserveOriginalStructure => preserveOriginalStructure;
        
        /// <summary>
        /// Gets whether to create procedural variants
        /// </summary>
        public bool CreateProceduralVariants => createProceduralVariants;
        
        /// <summary>
        /// Gets whether to auto-initialize on scene load
        /// </summary>
        public bool AutoInitializeOnSceneLoad => autoInitializeOnSceneLoad;
        
        /// <summary>
        /// Gets whether to create default systems if missing
        /// </summary>
        public bool CreateDefaultSystemsIfMissing => createDefaultSystemsIfMissing;
        
        /// <summary>
        /// Gets whether diagnostics are enabled
        /// </summary>
        public bool EnableDiagnostics => enableDiagnostics;
        
        /// <summary>
        /// Gets the system update interval
        /// </summary>
        public float SystemUpdateInterval => systemUpdateInterval;
        
        /// <summary>
        /// Gets the maximum concurrent generations
        /// </summary>
        public int MaxConcurrentGenerations => maxConcurrentGenerations;
        
        /// <summary>
        /// Gets the performance throttle threshold
        /// </summary>
        public float PerformanceThrottleThreshold => performanceThrottleThreshold;
        
        /// <summary>
        /// Gets the asset container name
        /// </summary>
        public string AssetContainerName => assetContainerName;
        
        /// <summary>
        /// Gets whether to organize assets by type
        /// </summary>
        public bool OrganizeAssetsByType => organizeAssetsByType;
        
        /// <summary>
        /// Applies this configuration to a NeonQuestSceneManager
        /// </summary>
        public void ApplyToSceneManager(NeonQuestSceneManager sceneManager)
        {
            if (sceneManager == null) return;
            
            var sceneManagerType = typeof(NeonQuestSceneManager);
            
            // Apply configuration using reflection
            SetPrivateField(sceneManagerType, sceneManager, "sceneConfigurationPath", yamlConfigurationPath);
            SetPrivateField(sceneManagerType, sceneManager, "autoActivateOnSceneLoad", autoInitializeOnSceneLoad);
            SetPrivateField(sceneManagerType, sceneManager, "createDefaultSystemsIfMissing", createDefaultSystemsIfMissing);
            SetPrivateField(sceneManagerType, sceneManager, "neonUndergroundPrefabs", neonUndergroundPrefabs);
            SetPrivateField(sceneManagerType, sceneManager, "preserveOriginalPrefabStructure", preserveOriginalStructure);
            
            Debug.Log($"Applied scene configuration '{sceneName}' to scene manager");
        }
        
        /// <summary>
        /// Applies this configuration to a NeonQuestManager
        /// </summary>
        public void ApplyToNeonQuestManager(NeonQuestManager neonQuestManager)
        {
            if (neonQuestManager == null) return;
            
            var managerType = typeof(NeonQuestManager);
            
            // Apply configuration using reflection
            SetPrivateField(managerType, neonQuestManager, "systemUpdateInterval", systemUpdateInterval);
            SetPrivateField(managerType, neonQuestManager, "configurationFilePath", yamlConfigurationPath);
            SetPrivateField(managerType, neonQuestManager, "enableHotReload", enableHotReload);
            SetPrivateField(managerType, neonQuestManager, "enableDiagnostics", enableDiagnostics);
            
            Debug.Log($"Applied scene configuration '{sceneName}' to NeonQuest manager");
        }
        
        private void SetPrivateField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(instance, value);
        }
        
        /// <summary>
        /// Creates a default scene configuration
        /// </summary>
        public static SceneConfigurationTemplate CreateDefault(string sceneName)
        {
            var config = CreateInstance<SceneConfigurationTemplate>();
            config.sceneName = sceneName;
            config.sceneDescription = $"Default configuration for {sceneName}";
            config.yamlConfigurationPath = "Scripts/NeonQuest/Configuration/Examples/default_environment.yaml";
            config.enableHotReload = true;
            config.preserveOriginalStructure = true;
            config.createProceduralVariants = true;
            config.autoInitializeOnSceneLoad = true;
            config.createDefaultSystemsIfMissing = true;
            config.enableDiagnostics = true;
            config.systemUpdateInterval = 0.1f;
            config.maxConcurrentGenerations = 3;
            config.performanceThrottleThreshold = 60f;
            config.assetContainerName = "NeonUndergroundAssets";
            config.organizeAssetsByType = true;
            
            return config;
        }
        
        /// <summary>
        /// Validates the configuration for common issues
        /// </summary>
        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();
            
            if (string.IsNullOrEmpty(sceneName))
            {
                issues.Add("Scene name is required");
            }
            
            if (string.IsNullOrEmpty(yamlConfigurationPath))
            {
                issues.Add("YAML configuration path is required");
            }
            
            if (systemUpdateInterval <= 0f)
            {
                issues.Add("System update interval must be positive");
            }
            
            if (maxConcurrentGenerations <= 0)
            {
                issues.Add("Max concurrent generations must be positive");
            }
            
            if (performanceThrottleThreshold <= 0f)
            {
                issues.Add("Performance throttle threshold must be positive");
            }
            
            if (neonUndergroundPrefabs != null)
            {
                for (int i = 0; i < neonUndergroundPrefabs.Length; i++)
                {
                    if (neonUndergroundPrefabs[i] == null)
                    {
                        issues.Add($"Neon Underground prefab at index {i} is null");
                    }
                }
            }
            
            return issues;
        }
        
        private void OnValidate()
        {
            // Ensure positive values
            systemUpdateInterval = Mathf.Max(0.01f, systemUpdateInterval);
            maxConcurrentGenerations = Mathf.Max(1, maxConcurrentGenerations);
            performanceThrottleThreshold = Mathf.Max(1f, performanceThrottleThreshold);
        }
    }
}