using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NeonQuest.Core;

namespace NeonQuest.Assets
{
    /// <summary>
    /// Manages asset loading, pooling, and procedural variations for Neon Underground assets
    /// </summary>
    public class AssetIntegrator : MonoBehaviour, IAssetIntegrator
    {
        [Header("Pool Configuration")]
        [SerializeField] private int defaultPoolSize = 5;
        [SerializeField] private int maxPoolSize = 50;
        [SerializeField] private float memoryThreshold = 100f; // MB
        [SerializeField] private bool enableAutomaticCleanup = true;
        [SerializeField] private float cleanupInterval = 30f; // seconds

        [Header("Asset Loading")]
        [SerializeField] private bool validateIntegrityOnLoad = true;
        [SerializeField] private bool logAssetOperations = false;

        private Dictionary<string, AssetReference> loadedAssets;
        private Dictionary<string, ObjectPool> objectPools;
        private Dictionary<GameObject, AssetReference> instanceToAssetMap;
        private Transform poolContainer;
        private float lastCleanupTime;

        public float CurrentMemoryUsage => CalculateCurrentMemoryUsage();

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            loadedAssets = new Dictionary<string, AssetReference>();
            objectPools = new Dictionary<string, ObjectPool>();
            instanceToAssetMap = new Dictionary<GameObject, AssetReference>();

            // Create container for all pools
            var containerGO = new GameObject("AssetIntegrator_Pools");
            containerGO.transform.SetParent(transform);
            poolContainer = containerGO.transform;

            lastCleanupTime = Time.time;
        }

        private void Update()
        {
            if (enableAutomaticCleanup && Time.time - lastCleanupTime > cleanupInterval)
            {
                PerformCleanup(memoryThreshold);
                lastCleanupTime = Time.time;
            }
        }

