using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonQuest.Core.ErrorHandling
{
    /// <summary>
    /// Centralized logging system for NeonQuest with structured logging and error categorization
    /// </summary>
    public static class NeonQuestLogger
    {
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        public enum LogCategory
        {
            General,
            Configuration,
            AssetLoading,
            Generation,
            Performance,
            PlayerBehavior,
            AgentHooks
        }

        private static readonly Dictionary<LogCategory, string> CategoryPrefixes = new Dictionary<LogCategory, string>
        {
            { LogCategory.General, "[NEON]" },
            { LogCategory.Configuration, "[CONFIG]" },
            { LogCategory.AssetLoading, "[ASSETS]" },
            { LogCategory.Generation, "[GEN]" },
            { LogCategory.Performance, "[PERF]" },
            { LogCategory.PlayerBehavior, "[PLAYER]" },
            { LogCategory.AgentHooks, "[HOOKS]" }
        };

        private static LogLevel _minimumLogLevel = LogLevel.Info;
        private static bool _enableStackTrace = true;

        public static void SetLogLevel(LogLevel level)
        {
            _minimumLogLevel = level;
        }

        public static void EnableStackTrace(bool enable)
        {
            _enableStackTrace = enable;
        }

        public static void LogDebug(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(LogLevel.Debug, message, category, context);
        }

        public static void LogInfo(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(LogLevel.Info, message, category, context);
        }

        public static void LogWarning(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(LogLevel.Warning, message, category, context);
        }

        public static void LogError(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(LogLevel.Error, message, category, context);
        }

        public static void LogCritical(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(LogLevel.Critical, message, category, context);
        }

        public static void LogException(Exception exception, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            string message = $"Exception: {exception.Message}";
            if (_enableStackTrace)
            {
                message += $"\nStack Trace: {exception.StackTrace}";
            }
            Log(LogLevel.Error, message, category, context);
        }

        private static void Log(LogLevel level, string message, LogCategory category, UnityEngine.Object context)
        {
            if (level < _minimumLogLevel) return;

            string prefix = CategoryPrefixes.ContainsKey(category) ? CategoryPrefixes[category] : "[NEON]";
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"{timestamp} {prefix} {message}";

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(formattedMessage, context);
                    break;
            }
        }
    }
}