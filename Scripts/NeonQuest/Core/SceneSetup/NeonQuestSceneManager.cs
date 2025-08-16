using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using NeonQuest.Core;
using NeonQuest.Configuration;
using NeonQuest.Assets;

namespace NeonQuest.Core.SceneSetup
{
    /// <summary>
    /// Manages Unity scene setup and integration with Neon Underground assets
    /// Handles scene-based configuration and system activation
    /// Requirements: 6.1, 6.3
    /// </summary>
    public class NeonQuestSceneManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string sceneConfigurationPath = "Scripts/NeonQuest/Configuration/Examples/default_environment.yaml";
        [SerializeField] private bool autoActivateOnSceneLoad = true;
        [SerializeField] private bool createDefaultSystemsIfMissing = true;
        
        [Header("Neon Underground Integration")]
        [SerializeField] private GameObject[] neonUndergroundPrefabs;
        [SerializeField] private Transform assetParent;
        [SerializeField] private bool preserveOriginalPrefabStructure = true;
        
        [Header("System References")]
        [SerializeField] private NeonQuestManager neonQuestManager;
        [SerializeField] private AssetIntegrator assetIntegrator;
        
        private Dictionary<string, GameObject> prefabVariants = new Dictionary<string, GameObject>();
        private List<GameObject> instantiatedAssets = new List<GameObject>();
        private bool isSceneSetupComplete = false;
        
        /// <summary>
        /// Gets whether the scene setup is complete
        /// </summary>
        public bool IsSceneSetupComplete => isSceneSetupComplete;
        
        /// <summary>
        /// Gets the available prefab variants
        /// </summary>
        public IReadOnlyDictionary<string, GameObject> PrefabVariants => prefabVariants;
        
        /// <summary>
        /// Gets the instantiated assets in the scene
        /// </summary>
        public IReadOnlyList<GameObject> InstantiatedAssets => instantiatedAssets.AsReadOnly();

