using System;
using UnityEngine;

namespace NeonQuest.Core.ErrorHandling
{
    /// <summary>
    /// Error boundary component that wraps system operations and provides fallback behavior
    /// </summary>
    public class ErrorBoundary : MonoBehaviour
    {
        [SerializeField] private bool _enableFallbackBehavior = true;
        [SerializeField] private float _errorCooldownTime = 5.0f;
        
        private float _lastErrorTime = -1f;
        private int _errorCount = 0;
        private const int MAX_ERRORS_BEFORE_DISABLE = 5;

        public event Action<Exception> OnErrorOccurred;
        public event Action OnFallbackActivated;

        /// <summary>
        /// Executes an action with error boundary protection
        /// </summary>
        public bool TryExecute(Action action, string operationName = "Unknown Operation", 
            NeonQuestLogger.LogCategory category = NeonQuestLogger.LogCategory.General)
        {
            if (IsInCooldown())
            {
                NeonQuestLogger.LogWarning($"Operation '{operationName}' skipped due to error cooldown", category, this);
                return false;
            }

            try
            {
                action?.Invoke();
                ResetErrorCount();
                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex, operationName, category);
                return false;
            }
        }

        /// <summary>
        /// Executes a function with error boundary protection and returns result or default value
        /// </summary>
        public T TryExecute<T>(Func<T> function, T defaultValue = default(T), string operationName = "Unknown Operation",
            NeonQuestLogger.LogCategory category = NeonQuestLogger.LogCategory.General)
        {
            if (IsInCooldown())
            {
                NeonQuestLogger.LogWarning($"Operation '{operationName}' skipped due to error cooldown, returning default value", category, this);
                return defaultValue;
            }

            try
            {
                T result = function != null ? function.Invoke() : defaultValue;
                ResetErrorCount();
                return result;
            }
            catch (Exception ex)
            {
                HandleError(ex, operationName, category);
                return defaultValue;
            }
        }

        private void HandleError(Exception ex, string operationName, NeonQuestLogger.LogCategory category)
        {
            _errorCount++;
            _lastErrorTime = Time.time;

            NeonQuestLogger.LogException(ex, category, this);
            NeonQuestLogger.LogError($"Error in operation '{operationName}'. Error count: {_errorCount}/{MAX_ERRORS_BEFORE_DISABLE}", category, this);

            OnErrorOccurred?.Invoke(ex);

            if (_errorCount >= MAX_ERRORS_BEFORE_DISABLE)
            {
                NeonQuestLogger.LogCritical($"Maximum error count reached for '{operationName}'. Disabling component.", category, this);
                enabled = false;
            }
            else if (_enableFallbackBehavior)
            {
                ActivateFallback(operationName, category);
            }
        }

        private void ActivateFallback(string operationName, NeonQuestLogger.LogCategory category)
        {
            NeonQuestLogger.LogInfo($"Activating fallback behavior for '{operationName}'", category, this);
            OnFallbackActivated?.Invoke();
        }

        private bool IsInCooldown()
        {
            return _lastErrorTime > 0 && (Time.time - _lastErrorTime) < _errorCooldownTime;
        }

        private void ResetErrorCount()
        {
            if (_errorCount > 0)
            {
                _errorCount = 0;
                NeonQuestLogger.LogInfo("Error count reset after successful operation", NeonQuestLogger.LogCategory.General, this);
            }
        }

        public void ResetErrorBoundary()
        {
            _errorCount = 0;
            _lastErrorTime = -1f;
            enabled = true;
            NeonQuestLogger.LogInfo("Error boundary manually reset", NeonQuestLogger.LogCategory.General, this);
        }
    }
}