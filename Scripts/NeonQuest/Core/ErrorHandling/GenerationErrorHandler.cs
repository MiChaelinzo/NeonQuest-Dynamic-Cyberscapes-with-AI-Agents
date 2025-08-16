using System;
using UnityEngine;

namespace NeonQuest.Core.ErrorHandling
{
    /// <summary>
    /// Specialized error handler for procedural generation operations
    /// </summary>
    public class GenerationErrorHandler : MonoBehaviour
    {
        private ErrorBoundary _errorBoundary;
        
        [SerializeField] private bool _enableSafeMode = true;
        [SerializeField] private float _performanceThreshold = 16.67f; // 60 FPS target
        [SerializeField] private int _maxGenerationFailures = 3;

        private int _consecutiveFailures = 0;
        private bool _safeModeActive = false;
        private GenerationParameters _currentParameters;
        private GenerationParameters _safeParameters;

        private void Awake()
        {
            _errorBoundary = GetComponent<ErrorBoundary>();
            if (_errorBoundary == null)
            {
                _errorBoundary = gameObject.AddComponent<ErrorBoundary>();
            }

            _errorBoundary.OnErrorOccurred += HandleGenerationError;
            _errorBoundary.OnFallbackActivated += ActivateGenerationFallback;

            _safeParameters = FallbackBehaviors.GetSafeGenerationParameters();
            _currentParameters = new GenerationParameters(); // Default parameters
        }

        /// <summary>
        /// Safely execute a generation operation with performance monitoring
        /// </summary>
        public bool ExecuteGenerationSafely(Action generationAction, string operationName)
        {
            float startTime = Time.realtimeSinceStartup;
            
            bool success = _errorBoundary.TryExecute(() =>
            {
                if (_safeModeActive)
                {
                    ApplySafeModeConstraints();
                }
                
                generationAction?.Invoke();
                
                float executionTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to milliseconds
                
                if (executionTime > _performanceThreshold)
                {
                    NeonQuestLogger.LogWarning($"Generation operation '{operationName}' took {executionTime:F2}ms (threshold: {_performanceThreshold:F2}ms)", 
                        NeonQuestLogger.LogCategory.Generation, this);
                    
                    if (!_safeModeActive && _enableSafeMode)
                    {
                        ActivateSafeMode();
                    }
                }
                
            }, operationName, NeonQuestLogger.LogCategory.Generation);

            if (success)
            {
                _consecutiveFailures = 0;
                if (_safeModeActive && _consecutiveFailures == 0)
                {
                    // Consider deactivating safe mode after successful operations
                    DeactivateSafeModeIfStable();
                }
            }

            return success;
        }

        /// <summary>
        /// Safely generate objects with quantity limits
        /// </summary>
        public T[] GenerateObjectsSafely<T>(Func<T> generatorFunction, int requestedCount, string operationName) where T : class
        {
            int actualCount = _safeModeActive ? 
                Mathf.Min(requestedCount, _safeParameters.MaxObjectsPerFrame) : 
                requestedCount;

            var results = new T[actualCount];
            int successCount = 0;

            for (int i = 0; i < actualCount; i++)
            {
                T result = _errorBoundary.TryExecute(generatorFunction, null, 
                    $"{operationName}[{i}]", NeonQuestLogger.LogCategory.Generation);
                
                if (result != null)
                {
                    results[successCount] = result;
                    successCount++;
                }
            }

            if (successCount < actualCount)
            {
                NeonQuestLogger.LogWarning($"Generated {successCount}/{actualCount} objects for '{operationName}'", 
                    NeonQuestLogger.LogCategory.Generation, this);
                
                // Resize array to actual successful count
                Array.Resize(ref results, successCount);
            }

            return results;
        }

        /// <summary>
        /// Check if generation should be throttled based on current conditions
        /// </summary>
        public bool ShouldThrottleGeneration()
        {
            return _safeModeActive || Time.deltaTime > (_performanceThreshold / 1000f);
        }

        /// <summary>
        /// Get current generation parameters (safe or normal)
        /// </summary>
        public GenerationParameters GetCurrentParameters()
        {
            return _safeModeActive ? _safeParameters : _currentParameters;
        }

        private void HandleGenerationError(Exception exception)
        {
            _consecutiveFailures++;
            
            NeonQuestLogger.LogError($"Generation error (failure #{_consecutiveFailures}): {exception.Message}", 
                NeonQuestLogger.LogCategory.Generation, this);

            if (_consecutiveFailures >= _maxGenerationFailures && !_safeModeActive && _enableSafeMode)
            {
                ActivateSafeMode();
            }
        }

        private void ActivateGenerationFallback()
        {
            NeonQuestLogger.LogInfo("Generation fallback activated - reducing complexity", 
                NeonQuestLogger.LogCategory.Generation, this);
            
            if (!_safeModeActive && _enableSafeMode)
            {
                ActivateSafeMode();
            }
        }

        private void ActivateSafeMode()
        {
            _safeModeActive = true;
            NeonQuestLogger.LogWarning("Safe mode activated for procedural generation", 
                NeonQuestLogger.LogCategory.Generation, this);
        }

        private void DeactivateSafeModeIfStable()
        {
            // Only deactivate safe mode after a period of stable operation
            if (_consecutiveFailures == 0 && Time.deltaTime < (_performanceThreshold / 1000f))
            {
                _safeModeActive = false;
                NeonQuestLogger.LogInfo("Safe mode deactivated - performance stable", 
                    NeonQuestLogger.LogCategory.Generation, this);
            }
        }

        private void ApplySafeModeConstraints()
        {
            // Apply safe mode constraints to current operation
            // This could involve reducing LOD, simplifying geometry, etc.
            NeonQuestLogger.LogDebug("Applying safe mode constraints to generation", 
                NeonQuestLogger.LogCategory.Generation, this);
        }

        public void ForceActivateSafeMode()
        {
            ActivateSafeMode();
        }

        public void ForceDeactivateSafeMode()
        {
            _safeModeActive = false;
            _consecutiveFailures = 0;
            NeonQuestLogger.LogInfo("Safe mode manually deactivated", 
                NeonQuestLogger.LogCategory.Generation, this);
        }

        private void OnDestroy()
        {
            if (_errorBoundary != null)
            {
                _errorBoundary.OnErrorOccurred -= HandleGenerationError;
                _errorBoundary.OnFallbackActivated -= ActivateGenerationFallback;
            }
        }
    }
}