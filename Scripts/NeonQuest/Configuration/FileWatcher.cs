using System;
using System.IO;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Configuration
{
    public class FileWatcher : IDisposable
    {
        private readonly NeonQuestLogger _logger;
        private FileSystemWatcher _watcher;
        private string _watchedFilePath;
        private DateTime _lastWriteTime;
        private bool _isDisposed;
        
        public event Action<string> FileChanged;
        
        public bool IsWatching => _watcher != null && _watcher.EnableRaisingEvents;
        public string WatchedFilePath => _watchedFilePath;
        
        public FileWatcher()
        {
            _logger = new NeonQuestLogger("FileWatcher");
        }
        
        public bool StartWatching(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogError("Cannot watch null or empty file path");
                    return false;
                }
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File does not exist: {filePath}. Will watch for creation.");
                }
                
                StopWatching();
                
                _watchedFilePath = Path.GetFullPath(filePath);
                string directory = Path.GetDirectoryName(_watchedFilePath);
                string fileName = Path.GetFileName(_watchedFilePath);
                
                if (!Directory.Exists(directory))
                {
                    _logger.LogError($"Directory does not exist: {directory}");
                    return false;
                }
                
                _watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                
                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileChanged;
                _watcher.Error += OnWatcherError;
                
                _lastWriteTime = File.Exists(_watchedFilePath) ? File.GetLastWriteTime(_watchedFilePath) : DateTime.MinValue;
                
                _logger.LogInfo($"Started watching file: {_watchedFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start watching file {filePath}: {ex.Message}");
                return false;
            }
        }
        
        public void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileChanged;
                _watcher.Created -= OnFileChanged;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
                
                _logger.LogInfo($"Stopped watching file: {_watchedFilePath}");
            }
        }
        
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce multiple rapid file change events
                if (File.Exists(e.FullPath))
                {
                    var lastWrite = File.GetLastWriteTime(e.FullPath);
                    if (lastWrite <= _lastWriteTime)
                        return;
                        
                    _lastWriteTime = lastWrite;
                }
                
                // Small delay to ensure file write is complete
                System.Threading.Thread.Sleep(100);
                
                _logger.LogDebug($"File changed: {e.FullPath}");
                FileChanged?.Invoke(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling file change event: {ex.Message}");
            }
        }
        
        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError($"FileWatcher error: {e.GetException().Message}");
            
            // Try to restart watching
            if (!string.IsNullOrEmpty(_watchedFilePath))
            {
                _logger.LogInfo("Attempting to restart file watching...");
                StartWatching(_watchedFilePath);
            }
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                StopWatching();
                _isDisposed = true;
            }
        }
    }
}