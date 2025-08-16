using UnityEngine;
using System.Collections.Generic;

namespace NeonQuest.Core
{
    /// <summary>
    /// Interface for environment trigger systems that evaluate conditions and dispatch generation commands
    /// </summary>
    public interface IEnvironmentTrigger
    {
        /// <summary>
        /// Evaluates trigger conditions against current player behavior and environment state
        /// </summary>
        /// <param name="playerPosition">Current player position</param>
        /// <param name="behaviorData">Player behavior analysis data</param>
        /// <param name="environmentState">Current environment state</param>
        /// <returns>True if trigger conditions are met</returns>
        bool EvaluateConditions(Vector3 playerPosition, Dictionary<string, object> behaviorData, Dictionary<string, object> environmentState);

        /// <summary>
        /// Dispatches generation commands to appropriate systems when trigger is activated
        /// </summary>
        /// <param name="triggerData">Data about what triggered the activation</param>
        void DispatchGenerationCommand(Dictionary<string, object> triggerData);

        /// <summary>
        /// Gets the priority of this trigger (higher values = higher priority)
        /// </summary>
        float Priority { get; }

        /// <summary>
        /// Gets the cooldown time in seconds before this trigger can fire again
        /// </summary>
        float Cooldown { get; }

        /// <summary>
        /// Gets whether this trigger is currently on cooldown
        /// </summary>
        bool IsOnCooldown { get; }

        /// <summary>
        /// Resets the trigger cooldown
        /// </summary>
        void ResetCooldown();
    }
}