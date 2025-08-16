using System;
using UnityEngine;

namespace NeonQuest.Core.ErrorHandling
{
    /// <summary>
    /// Specialized error handler for asset loading operations
    /// </summary>
    public class AssetLoadingErrorHandler : MonoBehaviour
    {
        private ErrorBoundary _errorBoundary;
        
        [SerializeField] private bool _useAssetFallbacks = true;
        [SerializeField] private int _maxRetryAttempts = 3;
        [SerializeField] private float _retryDelay = 1.0f;

        private void Awake()
        {
            _errorBoundary = GetComponent<ErrorBoundary>();
            if (_errorBoundary == null)
            {
                _errorBoundary = gameObject.AddComponent<ErrorBoundary>();
            }

            _errorBoundary.OnErrorOccurred += HandleAssetLoadingError;
            _errorBoundary.OnFallbackActivated += ActivateAssetFallback;
        }

        /// <summary>
        /// Safely load a prefab with fallback behavior
        /// </summary>
        public GameObject LoadPrefabSafely(string assetPath)
        {
            return _errorBoundary.TryExecute(() =>
            {
                GameObject prefab = Resources.Load<GameObject>(assetPath);
                if (prefab == null)
                {
                    throw new AssetLoadException($"Failed to load prefab at path: {assetPath}");
                }
                return prefab;
            }, 
            _useAssetFallbacks ? FallbackBehaviors.GetFallbackPrefab() : null,
            $"LoadPrefab({assetPath})",
            NeonQuestLogger.LogCategory.AssetLoading);
        }

        /// <summary>
        /// Safely load a material with fallback behavior
        /// </summary>
        public Material LoadMaterialSafely(string assetPath)
        {
            return _errorBoundary.TryExecute(() =>
            {
                Material material = Resources.Load<Material>(assetPath);
                if (material == null)
                {
                    throw new AssetLoadException($"Failed to load material at path: {assetPath}");
                }
                return material;
            },
            _useAssetFallbacks ? FallbackBehaviors.GetFallbackMaterial() : null,
            $"LoadMaterial({assetPath})",
            NeonQuestLogger.LogCategory.AssetLoading);
        }

        /// <summary>
        /// Safely load an audio clip with fallback behavior
        /// </summary>
        public AudioClip LoadAudioClipSafely(string assetPath)
        {
            return _errorBoundary.TryExecute(() =>
            {
                AudioClip clip = Resources.Load<AudioClip>(assetPath);
                if (clip == null)
                {
                    throw new AssetLoadException($"Failed to load audio clip at path: {assetPath}");
                }
                return clip;
            },
            _useAssetFallbacks ? FallbackBehaviors.GetFallbackAudioClip() : null,
            $"LoadAudioClip({assetPath})",
            NeonQuestLogger.LogCategory.AssetLoading);
        }

        /// <summary>
        /// Safely instantiate a prefab with error handling
        /// </summary>
        public GameObject InstantiateSafely(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return _errorBoundary.TryExecute(() =>
            {
                if (prefab == null)
                {
                    throw new AssetLoadException("Cannot instantiate null prefab");
                }
                return Instantiate(prefab, position, rotation, parent);
            },
            null,
            $"Instantiate({prefab?.name})",
            NeonQuestLogger.LogCategory.AssetLoading);
        }

        private void HandleAssetLoadingError(Exception exception)
        {
            if (exception is AssetLoadException)
            {
                NeonQuestLogger.LogError($"Asset loading failed: {exception.Message}", 
                    NeonQuestLogger.LogCategory.AssetLoading, this);
            }
            else
            {
                NeonQuestLogger.LogError($"Unexpected error during asset loading: {exception.Message}", 
                    NeonQuestLogger.LogCategory.AssetLoading, this);
            }
        }

        private void ActivateAssetFallback()
        {
            NeonQuestLogger.LogInfo("Asset loading fallback activated - using placeholder assets", 
                NeonQuestLogger.LogCategory.AssetLoading, this);
        }

        private void OnDestroy()
        {
            if (_errorBoundary != null)
            {
                _errorBoundary.OnErrorOccurred -= HandleAssetLoadingError;
                _errorBoundary.OnFallbackActivated -= ActivateAssetFallback;
            }
        }
    }

    /// <summary>
    /// Custom exception for asset loading failures
    /// </summary>
    public class AssetLoadException : Exception
    {
        public AssetLoadException(string message) : base(message) { }
        public AssetLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
}