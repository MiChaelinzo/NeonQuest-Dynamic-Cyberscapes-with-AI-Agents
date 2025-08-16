using System;
using System.Collections;
using System.IO;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Configuration
{
    public class ConfigurationManager : MonoBehaviour
    {
        [SerializeField] private string _configurationFilePath = "Scripts/NeonQuest/Configuration/Examples/default_environment.yaml";
        [SerializeField] private bool _enableHotReload = true;
        [SerializeField] private float _reloadDelay = 0.5f;
        
        private readonly NeonQuestLogger _logger;
        private YAMLConfigLoader _configLoader;
        private EnvironmentRulesEngine _rulesEngine;
        private FileWatcher _fileWatcher;
        private EnvironmentConfiguration _currentConfiguration;
        private EnvironmentConfiguration _fallbackConfiguration;
        private Coroutine _reloadCoroutine;
        
        public event Action<EnvironmentConfiguration> ConfigurationLoaded;
        public event Action<EnvironmentConfiguration> ConfigurationReloaded;
        public event Action<string> ConfigurationError;
        
        public EnvironmentConfiguration CurrentConfiguration => _currentConfiguration;
        public EnvironmentRulesEngine RulesEngine => _rulesEngine;
        public bool IsHotReloadEnabled => _enableHotReload;
        public string ConfigurationFilePath => _configurationFilePath;
        
        public ConfigurationManager()
        {
            _logger = new NeonQuestLogger("ConfigurationManager");
        }
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            LoadConfiguration();
            
            if (_enableHotReload)
            {
                SetupHotReload();
            }
        }
        
        private void OnDestroy()
        {
            CleanupHotReload();
        }
        
        private void InitializeComponents()
        {
            _configLoader = new YAMLConfigLoader();
            _rulesEngine = new EnvironmentRulesEngine();
            _fallbackConfiguration = CreateFallbackConfiguration();
        }
        
        public void LoadConfiguration()
        {
            LoadConfiguration(_configurationFilePath);
        }
        
        public void LoadConfiguration(string filePath)
        {
            try
            {
                _logger.LogInfo($"Loading configuration from: {filePath}");
                
                var config = _configLoader.LoadConfiguration(filePath);
                
                if (config == null || !config.IsValid())
                {
                    _logger.LogWarning("Loaded configuration is invalid. Using fallback configuration.");
                    config = _fallbackConfiguration;
                }
                
                ApplyConfiguration(config);
                _configurationFilePath = filePath;
                
                ConfigurationLoaded?.Invoke(_currentConfiguration);
                _logger.LogInfo("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load configuration: {ex.Message}");
                HandleConfigurationError($"Failed to load configuration: {ex.Message}");
            }
        }
        
        public void ReloadConfiguration()
        {
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
            }
            
            _reloadCoroutine = StartCoroutine(ReloadConfigurationCoroutine());
        }
        
        private IEnumerator ReloadConfigurationCoroutine()
        {
            yield return new WaitForSeconds(_reloadDelay);
            
            try
            {
                _logger.LogInfo("Hot-reloading configuration...");
                
                var newConfig = _configLoader.LoadConfiguration(_configurationFilePath);
                
                if (newConfig == null || !newConfig.IsValid())
                {
                    _logger.LogError("Reloaded configuration is invalid. Keeping current configuration.");
                    HandleConfigurationError("Reloaded configuration is invalid");
                    yield break;
                }
                
                // Validate that the new configuration won't break active generation
                if (!ValidateConfigurationForHotReload(newConfig))
                {
                    _logger.LogError("New configuration would break active generation. Keeping current configuration.");
                    HandleConfigurationError("New configuration incompatible with active generation");
                    yield break;
                }
                
                ApplyConfiguration(newConfig);
                ConfigurationReloaded?.Invoke(_currentConfiguration);
                
                _logger.LogInfo("Configuration hot-reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to hot-reload configuration: {ex.Message}");
                HandleConfigurationError($"Hot-reload failed: {ex.Message}");
            }
            finally
            {
                _reloadCoroutine = null;
            }
        }
        
        private void ApplyConfiguration(EnvironmentConfiguration config)
        {
            _currentConfiguration = config;
            _rulesEngine.LoadConfiguration(config);
        }
        
        private bool ValidateConfigurationForHotReload(EnvironmentConfiguration newConfig)
        {
            // Check if critical parameters haven't changed dramatically
            if (_currentConfiguration != null)
            {
                // Ensure generation distances haven't changed too drastically
                float distanceRatio = newConfig.CorridorGenerationDistance / _currentConfiguration.CorridorGenerationDistance;
                if (distanceRatio < 0.5f || distanceRatio > 2.0f)
                {
                    _logger.LogWarning("Generation distance changed dramatically. This might affect active generation.");
                }
                
                // Ensure performance settings are reasonable
                if (newConfig.PerformanceThrottleThreshold < 30.0f)
                {
                    _logger.LogWarning("Performance throttle threshold is very low. This might cause issues.");
                }
            }
            
            return true; // For now, allow all valid configurations
        }
        
        private void SetupHotReload()
        {
            if (_fileWatcher != null)
            {
                CleanupHotReload();
            }
            
            _fileWatcher = new FileWatcher();
            _fileWatcher.FileChanged += OnConfigurationFileChanged;
            
            string fullPath = Path.Combine(Application.dataPath, "..", _configurationFilePath);
            
            if (_fileWatcher.StartWatching(fullPath))
            {
                _logger.LogInfo($"Hot-reload enabled for: {fullPath}");
            }
            else
            {
                _logger.LogWarning("Failed to enable hot-reload");
            }
        }
        
        private void CleanupHotReload()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.FileChanged -= OnConfigurationFileChanged;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }
        
        private void OnConfigurationFileChanged(string filePath)
        {
            _logger.LogDebug($"Configuration file changed: {filePath}");
            
            // Use Unity's main thread to reload configuration
            if (this != null && gameObject.activeInHierarchy)
            {
                ReloadConfiguration();
            }
        }
        
        private void HandleConfigurationError(string errorMessage)
        {
            // If we don't have a current configuration, use fallback
            if (_currentConfiguration == null)
            {
                _logger.LogWarning("Using fallback configuration due to error");
                ApplyConfiguration(_fallbackConfiguration);
            }
            
            ConfigurationError?.Invoke(errorMessage);
        }
        
        private EnvironmentConfiguration CreateFallbackConfiguration()
        {
            var config = new EnvironmentConfiguration();
            
            // Add a basic rule for testing
            var basicRule = new GenerationRule("FallbackRule")
            {
                Priority = 1.0f,
                Cooldown = 5.0f
            };
            
            basicRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                1.0f
            ));
            
            basicRule.Actions.Add(new GenerationAction(
                GenerationAction.ActionType.GenerateLayout,
                "corridor"
            ));
            
            config.AddRule(basicRule);
            
            return config;
        }
        
        public void SetHotReloadEnabled(bool enabled)
        {
            if (_enableHotReload == enabled)
                return;
                
            _enableHotReload = enabled;
            
            if (_enableHotReload)
            {
                SetupHotReload();
            }
            else
            {
                CleanupHotReload();
            }
            
            _logger.LogInfo($"Hot-reload {(enabled ? "enabled" : "disabled")}");
        }
        
        public void SetConfigurationFilePath(string filePath)
        {
            if (_configurationFilePath == filePath)
                return;
                
            _configurationFilePath = filePath;
            
            if (_enableHotReload)
            {
                SetupHotReload();
            }
        }
        
        // Editor/Debug methods
        [ContextMenu("Reload Configuration")]
        public void ReloadConfigurationFromMenu()
        {
            ReloadConfiguration();
        }
        
        [ContextMenu("Test Configuration Error")]
        public void TestConfigurationError()
        {
            HandleConfigurationError("Test error from context menu");
        }
    }
}