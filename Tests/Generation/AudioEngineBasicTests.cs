using NUnit.Framework;
using UnityEngine;
using NeonQuest.Generation;
using NeonQuest.Configuration;
using System.Collections.Generic;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class AudioEngineBasicTests
    {
        private GameObject testGameObject;
        private AudioEngine audioEngine;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestAudioEngine");
            audioEngine = testGameObject.AddComponent<AudioEngine>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void AudioEngine_CanBeCreated()
        {
            // Assert
            Assert.IsNotNull(audioEngine);
            Assert.IsTrue(audioEngine.IsActive);
        }
        
        [Test]
        public void AudioEngine_ImplementsIProceduralGenerator()
        {
            // Assert
            Assert.IsTrue(audioEngine is IProceduralGenerator);
        }
        
        [Test]
        public void AudioEngine_InitializeWithDictionary_DoesNotThrow()
        {
            // Arrange
            var config = new Dictionary<string, object>();
            
            // Act & Assert
            Assert.DoesNotThrow(() => audioEngine.Initialize(config));
        }
        
        [Test]
        public void AudioEngine_SetActive_ChangesActiveState()
        {
            // Act
            audioEngine.SetActive(false);
            
            // Assert
            Assert.IsFalse(audioEngine.IsActive);
            
            // Act
            audioEngine.SetActive(true);
            
            // Assert
            Assert.IsTrue(audioEngine.IsActive);
        }
        
        [Test]
        public void AudioEngine_SetQualityLevel_UpdatesQuality()
        {
            // Act
            audioEngine.SetQualityLevel(0.5f);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(0.5f, (float)diagnostics["QualityLevel"], 0.01f);
        }
    }
}