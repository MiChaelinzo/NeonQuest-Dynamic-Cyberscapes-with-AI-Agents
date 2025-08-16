using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeonQuest.Core
{
    /// <summary>
    /// Interface for asset integration systems that handle Neon Underground assets and object pooling
    /// </summary>
    public interface IAssetIntegrator
    {
        /// <summary>
        /// Loads an asset reference while preserving its original structure
        /// </summary>
        /// <param name="assetPath">Path to the asset to load</param>
        /// <returns>Task returning the loaded asset reference</returns>
        Task<GameObject> LoadAssetAsync(string assetPath);

        /// <summary>
        /// Instantiates an asset from the pool or creates a new instance
        /// </summary>
        /// <param name="assetReference">Reference to the asset to instantiate</param>
        /// <param name="position">World position for the instantiated object</param>
        /// <param name="rotation">World rotation for the instantiated object</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The instantiated GameObject</returns>
        GameObject InstantiateAsset(GameObject assetReference, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// Returns an asset instance to the pool for reuse
        /// </summary>
        /// <param name="instance">The GameObject instance to return to pool</param>
        void ReturnToPool(GameObject instance);

        /// <summary>
        /// Applies procedural variations to an asset instance
        /// </summary>
        /// <param name="instance">The asset instance to modify</param>
        /// <param name="variations">Dictionary of variation parameters</param>
        void ApplyVariations(GameObject instance, Dictionary<string, object> variations);

        /// <summary>
        /// Validates that an asset maintains its original structure and relationships
        /// </summary>
        /// <param name="instance">The asset instance to validate</param>
        /// <returns>True if the asset structure is intact</returns>
        bool ValidateAssetIntegrity(GameObject instance);

        /// <summary>
        /// Gets the current memory usage of the asset system in MB
        /// </summary>
        float CurrentMemoryUsage { get; }

        /// <summary>
        /// Performs cleanup of unused pooled objects
        /// </summary>
        /// <param name="memoryThreshold">Memory threshold in MB that triggers aggressive cleanup</param>
        void PerformCleanup(float memoryThreshold);

        /// <summary>
        /// Preloads commonly used assets into the pool
        /// </summary>
        /// <param name="assetPaths">List of asset paths to preload</param>
        /// <param name="poolSize">Number of instances to preload for each asset</param>
        Task PreloadAssetsAsync(List<string> assetPaths, int poolSize = 5);

        /// <summary>
        /// Gets statistics about pool usage for monitoring
        /// </summary>
        /// <returns>Dictionary containing pool statistics</returns>
        Dictionary<string, object> GetPoolStatistics();
    }
}