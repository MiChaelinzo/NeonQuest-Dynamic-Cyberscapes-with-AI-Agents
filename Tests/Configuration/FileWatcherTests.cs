using NUnit.Framework;
using System.IO;
using System.Threading;
using UnityEngine;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class FileWatcherTests
    {
        private FileWatcher _fileWatcher;
        private string _testFilePath;
        private bool _fileChangedEventFired;
        private string _changedFilePath;
        
        [SetUp]
        public void Setup()
        {
            _fileWatcher = new FileWatcher();
            _testFilePath = Path.Combine(Application.temporaryCachePath, "test_watch_file.txt");
            _fileChangedEventFired = false;
            _changedFilePath = null;
            
            _fileWatcher.FileChanged += OnFileChanged;
        }
        
        [TearDown]
        public void TearDown()
        {
            _fileWatcher?.Dispose();
            
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }
        
        private void OnFileChanged(string filePath)
        {
            _fileChangedEventFired = true;
            _changedFilePath = filePath;
        }
        
        [Test]
        public void StartWatching_ValidFilePath_ReturnsTrue()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "test content");
            
            // Act
            bool result = _fileWatcher.StartWatching(_testFilePath);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(_fileWatcher.IsWatching);
            Assert.AreEqual(Path.GetFullPath(_testFilePath), _fileWatcher.WatchedFilePath);
        }
        
        [Test]
        public void StartWatching_NonExistentFile_ReturnsTrue()
        {
            // Act
            bool result = _fileWatcher.StartWatching(_testFilePath);
            
            // Assert
            Assert.IsTrue(result); // Should still watch for file creation
            Assert.IsTrue(_fileWatcher.IsWatching);
        }
        
        [Test]
        public void StartWatching_EmptyPath_ReturnsFalse()
        {
            // Act
            bool result = _fileWatcher.StartWatching("");
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(_fileWatcher.IsWatching);
        }
        
        [Test]
        public void StartWatching_InvalidDirectory_ReturnsFalse()
        {
            // Arrange
            string invalidPath = Path.Combine("C:\\NonExistentDirectory", "test.txt");
            
            // Act
            bool result = _fileWatcher.StartWatching(invalidPath);
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(_fileWatcher.IsWatching);
        }
        
        [Test]
        public void StopWatching_WhenWatching_StopsWatching()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "test content");
            _fileWatcher.StartWatching(_testFilePath);
            
            // Act
            _fileWatcher.StopWatching();
            
            // Assert
            Assert.IsFalse(_fileWatcher.IsWatching);
        }
        
        [Test]
        public void FileChanged_WhenFileModified_FiresEvent()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "initial content");
            _fileWatcher.StartWatching(_testFilePath);
            
            // Act
            Thread.Sleep(200); // Wait for watcher to initialize
            File.WriteAllText(_testFilePath, "modified content");
            Thread.Sleep(500); // Wait for file system event
            
            // Assert
            Assert.IsTrue(_fileChangedEventFired);
            Assert.AreEqual(Path.GetFullPath(_testFilePath), _changedFilePath);
        }
        
        [Test]
        public void FileChanged_WhenFileCreated_FiresEvent()
        {
            // Arrange
            _fileWatcher.StartWatching(_testFilePath);
            
            // Act
            Thread.Sleep(200); // Wait for watcher to initialize
            File.WriteAllText(_testFilePath, "new file content");
            Thread.Sleep(500); // Wait for file system event
            
            // Assert
            Assert.IsTrue(_fileChangedEventFired);
            Assert.AreEqual(Path.GetFullPath(_testFilePath), _changedFilePath);
        }
        
        [Test]
        public void Dispose_WhenWatching_StopsWatching()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "test content");
            _fileWatcher.StartWatching(_testFilePath);
            
            // Act
            _fileWatcher.Dispose();
            
            // Assert
            Assert.IsFalse(_fileWatcher.IsWatching);
        }
    }
}