        private void Awake()
        {
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Start()
        {
            if (autoActivateOnSceneLoad)
            {
                StartCoroutine(SetupSceneCoroutine());
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Sets up the scene with NeonQuest systems and Neon Underground assets
        /// </summary>
        public void SetupScene()
        {
            StartCoroutine(SetupSceneCoroutine());
        }

        private IEnumerator SetupSceneCoroutine()
        {
            Debug.Log("Starting NeonQuest scene setup...");
            
            try
            {
                // Step 1: Ensure NeonQuestManager exists and is initialized
                yield return StartCoroutine(EnsureNeonQuestManager());
                
                // Step 2: Setup asset integration
                yield return StartCoroutine(SetupAssetIntegration());
                
                // Step 3: Create prefab variants for procedural generation
                yield return StartCoroutine(CreatePrefabVariants());
                
                // Step 4: Configure scene-specific settings
                yield return StartCoroutine(ConfigureSceneSettings());
                
                // Step 5: Validate scene setup
                yield return StartCoroutine(ValidateSceneSetup());
                
                isSceneSetupComplete = true;
                Debug.Log("NeonQuest scene setup complete!");
                
                // Notify systems that scene is ready
                OnSceneSetupComplete();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to setup NeonQuest scene: {ex.Message}");
                isSceneSetupComplete = false;
            }
        }

        private IEnumerator EnsureNeonQuestManager()
        {
            if (neonQuestManager == null)
            {
                neonQuestManager = FindObjectOfType<NeonQuestManager>();
            }
            
            if (neonQuestManager == null && createDefaultSystemsIfMissing)
            {
                Debug.Log("Creating default NeonQuestManager for scene");
                var managerGO = new GameObject("NeonQuestManager");
                neonQuestManager = managerGO.AddComponent<NeonQuestManager>();
            }
            
            if (neonQuestManager != null && !neonQuestManager.IsInitialized)
            {
                Debug.Log("Initializing NeonQuestManager...");
                neonQuestManager.InitializeComponent();
                
                // Wait for initialization to complete
                while (!neonQuestManager.IsInitialized)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            yield return null;
        }

        private IEnumerator SetupAssetIntegration()
        {
            if (assetIntegrator == null && neonQuestManager != null)
            {
                assetIntegrator = neonQuestManager.AssetIntegrator as AssetIntegrator;
            }
            
            if (assetIntegrator == null && createDefaultSystemsIfMissing)
            {
                Debug.Log("Creating default AssetIntegrator for scene");
                var integratorGO = new GameObject("AssetIntegrator");
                if (assetParent != null)
                {
                    integratorGO.transform.SetParent(assetParent);
                }
                assetIntegrator = integratorGO.AddComponent<AssetIntegrator>();
                
                if (!assetIntegrator.IsInitialized)
                {
                    assetIntegrator.InitializeComponent();
                }
            }
            
            yield return null;
        }

        private IEnumerator CreatePrefabVariants()
        {
            if (neonUndergroundPrefabs == null || neonUndergroundPrefabs.Length == 0)
            {
                Debug.LogWarning("No Neon Underground prefabs assigned for variant creation");
                yield break;
            }
            
            Debug.Log($"Creating prefab variants for {neonUndergroundPrefabs.Length} Neon Underground assets...");
            
            foreach (var prefab in neonUndergroundPrefabs)
            {
                if (prefab == null) continue;
                
                try
                {
                    var variant = CreateProceduralVariant(prefab);
                    if (variant != null)
                    {
                        prefabVariants[prefab.name] = variant;
                        Debug.Log($"Created procedural variant for: {prefab.name}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to create variant for {prefab.name}: {ex.Message}");
                }
                
                yield return null; // Spread work across frames
            }
            
            Debug.Log($"Created {prefabVariants.Count} prefab variants");
        }

        private GameObject CreateProceduralVariant(GameObject originalPrefab)
        {
            if (originalPrefab == null) return null;
            
            // Create a variant that preserves the original structure (Requirement 6.1)
            var variant = Instantiate(originalPrefab);
            variant.name = $"{originalPrefab.name}_ProceduralVariant";
            
            if (assetParent != null)
            {
                variant.transform.SetParent(assetParent);
            }
            
            // Add procedural generation compatibility components
            var proceduralComponent = variant.GetComponent<NeonQuestComponent>();
            if (proceduralComponent == null)
            {
                proceduralComponent = variant.AddComponent<NeonQuestComponent>();
            }
            
            // Preserve original mesh and material configurations (Requirement 6.1)
            if (preserveOriginalPrefabStructure)
            {
                PreserveOriginalStructure(variant, originalPrefab);
            }
            
            // Add to asset integrator if available
            if (assetIntegrator != null)
            {
                var assetReference = new AssetReference
                {
                    AssetPath = originalPrefab.name,
                    Prefab = originalPrefab,
                    Variations = new List<VariationPoint>(),
                    Properties = new Dictionary<string, object>
                    {
                        ["isProceduralVariant"] = true,
                        ["originalPrefab"] = originalPrefab.name,
                        ["preserveStructure"] = preserveOriginalPrefabStructure
                    }
                };
                
                // Register with asset integrator
                try
                {
                    assetIntegrator.RegisterAssetReference(assetReference);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to register asset reference for {originalPrefab.name}: {ex.Message}");
                }
            }
            
            // Deactivate variant (it will be activated when needed by procedural generation)
            variant.SetActive(false);
            instantiatedAssets.Add(variant);
            
            return variant;
        }

        private void PreserveOriginalStructure(GameObject variant, GameObject original)
        {
            // Ensure all mesh renderers and materials are preserved
            var originalRenderers = original.GetComponentsInChildren<Renderer>();
            var variantRenderers = variant.GetComponentsInChildren<Renderer>();
            
            for (int i = 0; i < Mathf.Min(originalRenderers.Length, variantRenderers.Length); i++)
            {
                if (originalRenderers[i] != null && variantRenderers[i] != null)
                {
                    // Preserve materials
                    variantRenderers[i].materials = originalRenderers[i].materials;
                    
                    // Preserve mesh if it's a MeshRenderer
                    if (originalRenderers[i] is MeshRenderer originalMesh && variantRenderers[i] is MeshRenderer variantMesh)
                    {
                        var originalFilter = originalRenderers[i].GetComponent<MeshFilter>();
                        var variantFilter = variantRenderers[i].GetComponent<MeshFilter>();
                        
                        if (originalFilter != null && variantFilter != null)
                        {
                            variantFilter.mesh = originalFilter.mesh;
                        }
                    }
                }
            }
            
            // Preserve colliders
            var originalColliders = original.GetComponentsInChildren<Collider>();
            var variantColliders = variant.GetComponentsInChildren<Collider>();
            
            for (int i = 0; i < Mathf.Min(originalColliders.Length, variantColliders.Length); i++)
            {
                if (originalColliders[i] != null && variantColliders[i] != null)
                {
                    // Copy collider properties
                    variantColliders[i].isTrigger = originalColliders[i].isTrigger;
                    variantColliders[i].material = originalColliders[i].material;
                }
            }
        }

        private IEnumerator ConfigureSceneSettings()
        {
            if (neonQuestManager != null && !string.IsNullOrEmpty(sceneConfigurationPath))
            {
                Debug.Log($"Applying scene-specific configuration: {sceneConfigurationPath}");
                
                // Update configuration path for this scene
                var configManager = neonQuestManager.ConfigurationManager;
                if (configManager != null)
                {
                    configManager.SetConfigurationFilePath(sceneConfigurationPath);
                    
                    // Wait for configuration to load
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            yield return null;
        }

        private IEnumerator ValidateSceneSetup()
        {
            var validationErrors = new List<string>();
            
            // Validate NeonQuestManager
            if (neonQuestManager == null)
            {
                validationErrors.Add("NeonQuestManager not found or created");
            }
            else if (!neonQuestManager.IsInitialized)
            {
                validationErrors.Add("NeonQuestManager failed to initialize");
            }
            
            // Validate AssetIntegrator
            if (assetIntegrator == null)
            {
                validationErrors.Add("AssetIntegrator not found or created");
            }
            
            // Validate prefab variants
            if (neonUndergroundPrefabs != null && neonUndergroundPrefabs.Length > 0 && prefabVariants.Count == 0)
            {
                validationErrors.Add("No prefab variants were created despite having source prefabs");
            }
            
            if (validationErrors.Count > 0)
            {
                Debug.LogWarning($"Scene setup validation found {validationErrors.Count} issues:");
                foreach (var error in validationErrors)
                {
                    Debug.LogWarning($"  - {error}");
                }
            }
            else
            {
                Debug.Log("Scene setup validation passed successfully");
            }
            
            yield return null;
        }

        private void OnSceneSetupComplete()
        {
            // Notify other systems that scene setup is complete
            if (neonQuestManager != null)
            {
                var healthStatus = neonQuestManager.GetSystemHealthStatus();
                Debug.Log($"Scene setup complete. System status: {healthStatus["allSystemsReady"]}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (autoActivateOnSceneLoad && !isSceneSetupComplete)
            {
                StartCoroutine(SetupSceneCoroutine());
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Clean up instantiated assets
            foreach (var asset in instantiatedAssets)
            {
                if (asset != null)
                {
                    DestroyImmediate(asset);
                }
            }
            
            instantiatedAssets.Clear();
            prefabVariants.Clear();
            isSceneSetupComplete = false;
        }

        /// <summary>
        /// Gets a prefab variant by name
        /// </summary>
        public GameObject GetPrefabVariant(string prefabName)
        {
            prefabVariants.TryGetValue(prefabName, out GameObject variant);
            return variant;
        }

        /// <summary>
        /// Manually adds a Neon Underground prefab for variant creation
        /// </summary>
        public void AddNeonUndergroundPrefab(GameObject prefab)
        {
            if (prefab == null) return;
            
            var prefabList = new List<GameObject>(neonUndergroundPrefabs ?? new GameObject[0]);
            if (!prefabList.Contains(prefab))
            {
                prefabList.Add(prefab);
                neonUndergroundPrefabs = prefabList.ToArray();
                
                // Create variant immediately if scene is already set up
                if (isSceneSetupComplete)
                {
                    StartCoroutine(CreateSinglePrefabVariant(prefab));
                }
            }
        }

        private IEnumerator CreateSinglePrefabVariant(GameObject prefab)
        {
            try
            {
                var variant = CreateProceduralVariant(prefab);
                if (variant != null)
                {
                    prefabVariants[prefab.name] = variant;
                    Debug.Log($"Added procedural variant for: {prefab.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create variant for {prefab.name}: {ex.Message}");
            }
            
            yield return null;
        }
    }
}