using UnityEngine;
using System.Collections.Generic;

namespace NeonQuest.Core
{
    /// <summary>
    /// Base MonoBehaviour class for all NeonQuest system components
    /// Provides common functionality like error handling, logging, and configuration management
    /// </summary>
    public abstract class NeonQuestComponent : MonoBehaviour
    {
        [Header("Component Configuration")]
        [SerializeField] protected bool enableDebugLogging = false;
        [SerializeField] protected bool autoInitialize = true;

        protected bool isInitialized = false;
        protected Dictionary<string, object> componentConfig = new Dictionary<string, object>();

        /// <summary>
        /// Gets whether this component is initialized and ready for use
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets the component's current configuration
        /// </summary>
        public Dictionary<string, object> Configuration => new Dictionary<string, object>(componentConfig);

        protected virtual void Awake()
        {
            if (autoInitialize)
            {
                InitializeComponent();
            }
        }

        protected virtual void Start()
        {
            if (autoInitialize && !isInitialized)
            {
                InitializeComponent();
            }
        }

        /// <summary>
        /// Initializes the component with default or provided configuration
        /// </summary>
        /// <param name="config">Optional configuration parameters</param>
        public virtual void InitializeComponent(Dictionary<string, object> config = null)
        {
            if (isInitialized)
            {
                LogDebug("Component already initialized, skipping initialization");
                return;
            }

            try
            {
                if (config != null)
                {
                    componentConfig = new Dictionary<string, object>(config);
                }

                OnInitialize();
                isInitialized = true;
                LogDebug($"Component {GetType().Name} initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize component {GetType().Name}: {ex.Message}");
                OnInitializationFailed(ex);
            }
        }

        /// <summary>
        /// Updates the component's configuration at runtime
        /// </summary>
        /// <param name="newConfig">New configuration parameters</param>
        public virtual void UpdateConfiguration(Dictionary<string, object> newConfig)
        {
            if (newConfig == null) return;

            foreach (var kvp in newConfig)
            {
                componentConfig[kvp.Key] = kvp.Value;
            }

            OnConfigurationUpdated();
            LogDebug($"Configuration updated for {GetType().Name}");
        }

        /// <summary>
        /// Gets a configuration value with optional default
        /// </summary>
        /// <typeparam name="T">Type of the configuration value</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value or default</returns>
        protected T GetConfigValue<T>(string key, T defaultValue = default(T))
        {
            if (componentConfig.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch (System.InvalidCastException)
                {
                    LogWarning($"Failed to cast config value '{key}' to type {typeof(T).Name}, using default");
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Override this method to implement component-specific initialization logic
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// Override this method to handle configuration updates
        /// </summary>
        protected virtual void OnConfigurationUpdated() { }

        /// <summary>
        /// Override this method to handle initialization failures
        /// </summary>
        /// <param name="exception">The exception that caused initialization to fail</param>
        protected virtual void OnInitializationFailed(System.Exception exception) { }

        /// <summary>
        /// Logs a debug message if debug logging is enabled
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        protected virtual void OnDestroy()
        {
            OnCleanup();
        }

        /// <summary>
        /// Override this method to implement component cleanup logic
        /// </summary>
        protected virtual void OnCleanup() { }
    }
}