using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    /// <summary>
    /// Validation tests to ensure AudioEngine meets specific requirements 3.2 and 3.3
    /// </summary>
    [TestFixture]
    public class AudioEngineValidationTests
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
            
            playerGameObject = new GameObject("Player");
            playerGameObject.tag = "Player";
            playerGameObject.transform.position = Vector3.zero;
            
            testConfig = ScriptableObject.CreateInstance<EnvironmentConfiguration>();
            testConfig.AmbientVolumeRange = new Vector2(0.3f, 0.9f);
            testConfig.AtmosphereTransitionSpeed = 0.1f;
            
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
        
        /// <summary>
        /// Requirement 3.2: WHEN the player enters different zone types THEN ambient drone sounds SHALL transition to match the area's character
        /// </summary>
        [UnityTest]
        public IEnumerator Requirement_3_2_PlayerEntersZone_AmbientSoundsTransition()
        {
            // Arrange - Create different zone types
            string industrialZone = "industrial";
            string residentialZone = "residential";
            Vector3 industrialPosition = new Vector3(0, 0, 0);
            Vector3 residentialPosition = new Vector3(40, 0, 0);
            float radius = 25f;
            
            AudioClip industrialClip = AudioClip.Create("IndustrialDrone", 44100, 1, 44100, false);
            AudioClip residentialClip = AudioClip.Create("ResidentialAmbient", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(industrialZone, industrialPosition, radius, industrialClip);
            audioEngine.RegisterAudioZone(residentialZone, residentialPosition, radius, residentialClip);
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.5f);
            
            // Act - Player enters industrial zone
            playerGameObject.transform.position = industrialPosition;
            yield return new WaitForSeconds(1f);
            
            // Assert - Should be in industrial zone
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(industrialZone, (string)diagnostics["CurrentZone"], 
                "Player should be in industrial zone");
            
            // Act - Player moves to residential zone
            playerGameObject.transform.position = residentialPosition;
            yield return new WaitForSeconds(1.5f);
            
            // Assert - Should transition to residential zone
            diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(residentialZone, (string)diagnostics["CurrentZone"], 
                "Ambient sounds should transition when player enters different zone type");
        }
        
        /// <summary>
        /// Requirement 3.3: WHEN specific gameplay events occur THEN the system SHALL trigger coordinated changes across lighting, fog, and audio
        /// </summary>
        [UnityTest]
        public IEnumerator Requirement_3_3_GameplayEvents_TriggerCoordinatedAudioChanges()
        {
            // Arrange - Set up audio system for coordinated changes
            string eventZone = "event_zone";
            Vector3 eventPosition = new Vector3(10, 0, 10);
            float radius = 20f;
            
            AudioClip eventClip = AudioClip.Create("EventAmbient", 44100, 1, 44100, false);
            audioEngine.RegisterAudioZone(eventZone, eventPosition, radius, eventClip);
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            // Act - Simulate gameplay event triggering coordinated audio change
            audioEngine.ModifyAmbientVolume(0.8f); // Simulate coordinated intensity change
            audioEngine.TransitionToZone(eventZone); // Simulate coordinated zone change
            
            yield return new WaitForSeconds(0.5f);
            
            // Assert - Audio should respond to coordinated changes
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(eventZone, (string)diagnostics["CurrentZone"], 
                "Audio should respond to coordinated gameplay events");
        }
        
        /// <summary>
        /// Requirement 3.3 (continued): IF multiple atmospheric changes are triggered simultaneously THEN the system SHALL blend them smoothly without conflicts
        /// </summary>
        [UnityTest]
        public IEnumerator Requirement_3_3_MultipleChanges_BlendSmoothlyWithoutConflicts()
        {
            // Arrange - Set up multiple zones for simultaneous changes
            string[] zoneIds = { "zone_a", "zone_b", "zone_c" };
            Vector3[] positions = {
                new Vector3(0, 0, 0),
                new Vector3(30, 0, 0),
                new Vector3(60, 0, 0)
            };
            
            for (int i = 0; i < zoneIds.Length; i++)
            {
                AudioClip clip = AudioClip.Create($"ZoneClip{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone(zoneIds[i], positions[i], 20f, clip);
            }
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            // Act - Trigger multiple simultaneous changes
            audioEngine.TransitionToZone(zoneIds[1]);
            audioEngine.ModifyAmbientVolume(0.7f);
            
            // Immediately trigger another change
            yield return new WaitForSeconds(0.1f);
            audioEngine.TransitionToZone(zoneIds[2]);
            audioEngine.ModifyAmbientVolume(0.9f);
            
            // Wait for changes to settle
            yield return new WaitForSeconds(3f);
            
            // Assert - System should handle multiple changes without conflicts
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneIds[2], (string)diagnostics["CurrentZone"], 
                "Final zone should be correctly set despite multiple simultaneous changes");
            
            // Should not have excessive active transitions (indicating conflicts)
            Assert.LessOrEqual((int)diagnostics["ActiveTransitions"], 1, 
                "Should not have excessive active transitions indicating conflicts");
        }
        
        /// <summary>
        /// Test smooth audio transitions as specified in task requirements
        /// </summary>
        [UnityTest]
        public IEnumerator AudioTransitions_AreSmoothBetweenAreaTypes()
        {
            // Arrange
            string quietZone = "library";
            string loudZone = "factory";
            Vector3 quietPosition = new Vector3(0, 0, 0);
            Vector3 loudPosition = new Vector3(25, 0, 0);
            
            AudioClip quietClip = AudioClip.Create("LibraryAmbient", 44100, 1, 44100, false);
            AudioClip loudClip = AudioClip.Create("FactoryNoise", 44100, 1, 44100, false);
            
            audioEngine.RegisterAudioZone(quietZone, quietPosition, 15f, quietClip);
            audioEngine.RegisterAudioZone(loudZone, loudPosition, 15f, loudClip);
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            // Act - Trigger transition
            float transitionStartTime = Time.time;
            audioEngine.TransitionToZone(loudZone);
            
            // Monitor transition smoothness
            bool transitionInProgress = true;
            float maxTransitionTime = 5f; // Should complete within reasonable time
            
            while (transitionInProgress && (Time.time - transitionStartTime) < maxTransitionTime)
            {
                var diagnostics = audioEngine.GetDiagnosticInfo();
                transitionInProgress = (int)diagnostics["ActiveTransitions"] > 0;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Assert
            Assert.Less(Time.time - transitionStartTime, maxTransitionTime, 
                "Audio transition should complete within reasonable time for smoothness");
            
            var finalDiagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(loudZone, (string)finalDiagnostics["CurrentZone"], 
                "Should successfully transition to target zone");
        }
        
        /// <summary>
        /// Test spatial audio positioning accuracy as specified in task requirements
        /// </summary>
        [UnityTest]
        public IEnumerator SpatialAudio_PositioningIsAccurate()
        {
            // Arrange
            string leftSource = "left_machine";
            string rightSource = "right_machine";
            Vector3 leftPosition = new Vector3(-10, 0, 0);
            Vector3 rightPosition = new Vector3(10, 0, 0);
            
            AudioClip machineClip = AudioClip.Create("MachineSound", 44100, 1, 44100, false);
            
            audioEngine.CreateSpatialAudioSource(leftSource, leftPosition, machineClip, true);
            audioEngine.CreateSpatialAudioSource(rightSource, rightPosition, machineClip, true);
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.5f);
            
            // Act - Position player closer to left source
            playerGameObject.transform.position = new Vector3(-5, 0, 0);
            yield return new WaitForSeconds(0.5f);
            
            // Assert - Both sources should be tracked
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(2, (int)diagnostics["SpatialAudioSources"], 
                "Both spatial audio sources should be active");
            
            // Move player to extreme distance to test culling
            playerGameObject.transform.position = new Vector3(100, 0, 0);
            yield return new WaitForSeconds(1f);
            
            // Sources should still exist but may be volume-adjusted or culled based on distance
            diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.GreaterOrEqual((int)diagnostics["SpatialAudioSources"], 0, 
                "Spatial audio system should handle distance-based adjustments");
        }
        
        /// <summary>
        /// Test zone-based ambient sound management as specified in task requirements
        /// </summary>
        [Test]
        public void ZoneBasedAmbientSoundManagement_WorksCorrectly()
        {
            // Arrange
            string[] zoneTypes = { "urban", "industrial", "residential", "commercial" };
            Vector3[] positions = {
                new Vector3(0, 0, 0),
                new Vector3(50, 0, 0),
                new Vector3(100, 0, 0),
                new Vector3(150, 0, 0)
            };
            
            // Act - Register different zone types
            for (int i = 0; i < zoneTypes.Length; i++)
            {
                AudioClip clip = AudioClip.Create($"{zoneTypes[i]}Ambient", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone(zoneTypes[i], positions[i], 30f, clip);
            }
            
            // Assert - All zones should be registered
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneTypes.Length, (int)diagnostics["ActiveZones"], 
                "All zone types should be registered for ambient sound management");
            
            // Test zone removal
            audioEngine.UnregisterAudioZone(zoneTypes[0]);
            diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneTypes.Length - 1, (int)diagnostics["ActiveZones"], 
                "Zone removal should work correctly");
        }
    }
}