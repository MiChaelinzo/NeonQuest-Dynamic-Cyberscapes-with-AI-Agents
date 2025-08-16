using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;
using NeonQuest.Generation;
using NeonQuest.Assets;
using NeonQuest.Core.ErrorHandling;
using NeonQuest.Core.Diagnostics;

namespace NeonQuest.Core
{
    /// <summary>
    /// Main controller MonoBehaviour for the NeonQuest system
    /// Manages system initialization, dependency injection, and lifecycle coordination
    /// </summary>
    public class NeonQuestManager : NeonQuestComponent
    {
        [Header("System Configuration")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private float systemUpdateInterval = 0.1f;
        [SerializeField] private string configurationFilePath = "Scripts/NeonQuest/Configuration/Examples/default_environment.yaml";
        [SerializeField] private bool enableHotReload = true;
        [SerializeField] private bool enableDiagnostics = true;

        [Header("System Dependencies")]
        [SerializeField] private ConfigurationManager configurationManager;
        [SerializeField] private PlayerMovementTracker playerMovementTracker;
        [SerializeField] private PlayerBehaviorAnalyzer playerBehaviorAnalyzer;
        [SerializeField] private KiroAgentHooksManager kiroHooksManager;
        [SerializeField] private EnvironmentTriggersManager environmentTriggersManager;
        [SerializeField] private ProceduralGenerator proceduralGenerator;
        [SerializeField] private AssetIntegrator assetIntegrator;
        [SerializeField] private PerformanceThrottler performanceThrottler;
        [SerializeField] private DiagnosticsManager diagnosticsManager;

        private Dictionary<System.Type, NeonQuestComponent> systemComponents = new Dictionary<System.Type, NeonQuestComponent>();
        private List<IProceduralGenerator> proceduralGenerators = new List<IProceduralGenerator>();
        private List<IEnvironmentTrigger> environmentTriggers = new List<IEnvironmentTrigger>();
        private IAssetIntegrator assetIntegratorInterface;

        private Coroutine systemUpdateCoroutine;
        private bool isShuttingDown = false;
        private readonly List<System.Type> initializationOrder = new List<System.Type>();

        // System state tracking
        private Dictionary<System.Type, bool> systemInitializationStatus = new Dictionary<System.Type, bool>();
        private Dictionary<System.Type, string> systemErrors = new Dictionary<System.Type, string>();

        /// <summary>
        /// Gets whether all core systems are initialized and ready
        /// </summary>
        public bool AllSystemsReady => isInitialized && 
            systemComponents.Count > 0 && 
            systemInitializationStatus.Values.All(status => status) &&
            !isShuttingDown;

        /// <summary>
        /// Gets the configuration manager instance
        /// </summary>
        public ConfigurationManager ConfigurationManager => configurationManager;

        /// <summary>
        /// Gets the asset integrator instance
        /// </summary>
        public IAssetIntegrator AssetIntegrator => assetIntegratorInterface;

        /// <summary>
        /// Gets all registered procedural generators
        /// </summary>
        public IReadOnlyList<IProceduralGenerator> ProceduralGenerators => proceduralGenerators.AsReadOnly();

        /// <summary>
        /// Gets all registered environment triggers
        /// </summary>
        public IReadOnlyList<IEnvironmentTrigger> EnvironmentTriggers => environmentTriggers.AsReadOnly();

        /// <summary>
        /// Gets system initialization status for diagnostics
        /// </summary>
        public IReadOnlyDictionary<System.Type, bool> SystemInitializationStatus => systemInitializationStatus;

        /// <summary>
        /// Gets system errors for diagnostics
        /// </summary>
        public IReadOnlyDictionary<System.Type, string> SystemErrors => systemErrors;

        protected override void OnInitialize()
        {
            LogDebug("Initializing NeonQuest system manager");

            try
            {
                // Setup initialization order based on dependencies
                SetupInitializationOrder();

                // Find and register all system components
                RegisterSystemComponents();

                // Initialize configuration system first
                InitializeConfigurationSystem();

                // Initialize core systems in dependency order
                InitializeCoreSystems();

                // Setup system coordination
                SetupSystemCoordination();

                // Start system update loop
                if (systemUpdateCoroutine == null)
                {
                    systemUpdateCoroutine = StartCoroutine(SystemUpdateLoop());
                }

                // Initialize diagnostics if enabled
                if (enableDiagnostics && diagnosticsManager != null)
                {
                    InitializeDiagnostics();
                }

                LogDebug("NeonQuest system manager initialization complete");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize NeonQuest system manager: {ex.Message}");
                OnInitializationFailed(ex);
                throw;
            }
        }

        /// <summary>
        /// Sets up the initialization order based on system dependencies
        /// </summary>
        private void SetupInitializationOrder()
        {
            // Order matters: dependencies must be initialized before dependents
            initializationOrder.Clear();
            initializationOrder.AddRange(new System.Type[]
            {
                typeof(ConfigurationManager),
                typeof(DiagnosticsManager),
                typeof(PerformanceThrottler),
                typeof(AssetIntegrator),
                typeof(PlayerMovementTracker),
                typeof(PlayerBehaviorAnalyzer),
                typeof(KiroAgentHooksManager),
                typeof(EnvironmentTriggersManager),
                typeof(ProceduralGenerator)
            });
        }

        /// <summary>
        /// Initializes the configuration system and loads initial configuration
        /// </summary>
        private void InitializeConfigurationSystem()
        {
            if (configurationManager == null)
            {
                LogWarning("ConfigurationManager not assigned, creating default instance");
                var configGO = new GameObject("ConfigurationManager");
                configGO.transform.SetParent(transform);
                configurationManager = configGO.AddComponent<ConfigurationManager>();
            }

            // Set configuration parameters
            configurationManager.SetConfigurationFilePath(configurationFilePath);
            configurationManager.SetHotReloadEnabled(enableHotReload);

            // Subscribe to configuration events
            configurationManager.ConfigurationLoaded += OnConfigurationLoaded;
            configurationManager.ConfigurationReloaded += OnConfigurationReloaded;
            configurationManager.ConfigurationError += OnConfigurationError;

            RegisterSystemComponent(configurationManager);
            LogDebug("Configuration system initialized");
        }

        /// <summary>
        /// Sets up coordination between systems
        /// </summary>
        private void SetupSystemCoordination()
        {
            // Connect player behavior to environment triggers
            if (playerBehaviorAnalyzer != null && environmentTriggersManager != null)
            {
                playerBehaviorAnalyzer.BehaviorPatternDetected += environmentTriggersManager.OnBehaviorPatternDetected;
            }

            // Connect environment triggers to procedural generator
            if (environmentTriggersManager != null && proceduralGenerator != null)
            {
                environmentTriggersManager.TriggerActivated += proceduralGenerator.OnEnvironmentTriggerActivated;
            }

            // Connect performance throttler to procedural generator
            if (performanceThrottler != null && proceduralGenerator != null)
            {
                performanceThrottler.PerformanceThresholdExceeded += proceduralGenerator.OnPerformanceThresholdExceeded;
            }

            LogDebug("System coordination setup complete");
        }

        /// <summary>
        /// Initializes diagnostics system
        /// </summary>
        private void InitializeDiagnostics()
        {
            if (diagnosticsManager != null)
            {
                diagnosticsManager.RegisterSystemManager(this);
                LogDebug("Diagnostics system initialized");
            }
        }

        /// <summary>
        /// Registers a system component with the manager
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="component">Component instance to register</param>
        public void RegisterSystemComponent<T>(T component) where T : NeonQuestComponent
        {
            if (component == null)
            {
                LogWarning($"Attempted to register null component of type {typeof(T).Name}");
                systemInitializationStatus[typeof(T)] = false;
                systemErrors[typeof(T)] = "Component is null";
                return;
            }

            systemComponents[typeof(T)] = component;
            systemInitializationStatus[typeof(T)] = false;

            // Register with appropriate interface lists
            if (component is IProceduralGenerator generator)
            {
                proceduralGenerators.Add(generator);
            }

            if (component is IEnvironmentTrigger trigger)
            {
                environmentTriggers.Add(trigger);
            }

            if (component is IAssetIntegrator integrator)
            {
                assetIntegratorInterface = integrator;
            }

            LogDebug($"Registered system component: {typeof(T).Name}");
        }

        /// <summary>
        /// Gets a registered system component by type
        /// </summary>
        /// <typeparam name="T">Type of the component to retrieve</typeparam>
        /// <returns>Component instance or null if not found</returns>
        public T GetSystemComponent<T>() where T : NeonQuestComponent
        {
            if (systemComponents.TryGetValue(typeof(T), out NeonQuestComponent component))
            {
                return component as T;
            }
            return null;
        }

        /// <summary>
        /// Updates system configuration for all registered components
        /// </summary>
        /// <param name="globalConfig">Global configuration to apply</param>
        public void UpdateSystemConfiguration(Dictionary<string, object> globalConfig)
        {
            if (globalConfig == null) return;

            foreach (var component in systemComponents.Values)
            {
                if (component != null)
                {
                    try
                    {
                        component.UpdateConfiguration(globalConfig);
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Failed to update configuration for {component.GetType().Name}: {ex.Message}");
                        systemErrors[component.GetType()] = $"Configuration update failed: {ex.Message}";
                    }
                }
            }

            LogDebug("Updated system configuration for all components");
        }

        /// <summary>
        /// Configuration loaded event handler
        /// </summary>
        private void OnConfigurationLoaded(EnvironmentConfiguration config)
        {
            LogDebug("Configuration loaded, updating all systems");
            
            // Convert configuration to dictionary format for legacy compatibility
            var configDict = ConvertConfigurationToDictionary(config);
            UpdateSystemConfiguration(configDict);
        }

        /// <summary>
        /// Configuration reloaded event handler
        /// </summary>
        private void OnConfigurationReloaded(EnvironmentConfiguration config)
        {
            LogDebug("Configuration reloaded, updating all systems");
            
            var configDict = ConvertConfigurationToDictionary(config);
            UpdateSystemConfiguration(configDict);
        }

        /// <summary>
        /// Configuration error event handler
        /// </summary>
        private void OnConfigurationError(string errorMessage)
        {
            LogError($"Configuration error: {errorMessage}");
            
            if (diagnosticsManager != null)
            {
                diagnosticsManager.ReportSystemError("Configuration", errorMessage);
            }
        }

        /// <summary>
        /// Converts EnvironmentConfiguration to dictionary format
        /// </summary>
        private Dictionary<string, object> ConvertConfigurationToDictionary(EnvironmentConfiguration config)
        {
            var dict = new Dictionary<string, object>();
            
            if (config != null)
            {
                dict["corridorGenerationDistance"] = config.CorridorGenerationDistance;
                dict["corridorCleanupDistance"] = config.CorridorCleanupDistance;
                dict["neonResponseDistance"] = config.NeonResponseDistance;
                dict["lightingTransitionDuration"] = config.LightingTransitionDuration;
                dict["performanceThrottleThreshold"] = config.PerformanceThrottleThreshold;
                dict["fogDensityRange"] = new float[] { config.FogDensityMin, config.FogDensityMax };
                dict["ambientVolumeRange"] = new float[] { config.AmbientVolumeMin, config.AmbientVolumeMax };
            }
            
            return dict;
        }

        /// <summary>
        /// Gracefully shuts down all systems
        /// </summary>
        public void ShutdownSystems()
        {
            if (isShuttingDown)
            {
                LogWarning("System shutdown already in progress");
                return;
            }

            isShuttingDown = true;
            LogDebug("Shutting down NeonQuest systems");

            try
            {
                // Stop system update loop first
                if (systemUpdateCoroutine != null)
                {
                    StopCoroutine(systemUpdateCoroutine);
                    systemUpdateCoroutine = null;
                }

                // Unsubscribe from configuration events
                if (configurationManager != null)
                {
                    configurationManager.ConfigurationLoaded -= OnConfigurationLoaded;
                    configurationManager.ConfigurationReloaded -= OnConfigurationReloaded;
                    configurationManager.ConfigurationError -= OnConfigurationError;
                }

                // Disconnect system coordination
                DisconnectSystemCoordination();

                // Shutdown systems in reverse dependency order
                var reversedOrder = initializationOrder.ToArray().Reverse();
                foreach (var systemType in reversedOrder)
                {
                    if (systemComponents.TryGetValue(systemType, out var component) && 
                        component != null && component.gameObject != null)
                    {
                        try
                        {
                            component.OnCleanup();
                            LogDebug($"Shutdown system: {systemType.Name}");
                        }
                        catch (System.Exception ex)
                        {
                            LogError($"Error shutting down {systemType.Name}: {ex.Message}");
                        }
                    }
                }

                // Clear all collections
                systemComponents.Clear();
                proceduralGenerators.Clear();
                environmentTriggers.Clear();
                assetIntegratorInterface = null;
                systemInitializationStatus.Clear();
                systemErrors.Clear();

                LogDebug("System shutdown complete");
            }
            catch (System.Exception ex)
            {
                LogError($"Error during system shutdown: {ex.Message}");
            }
            finally
            {
                isShuttingDown = false;
            }
        }

        /// <summary>
        /// Disconnects system coordination
        /// </summary>
        private void DisconnectSystemCoordination()
        {
            try
            {
                // Disconnect player behavior from environment triggers
                if (playerBehaviorAnalyzer != null && environmentTriggersManager != null)
                {
                    playerBehaviorAnalyzer.BehaviorPatternDetected -= environmentTriggersManager.OnBehaviorPatternDetected;
                }

                // Disconnect environment triggers from procedural generator
                if (environmentTriggersManager != null && proceduralGenerator != null)
                {
                    environmentTriggersManager.TriggerActivated -= proceduralGenerator.OnEnvironmentTriggerActivated;
                }

                // Disconnect performance throttler from procedural generator
                if (performanceThrottler != null && proceduralGenerator != null)
                {
                    performanceThrottler.PerformanceThresholdExceeded -= proceduralGenerator.OnPerformanceThresholdExceeded;
                }

                LogDebug("System coordination disconnected");
            }
            catch (System.Exception ex)
            {
                LogError($"Error disconnecting system coordination: {ex.Message}");
            }
        }

        private void RegisterSystemComponents()
        {
            // Register explicitly assigned components first
            RegisterExplicitComponents();

            // Find and register any additional NeonQuest components in the scene
            var allComponents = FindObjectsOfType<NeonQuestComponent>();

            foreach (var component in allComponents)
            {
                if (component != this && !systemComponents.ContainsValue(component))
                {
                    var componentType = component.GetType();
                    RegisterSystemComponent(component);
                }
            }

            LogDebug($"Registered {systemComponents.Count} system components");
        }

        /// <summary>
        /// Registers explicitly assigned components
        /// </summary>
        private void RegisterExplicitComponents()
        {
            if (configurationManager != null) RegisterSystemComponent(configurationManager);
            if (playerMovementTracker != null) RegisterSystemComponent(playerMovementTracker);
            if (playerBehaviorAnalyzer != null) RegisterSystemComponent(playerBehaviorAnalyzer);
            if (kiroHooksManager != null) RegisterSystemComponent(kiroHooksManager);
            if (environmentTriggersManager != null) RegisterSystemComponent(environmentTriggersManager);
            if (proceduralGenerator != null) RegisterSystemComponent(proceduralGenerator);
            if (assetIntegrator != null) RegisterSystemComponent(assetIntegrator);
            if (performanceThrottler != null) RegisterSystemComponent(performanceThrottler);
            if (diagnosticsManager != null) RegisterSystemComponent(diagnosticsManager);
        }

        private void InitializeCoreSystems()
        {
            // Initialize components in dependency order
            foreach (var systemType in initializationOrder)
            {
                if (systemComponents.TryGetValue(systemType, out var component) && component != null)
                {
                    try
                    {
                        if (!component.IsInitialized)
                        {
                            LogDebug($"Initializing system: {systemType.Name}");
                            component.InitializeComponent();
                            systemInitializationStatus[systemType] = component.IsInitialized;
                            
                            if (component.IsInitialized)
                            {
                                LogDebug($"Successfully initialized: {systemType.Name}");
                                systemErrors.Remove(systemType); // Clear any previous errors
                            }
                            else
                            {
                                LogWarning($"Failed to initialize: {systemType.Name}");
                                systemErrors[systemType] = "Initialization failed";
                            }
                        }
                        else
                        {
                            systemInitializationStatus[systemType] = true;
                            LogDebug($"System already initialized: {systemType.Name}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Exception initializing {systemType.Name}: {ex.Message}");
                        systemInitializationStatus[systemType] = false;
                        systemErrors[systemType] = ex.Message;
                    }
                }
                else
                {
                    LogWarning($"System component not found: {systemType.Name}");
                    systemInitializationStatus[systemType] = false;
                    systemErrors[systemType] = "Component not found";
                }
            }

            // Initialize any remaining components not in the explicit order
            foreach (var kvp in systemComponents)
            {
                if (!initializationOrder.Contains(kvp.Key) && kvp.Value != null && !kvp.Value.IsInitialized)
                {
                    try
                    {
                        LogDebug($"Initializing additional system: {kvp.Key.Name}");
                        kvp.Value.InitializeComponent();
                        systemInitializationStatus[kvp.Key] = kvp.Value.IsInitialized;
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Exception initializing {kvp.Key.Name}: {ex.Message}");
                        systemInitializationStatus[kvp.Key] = false;
                        systemErrors[kvp.Key] = ex.Message;
                    }
                }
            }
        }

        private IEnumerator SystemUpdateLoop()
        {
            var waitInterval = new WaitForSeconds(systemUpdateInterval);

            while (!isShuttingDown)
            {
                yield return waitInterval;

                if (!isInitialized || isShuttingDown) continue;

                try
                {
                    // Update procedural generators
                    var environmentState = GetCurrentEnvironmentState();
                    foreach (var generator in proceduralGenerators)
                    {
                        if (generator != null && generator.IsActive)
                        {
                            generator.UpdateGeneration(systemUpdateInterval, environmentState);
                        }
                    }

                    // Update performance monitoring
                    if (performanceThrottler != null && performanceThrottler.IsInitialized)
                    {
                        performanceThrottler.UpdatePerformanceMetrics();
                    }

                    // Update diagnostics
                    if (enableDiagnostics && diagnosticsManager != null && diagnosticsManager.IsInitialized)
                    {
                        diagnosticsManager.UpdateDiagnostics();
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in system update loop: {ex.Message}");
                    
                    if (diagnosticsManager != null)
                    {
                        diagnosticsManager.ReportSystemError("SystemUpdateLoop", ex.Message);
                    }
                }
            }
        }

        private Dictionary<string, object> GetCurrentEnvironmentState()
        {
            var state = new Dictionary<string, object>
            {
                ["timestamp"] = Time.time,
                ["frameCount"] = Time.frameCount,
                ["deltaTime"] = Time.deltaTime,
                ["systemsReady"] = AllSystemsReady
            };

            // Add player behavior data if available
            if (playerMovementTracker != null && playerMovementTracker.IsInitialized)
            {
                state["playerPosition"] = playerMovementTracker.CurrentPosition;
                state["playerVelocity"] = playerMovementTracker.CurrentVelocity;
                state["playerSpeed"] = playerMovementTracker.CurrentSpeed;
            }

            if (playerBehaviorAnalyzer != null && playerBehaviorAnalyzer.IsInitialized)
            {
                state["behaviorPattern"] = playerBehaviorAnalyzer.CurrentBehaviorPattern;
                state["dwellTime"] = playerBehaviorAnalyzer.CurrentDwellTime;
            }

            // Add performance data if available
            if (performanceThrottler != null && performanceThrottler.IsInitialized)
            {
                state["currentFPS"] = performanceThrottler.CurrentFPS;
                state["performanceLevel"] = performanceThrottler.CurrentPerformanceLevel;
            }

            // Add configuration data if available
            if (configurationManager != null && configurationManager.CurrentConfiguration != null)
            {
                state["configurationLoaded"] = true;
                state["configurationPath"] = configurationManager.ConfigurationFilePath;
            }

            return state;
        }

        protected override void OnCleanup()
        {
            ShutdownSystems();
        }

        protected override void OnInitializationFailed(System.Exception exception)
        {
            LogError($"NeonQuest system initialization failed: {exception.Message}");
            
            if (diagnosticsManager != null)
            {
                diagnosticsManager.ReportSystemError("SystemManager", $"Initialization failed: {exception.Message}");
            }
            
            // Attempt graceful degradation
            ShutdownSystems();
        }

        /// <summary>
        /// Unity lifecycle - ensure initialization on start if enabled
        /// </summary>
        private void Start()
        {
            if (initializeOnStart && !isInitialized)
            {
                InitializeComponent();
            }
        }

        /// <summary>
        /// Unity lifecycle - ensure cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            ShutdownSystems();
        }

        /// <summary>
        /// Unity lifecycle - handle application quit
        /// </summary>
        private void OnApplicationQuit()
        {
            ShutdownSystems();
        }

        /// <summary>
        /// Unity lifecycle - handle application pause (mobile/console)
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Pause system updates but don't shutdown
                if (systemUpdateCoroutine != null)
                {
                    StopCoroutine(systemUpdateCoroutine);
                    systemUpdateCoroutine = null;
                }
            }
            else if (isInitialized && !isShuttingDown)
            {
                // Resume system updates
                if (systemUpdateCoroutine == null)
                {
                    systemUpdateCoroutine = StartCoroutine(SystemUpdateLoop());
                }
            }
        }

        /// <summary>
        /// Gets system health status for diagnostics
        /// </summary>
        public Dictionary<string, object> GetSystemHealthStatus()
        {
            var health = new Dictionary<string, object>
            {
                ["allSystemsReady"] = AllSystemsReady,
                ["isInitialized"] = isInitialized,
                ["isShuttingDown"] = isShuttingDown,
                ["systemCount"] = systemComponents.Count,
                ["updateLoopActive"] = systemUpdateCoroutine != null,
                ["configurationLoaded"] = configurationManager?.CurrentConfiguration != null
            };

            // Add individual system status
            var systemStatus = new Dictionary<string, bool>();
            foreach (var kvp in systemInitializationStatus)
            {
                systemStatus[kvp.Key.Name] = kvp.Value;
            }
            health["systemStatus"] = systemStatus;

            // Add system errors if any
            if (systemErrors.Count > 0)
            {
                var errors = new Dictionary<string, string>();
                foreach (var kvp in systemErrors)
                {
                    errors[kvp.Key.Name] = kvp.Value;
                }
                health["systemErrors"] = errors;
            }

            return health;
        }
    }
}