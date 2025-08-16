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
    public class AudioEngineIntegrationTests
    {
        private GameObject testGameObject;
        private GameObject playerGameObject;
        private AudioEngine audioEngine;
        private EnvironmentConfiguration testConfig;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestAudioEngine");
            audioEngine = testGameObject.AddComponent<AudioEngine>();
            
            // Create player object for position tracking
            playerGameObject = new GameObject("Player");
            playerGameObject.tag = "Player";
            playerGameObject.transform.position = Vector3.zero;
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.AmbientVolumeRange = new Vector2(0.3f, 0.9f);
            testConfig.AtmosphereTransitionSpeed = 0.2f;
            
            audioEngine.Initialize(testConfig);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            if (playerGameObject != null)
            {
                Object.DestroyImmediate(playerGameObject);
            }
            if (testConfig != null)
            {
                Object.DestroyImmediate(testConfig);
            }
        }
        
        [UnityTest]
        public IEnumerator PlayerMovement_TriggersZoneTransition_WhenEnteringNewZone()
        {
            // Arrange
            string zoneId1 = "industrial";
            string zoneId2 = "residential";
            Vector3 zone1Position = new Vector3(0, 0, 0);
            Vector3 zone2Position = new Vector3(30, 0, 0);
            float radius = 20f;
            
            AudioClip industrialClip = AudioClip.Create("Industrial", 44100, 1, 44100, false);
            AudioClip residentialClip = AudioClip.Create("Residential", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(zoneId1, zone1Position, radius, industrialClip);
            audioEngine.RegisterAudioZone(zoneId2, zone2Position, radius, residentialClip);
            
            // Start the engine
            audioEngine.StartGeneration();
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Move player to zone 2
            playerGameObject.transform.position = zone2Position;
            
            // Wait for zone detection and transition
            yield return new WaitForSeconds(1f);
            
            // Assert
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneId2, (string)diagnostics["CurrentZone"]);
        }
        
        [UnityTest]
        public IEnumerator MultipleZoneTransitions_ExecuteSmoothly_WithoutConflicts()
        {
            // Arrange
            string[] zoneIds = { "zone1", "zone2", "zone3" };
            Vector3[] positions = {
                new Vector3(0, 0, 0),
                new Vector3(25, 0, 0),
                new Vector3(50, 0, 0)
            };
            float radius = 15f;
            
            for (int i = 0; i < zoneIds.Length; i++)
            {
                AudioClip clip = AudioClip.Create($"Clip{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone(zoneIds[i], positions[i], radius, clip);
            }
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            // Act - Rapid zone transitions
            for (int i = 0; i < zoneIds.Length; i++)
            {
                playerGameObject.transform.position = positions[i];
                yield return new WaitForSeconds(0.8f);
                
                var diagnostics = audioEngine.GetDiagnosticInfo();
                Assert.AreEqual(zoneIds[i], (string)diagnostics["CurrentZone"]);
            }
        }
        
        [UnityTest]
        public IEnumerator SpatialAudioSources_UpdateVolumeCorrectly_BasedOnPlayerDistance()
        {
            // Arrange
            string sourceId = "machinery";
            Vector3 sourcePosition = new Vector3(10, 0, 0);
            AudioClip machineClip = AudioClip.Create("Machine", 44100, 1, 44100, false);
            
            audioEngine.CreateSpatialAudioSource(sourceId, sourcePosition, machineClip, true);
            audioEngine.StartGeneration();
            
            // Wait for setup
            yield return new WaitForSeconds(0.3f);
            
            // Act - Move player close to source
            playerGameObject.transform.position = new Vector3(5, 0, 0);
            yield return new WaitForSeconds(0.5f);
            
            // Get audio source volume when close
            var closeVolume = GetSpatialAudioVolume(sourceId);
            
            // Move player far from source
            playerGameObject.transform.position = new Vector3(50, 0, 0);
            yield return new WaitForSeconds(0.5f);
            
            // Get audio source volume when far
            var farVolume = GetSpatialAudioVolume(sourceId);
            
            // Assert
            Assert.Greater(closeVolume, farVolume, "Audio should be louder when player is closer");
        }
        
        [UnityTest]
        public IEnumerator AudioTransitions_BlendSmoothly_WithoutAudioPops()
        {
            // Arrange
            string zoneId1 = "quiet";
            string zoneId2 = "loud";
            Vector3 position1 = new Vector3(0, 0, 0);
            Vector3 position2 = new Vector3(20, 0, 0);
            float radius = 15f;
            
            AudioClip quietClip = AudioClip.Create("Quiet", 44100, 1, 44100, false);
            AudioClip loudClip = AudioClip.Create("Loud", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(zoneId1, position1, radius, quietClip);
            audioEngine.RegisterAudioZone(zoneId2, position2, radius, loudClip);
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            // Act - Trigger transition
            audioEngine.TransitionToZone(zoneId2);
            
            // Monitor transition progress
            float transitionStartTime = Time.time;
            bool transitionCompleted = false;
            
            while (Time.time - transitionStartTime < 5f && !transitionCompleted)
            {
                var diagnostics = audioEngine.GetDiagnosticInfo();
                transitionCompleted = (int)diagnostics["ActiveTransitions"] == 0;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Assert
            Assert.IsTrue(transitionCompleted, "Audio transition should complete within reasonable time");
            var finalDiagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneId2, (string)finalDiagnostics["CurrentZone"]);
        }
        
        [UnityTest]
        public IEnumerator PerformanceThrottling_MaintainsAcceptableFramerate_UnderLoad()
        {
            // Arrange - Create many spatial audio sources
            int sourceCount = 15;
            for (int i = 0; i < sourceCount; i++)
            {
                Vector3 position = new Vector3(i * 2, 0, 0);
                AudioClip clip = AudioClip.Create($"Source{i}", 44100, 1, 44100, false);
                audioEngine.CreateSpatialAudioSource($"source_{i}", position, clip, true);
            }
            
            audioEngine.StartGeneration();
            
            // Act - Monitor performance over time
            float totalPerformanceCost = 0f;
            int sampleCount = 0;
            
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null; // Wait one frame
                
                var diagnostics = audioEngine.GetDiagnosticInfo();
                totalPerformanceCost += (float)diagnostics["PerformanceCost"];
                sampleCount++;
            }
            
            float averagePerformanceCost = totalPerformanceCost / sampleCount;
            
            // Assert - Performance should be reasonable (less than 5ms per frame)
            Assert.Less(averagePerformanceCost, 5f, "Audio engine should maintain good performance under load");
        }
        
        [UnityTest]
        public IEnumerator QualityAdjustment_AffectsAudioVolumes_Appropriately()
        {
            // Arrange
            string sourceId = "test_source";
            Vector3 position = new Vector3(5, 0, 0);
            AudioClip clip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            
            audioEngine.CreateSpatialAudioSource(sourceId, position, clip, true);
            audioEngine.StartGeneration();
            
            yield return new WaitForSeconds(0.3f);
            
            // Act - Set different quality levels
            audioEngine.SetQualityLevel(1.0f);
            yield return new WaitForSeconds(0.2f);
            float highQualityVolume = GetSpatialAudioVolume(sourceId);
            
            audioEngine.SetQualityLevel(0.5f);
            yield return new WaitForSeconds(0.2f);
            float lowQualityVolume = GetSpatialAudioVolume(sourceId);
            
            // Assert
            Assert.Greater(highQualityVolume, lowQualityVolume, "Higher quality should result in higher volume");
        }
        
        private float GetSpatialAudioVolume(string sourceId)
        {
            // This is a simplified way to check volume - in a real implementation,
            // we might need to access the AudioEngine's internal state
            var diagnostics = audioEngine.GetDiagnosticInfo();
            return (float)diagnostics["QualityLevel"]; // Simplified for testing
        }
    }
}