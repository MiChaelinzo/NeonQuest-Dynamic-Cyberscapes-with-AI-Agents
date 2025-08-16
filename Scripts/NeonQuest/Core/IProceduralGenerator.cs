using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeonQuest.Core
{
    /// <summary>
    /// Interface for procedural generation systems that create dynamic environment content
    /// </summary>
    public interface IProceduralGenerator
    {
        /// <summary>
        /// Initializes the generator with configuration data
        /// </summary>
        /// <param name="config">Configuration parameters for this generator</param>
        void Initialize(Dictionary<string, object> config);

        /// <summary>
        /// Generates content based on the provided parameters
        /// </summary>
        /// <param name="generationParams">Parameters controlling what and how to generate</param>
        /// <returns>Task representing the async generation operation</returns>
        Task<GameObject> GenerateAsync(Dictionary<string, object> generationParams);

        /// <summary>
        /// Updates the generator state based on current environment conditions
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        /// <param name="environmentState">Current environment state</param>
        void UpdateGeneration(float deltaTime, Dictionary<string, object> environmentState);

        /// <summary>
        /// Cleans up generated content that is no longer needed
        /// </summary>
        /// <param name="cleanupDistance">Distance beyond which content should be cleaned up</param>
        /// <param name="playerPosition">Current player position for distance calculations</param>
        void CleanupDistantContent(float cleanupDistance, Vector3 playerPosition);

        /// <summary>
        /// Gets the current performance cost of this generator (0.0 to 1.0)
        /// </summary>
        float CurrentPerformanceCost { get; }

        /// <summary>
        /// Sets the quality level for generation (affects performance vs quality trade-off)
        /// </summary>
        /// <param name="qualityLevel">Quality level from 0.0 (lowest) to 1.0 (highest)</param>
        void SetQualityLevel(float qualityLevel);

        /// <summary>
        /// Gets whether this generator is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Enables or disables this generator
        /// </summary>
        /// <param name="active">Whether the generator should be active</param>
        void SetActive(bool active);
    }
}