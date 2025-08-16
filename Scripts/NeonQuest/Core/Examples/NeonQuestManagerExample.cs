using UnityEngine;
using NeonQuest.Core;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Example script demonstrating NeonQuestManager usage and validation
    /// Shows proper system initialization and lifecycle management
    /// </summary>
    public class NeonQuestManagerExample : MonoBehaviour
    {
        [Header("Manager Configuration")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool showDebugInfo = true;
        
        private NeonQuestManager neonQuestManager;
        
        private void Start()
        {
            // Find or create NeonQuestManager
            neonQuestManager = FindObjectOfType<NeonQuestManager>();
            
            if (neonQuestManager == null)
            {
                Debug.LogWarning("NeonQuestManager not found in scene. Creating default instance.");
                CreateDefaultNeonQuestManager();
            }
            
            if (autoInitialize && !neonQuestManager.IsInitialized)
            {
                Debug.Log("Initializing NeonQuest system...");
                neonQuestManager.InitializeComponent();
            }
            
            // Subscribe to system events for monitoring
            if (showDebugInfo)
            {
                InvokeRepeating(nameof(LogSystemStatus), 2f, 5f);
            }
        }
        
        private void CreateDefaultNeonQuestManager()
        {
            var managerGO = new GameObject("NeonQuestManager");
            neonQuestManager = managerGO.AddComponent<NeonQuestManager>();
            
            Debug.Log("Created default NeonQuestManager instance");
        }
        
        private void LogSystemStatus()
        {
            if (neonQuestManager == null) return;
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            
            Debug.Log($"NeonQuest System Status:");
            Debug.Log($"  - All Systems Ready: {healthStatus["allSystemsReady"]}");
            Debug.Log($"  - Initialized: {healthStatus["isInitialized"]}");
            Debug.Log($"  - System Count: {healthStatus["systemCount"]}");
            Debug.Log($"  - Update Loop Active: {healthStatus["updateLoopActive"]}");
            Debug.Log($"  - Configuration Loaded: {healthStatus["configurationLoaded"]}");
            
            if (healthStatus.ContainsKey("systemErrors"))
            {
                Debug.LogWarning("System errors detected - check diagnostics");
            }
        }
        
        [ContextMenu("Initialize System")]
        public void InitializeSystem()
        {
            if (neonQuestManager != null && !neonQuestManager.IsInitialized)
            {
                neonQuestManager.InitializeComponent();
                Debug.Log("NeonQuest system initialized manually");
            }
        }
        
        [ContextMenu("Shutdown System")]
        public void ShutdownSystem()
        {
            if (neonQuestManager != null)
            {
                neonQuestManager.ShutdownSystems();
                Debug.Log("NeonQuest system shutdown manually");
            }
        }
        
        [ContextMenu("Show System Health")]
        public void ShowSystemHealth()
        {
            LogSystemStatus();
        }
        
        private void OnDestroy()
        {
            if (showDebugInfo)
            {
                CancelInvoke(nameof(LogSystemStatus));
            }
        }
    }
}