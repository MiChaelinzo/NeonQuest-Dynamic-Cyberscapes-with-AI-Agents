using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class AudioEngineTests
    {
        private GameObject testGameObject;
        private AudioEngine audioEngine;
        private EnvironmentConfiguration testConfig;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestAudioEngine");
            audioEngine = testGameObject.AddComponent<AudioEngine>();
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.AmbientVolumeRange = new Vector2(0.2f, 0.8f);
            testConfig.AtmosphereTransitionSpeed = 0.5f;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            if (testConfig != null)
            {
                Object.DestroyImmediate(testConfig);
            }
        }
        
        [Test]
        public void Initialize_WithValidConfiguration_SetsUpCorrectly()
        {
            // Act
            audioEngine.Initialize(testConfig);
            
            // Assert
            Assert.IsTrue(audioEngine.IsActive);
            Assert.AreEqual(0f, audioEngine.CurrentPerformanceCost);
        }
        
        [Test]
        public void RegisterAudioZone_WithValidParameters_AddsZoneSuccessfully()
        {
            // Arrange
            string zoneId = "test_zone";
            Vector3 position = new Vector3(10, 0, 10);
            float radius = 15f;
            AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            
            // Act
            audioEngine.RegisterAudioZone(zoneId, position, radius, testClip);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(1, (int)diagnostics["ActiveZones"]);
        }
        
        [Test]
        public void UnregisterAudioZone_WithExistingZone_RemovesZoneSuccessfully()
        {
            // Arrange
            string zoneId = "test_zone";
            Vector3 position = new Vector3(10, 0, 10);
            float radius = 15f;
            AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(zoneId, position, radius, testClip);
            
            // Act
            audioEngine.UnregisterAudioZone(zoneId);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(0, (int)diagnostics["ActiveZones"]);
        }
        
        [Test]
        public void CreateSpatialAudioSource_WithValidParameters_CreatesSourceSuccessfully()
        {
            // Arrange
            string sourceId = "test_source";
            Vector3 position = new Vector3(5, 0, 5);
            AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            
            // Act
            audioEngine.CreateSpatialAudioSource(sourceId, position, testClip, true);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(1, (int)diagnostics["SpatialAudioSources"]);
        }
        
        [Test]
        public void RemoveSpatialAudioSource_WithExistingSource_RemovesSourceSuccessfully()
        {
            // Arrange
            string sourceId = "test_source";
            Vector3 position = new Vector3(5, 0, 5);
            AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            
            audioEngine.CreateSpatialAudioSource(sourceId, position, testClip, true);
            
            // Act
            audioEngine.RemoveSpatialAudioSource(sourceId);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(0, (int)diagnostics["SpatialAudioSources"]);
        }
        
        [Test]
        public void SetActive_WithFalse_DeactivatesEngine()
        {
            // Act
            audioEngine.SetActive(false);
            
            // Assert
            Assert.IsFalse(audioEngine.IsActive);
        }
        
        [Test]
        public void SetQualityLevel_WithValidValue_UpdatesQualityCorrectly()
        {
            // Arrange
            float testQuality = 0.7f;
            
            // Act
            audioEngine.SetQualityLevel(testQuality);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(testQuality, (float)diagnostics["QualityLevel"], 0.01f);
        }
        
        [Test]
        public void SetQualityLevel_WithOutOfRangeValue_ClampsCorrectly()
        {
            // Arrange
            float testQuality = 1.5f; // Above 1.0
            
            // Act
            audioEngine.SetQualityLevel(testQuality);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(1.0f, (float)diagnostics["QualityLevel"], 0.01f);
        }
        
        [UnityTest]
        public IEnumerator TransitionToZone_WithValidZone_ExecutesTransitionSmoothly()
        {
            // Arrange
            string zoneId1 = "zone1";
            string zoneId2 = "zone2";
            Vector3 position1 = new Vector3(0, 0, 0);
            Vector3 position2 = new Vector3(20, 0, 0);
            float radius = 25f;
            
            AudioClip clip1 = AudioClip.Create("Clip1", 44100, 1, 44100, false);
            AudioClip clip2 = AudioClip.Create("Clip2", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(zoneId1, position1, radius, clip1);
            audioEngine.RegisterAudioZone(zoneId2, position2, radius, clip2);
            
            // Act
            audioEngine.TransitionToZone(zoneId2);
            
            // Wait for transition to start
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.IsTrue((int)diagnostics["ActiveTransitions"] >= 0);
            Assert.AreEqual(zoneId2, (string)diagnostics["CurrentZone"]);
        }
        
        [Test]
        public void GetDiagnosticInfo_ReturnsCompleteInformation()
        {
            // Act
            var diagnostics = audioEngine.GetDiagnosticInfo();
            
            // Assert
            Assert.IsTrue(diagnostics.ContainsKey("ActiveZones"));
            Assert.IsTrue(diagnostics.ContainsKey("ActiveTransitions"));
            Assert.IsTrue(diagnostics.ContainsKey("SpatialAudioSources"));
            Assert.IsTrue(diagnostics.ContainsKey("CurrentZone"));
            Assert.IsTrue(diagnostics.ContainsKey("PerformanceCost"));
            Assert.IsTrue(diagnostics.ContainsKey("QualityLevel"));
            Assert.IsTrue(diagnostics.ContainsKey("IsActive"));
        }
    }
}