        public async Task<GameObject> LoadAssetAsync(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"[AssetIntegrator] Invalid asset path: {assetPath}");
                return null;
            }

            // Check if already loaded
            if (loadedAssets.ContainsKey(assetPath))
            {
                return loadedAssets[assetPath].Prefab;
            }

            try
            {
                // Load the asset using Unity's Resources system
                // In a real implementation, this would use Addressables or AssetBundles
                var prefab = await LoadAssetFromPath(assetPath);
                
                if (prefab == null)
                {
                    Debug.LogError($"[AssetIntegrator] Failed to load asset: {assetPath}");
                    return null;
                }

                // Create asset reference and capture integrity data
                var assetRef = new AssetReference(assetPath, prefab);
                
                // Detect and configure variation points
                DetectVariationPoints(assetRef);
                
                // Store the loaded asset
                loadedAssets[assetPath] = assetRef;

                if (logAssetOperations)
                {
                    Debug.Log($"[AssetIntegrator] Loaded asset: {assetPath}");
                }

                return prefab;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AssetIntegrator] Exception loading asset {assetPath}: {ex.Message}");
                return null;
            }
        }

        private async Task<GameObject> LoadAssetFromPath(string assetPath)
        {
            // Simulate async loading - in real implementation this would be Addressables
            await Task.Yield();
            
            // Try to load from Resources folder
            var prefab = Resources.Load<GameObject>(assetPath);
            
            if (prefab == null)
            {
                // Try alternative loading methods or create placeholder
                Debug.LogWarning($"[AssetIntegrator] Asset not found in Resources: {assetPath}, creating placeholder");
                return CreatePlaceholderAsset(assetPath);
            }

            return prefab;
        }

        private GameObject CreatePlaceholderAsset(string assetPath)
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = $"Placeholder_{System.IO.Path.GetFileNameWithoutExtension(assetPath)}";
            
            // Make it visually distinct
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.magenta;
            }

            return placeholder;
        }

        public GameObject InstantiateAsset(GameObject assetReference, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (assetReference == null)
            {
                Debug.LogError("[AssetIntegrator] Cannot instantiate null asset reference");
                return null;
            }

            // Find the asset reference data
            var assetRef = FindAssetReference(assetReference);
            if (assetRef == null)
            {
                Debug.LogWarning($"[AssetIntegrator] Asset reference not found for {assetReference.name}, creating direct instance");
                return Instantiate(assetReference, position, rotation, parent);
            }

            // Get or create object pool
            var pool = GetOrCreatePool(assetRef);
            
            // Get instance from pool
            var instance = pool.Get(position, rotation, parent);
            
            // Track the instance
            instanceToAssetMap[instance] = assetRef;

            if (logAssetOperations)
            {
                Debug.Log($"[AssetIntegrator] Instantiated {assetReference.name} from pool");
            }

            return instance;
        }

        public void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;

            if (instanceToAssetMap.TryGetValue(instance, out var assetRef))
            {
                var pool = GetOrCreatePool(assetRef);
                if (pool.Return(instance))
                {
                    instanceToAssetMap.Remove(instance);
                    
                    if (logAssetOperations)
                    {
                        Debug.Log($"[AssetIntegrator] Returned {instance.name} to pool");
                    }
                }
            }
            else
            {
                // Not a pooled object, destroy directly
                Destroy(instance);
            }
        }

        public void ApplyVariations(GameObject instance, Dictionary<string, object> variations)
        {
            if (instance == null || variations == null || variations.Count == 0) return;

            if (!instanceToAssetMap.TryGetValue(instance, out var assetRef))
            {
                Debug.LogWarning($"[AssetIntegrator] Cannot apply variations to non-tracked instance: {instance.name}");
                return;
            }

            foreach (var variation in assetRef.Variations)
            {
                if (variations.ContainsKey(variation.Name))
                {
                    ApplyVariation(instance, variation, variations[variation.Name]);
                }
            }
        }

        private void ApplyVariation(GameObject instance, VariationPoint variation, object value)
        {
            try
            {
                var target = FindTargetObject(instance, variation.TargetPath);
                if (target == null)
                {
                    Debug.LogWarning($"[AssetIntegrator] Variation target not found: {variation.TargetPath}");
                    return;
                }

                switch (variation.Type)
                {
                    case VariationType.MaterialColor:
                        ApplyColorVariation(target, value);
                        break;
                    case VariationType.LightIntensity:
                        ApplyLightIntensityVariation(target, value);
                        break;
                    case VariationType.Scale:
                        ApplyScaleVariation(target, value);
                        break;
                    case VariationType.Rotation:
                        ApplyRotationVariation(target, value);
                        break;
                    case VariationType.Position:
                        ApplyPositionVariation(target, value);
                        break;
                    default:
                        Debug.LogWarning($"[AssetIntegrator] Unsupported variation type: {variation.Type}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AssetIntegrator] Error applying variation {variation.Name}: {ex.Message}");
            }
        }

        public bool ValidateAssetIntegrity(GameObject instance)
        {
            if (instance == null) return false;

            if (!instanceToAssetMap.TryGetValue(instance, out var assetRef))
            {
                // Not a tracked asset, assume it's valid
                return true;
            }

            var integrityData = assetRef.IntegrityData;
            
            // Check child count
            if (instance.transform.childCount != integrityData.OriginalChildCount)
            {
                Debug.LogWarning($"[AssetIntegrator] Child count mismatch for {instance.name}. Expected: {integrityData.OriginalChildCount}, Actual: {instance.transform.childCount}");
                return false;
            }

            // Check component types
            var currentComponents = instance.GetComponents<Component>();
            var currentComponentTypes = currentComponents.Where(c => c != null).Select(c => c.GetType().Name).ToList();
            
            foreach (var expectedType in integrityData.OriginalComponentTypes)
            {
                if (!currentComponentTypes.Contains(expectedType))
                {
                    Debug.LogWarning($"[AssetIntegrator] Missing component {expectedType} on {instance.name}");
                    return false;
                }
            }

            // Check child hierarchy
            var currentHierarchy = new List<string>();
            CaptureChildHierarchy(instance.transform, "", currentHierarchy);
            
            if (currentHierarchy.Count != integrityData.ChildHierarchy.Count)
            {
                Debug.LogWarning($"[AssetIntegrator] Child hierarchy count mismatch for {instance.name}");
                return false;
            }

            return true;
        }

        public void PerformCleanup(float memoryThreshold)
        {
            float currentUsage = CurrentMemoryUsage;
            
            if (currentUsage <= memoryThreshold) return;

            int totalCleaned = 0;
            
            // Clean up pools starting with least used
            var poolsByUsage = objectPools.Values.OrderBy(p => p.ActiveCount).ToList();
            
            foreach (var pool in poolsByUsage)
            {
                int cleaned = pool.Cleanup();
                totalCleaned += cleaned;
                
                if (CurrentMemoryUsage <= memoryThreshold)
                {
                    break;
                }
            }

            if (logAssetOperations)
            {
                Debug.Log($"[AssetIntegrator] Cleanup completed. Removed {totalCleaned} objects. Memory usage: {CurrentMemoryUsage:F1} MB");
            }
        }

        public async Task PreloadAssetsAsync(List<string> assetPaths, int poolSize = 5)
        {
            var loadTasks = new List<Task>();
            
            foreach (var path in assetPaths)
            {
                loadTasks.Add(PreloadSingleAsset(path, poolSize));
            }

            await Task.WhenAll(loadTasks);
            
            if (logAssetOperations)
            {
                Debug.Log($"[AssetIntegrator] Preloaded {assetPaths.Count} assets with pool size {poolSize}");
            }
        }

        private async Task PreloadSingleAsset(string assetPath, int poolSize)
        {
            var prefab = await LoadAssetAsync(assetPath);
            if (prefab != null && loadedAssets.TryGetValue(assetPath, out var assetRef))
            {
                var pool = GetOrCreatePool(assetRef, poolSize);
                // Pool is automatically populated during creation
            }
        }

        public Dictionary<string, object> GetPoolStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalPools"] = objectPools.Count,
                ["TotalActiveObjects"] = objectPools.Values.Sum(p => p.ActiveCount),
                ["TotalAvailableObjects"] = objectPools.Values.Sum(p => p.AvailableCount),
                ["MemoryUsageMB"] = CurrentMemoryUsage,
                ["LoadedAssets"] = loadedAssets.Count
            };

            var poolDetails = new Dictionary<string, object>();
            foreach (var kvp in objectPools)
            {
                poolDetails[kvp.Key] = new Dictionary<string, object>
                {
                    ["Active"] = kvp.Value.ActiveCount,
                    ["Available"] = kvp.Value.AvailableCount,
                    ["Total"] = kvp.Value.TotalCount
                };
            }
            stats["PoolDetails"] = poolDetails;

            return stats;
        }

        // Helper methods
        private AssetReference FindAssetReference(GameObject prefab)
        {
            return loadedAssets.Values.FirstOrDefault(ar => ar.Prefab == prefab);
        }

        private ObjectPool GetOrCreatePool(AssetReference assetRef, int initialSize = -1)
        {
            if (initialSize < 0) initialSize = defaultPoolSize;
            
            if (!objectPools.TryGetValue(assetRef.AssetPath, out var pool))
            {
                pool = new ObjectPool(assetRef.Prefab, initialSize, maxPoolSize, poolContainer);
                objectPools[assetRef.AssetPath] = pool;
            }
            
            return pool;
        }

        private float CalculateCurrentMemoryUsage()
        {
            long totalBytes = 0;
            
            foreach (var pool in objectPools.Values)
            {
                totalBytes += pool.GetMemoryUsage();
            }

            return totalBytes / (1024f * 1024f); // Convert to MB
        }

        private void DetectVariationPoints(AssetReference assetRef)
        {
            if (assetRef.Prefab == null) return;

            // Auto-detect common variation points
            var renderers = assetRef.Prefab.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    var colorVariation = new VariationPoint($"Color_{renderer.name}", VariationType.MaterialColor, GetRelativePath(assetRef.Prefab.transform, renderer.transform));
                    assetRef.Variations.Add(colorVariation);
                }
            }

            var lights = assetRef.Prefab.GetComponentsInChildren<Light>();
            foreach (var light in lights)
            {
                var intensityVariation = new VariationPoint($"Intensity_{light.name}", VariationType.LightIntensity, GetRelativePath(assetRef.Prefab.transform, light.transform));
                assetRef.Variations.Add(intensityVariation);
            }
        }

        private string GetRelativePath(Transform root, Transform target)
        {
            if (root == target) return "";
            
            var path = target.name;
            var current = target.parent;
            
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }

        private GameObject FindTargetObject(GameObject root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            
            var transform = root.transform.Find(path);
            return transform?.gameObject;
        }

        private void CaptureChildHierarchy(Transform parent, string path, List<string> hierarchy)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string childPath = string.IsNullOrEmpty(path) ? child.name : $"{path}/{child.name}";
                hierarchy.Add(childPath);
                
                if (child.childCount > 0)
                {
                    CaptureChildHierarchy(child, childPath, hierarchy);
                }
            }
        }

        // Variation application methods
        private void ApplyColorVariation(GameObject target, object value)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && value is Color color)
            {
                renderer.material.color = color;
            }
        }

        private void ApplyLightIntensityVariation(GameObject target, object value)
        {
            var light = target.GetComponent<Light>();
            if (light != null && value is float intensity)
            {
                light.intensity = intensity;
            }
        }

        private void ApplyScaleVariation(GameObject target, object value)
        {
            if (value is Vector3 scale)
            {
                target.transform.localScale = scale;
            }
            else if (value is float uniformScale)
            {
                target.transform.localScale = Vector3.one * uniformScale;
            }
        }

        private void ApplyRotationVariation(GameObject target, object value)
        {
            if (value is Quaternion rotation)
            {
                target.transform.localRotation = rotation;
            }
            else if (value is Vector3 eulerAngles)
            {
                target.transform.localRotation = Quaternion.Euler(eulerAngles);
            }
        }

        private void ApplyPositionVariation(GameObject target, object value)
        {
            if (value is Vector3 position)
            {
                target.transform.localPosition = position;
            }
        }

        private void OnDestroy()
        {
            // Clean up all pools
            foreach (var pool in objectPools.Values)
            {
                pool.Destroy();
            }
            
            objectPools.Clear();
            loadedAssets.Clear();
            instanceToAssetMap.Clear();
        }
    }
}