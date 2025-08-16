using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NeonQuest.Generation;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Generation
{
    [TestFixture]
    public class AudioEnginePerformanceTests
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
        
        [Test]
        public void AudioZoneRegistration_PerformanceTest_HandlesLargeNumberOfZones()
        {
            // Arrange
            int zoneCount = 100;
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < zoneCount; i++)
            {
                Vector3 position = new Vector3(i * 10, 0, 0);
                AudioClip clip = AudioClip.Create($"Zone{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone($"zone_{i}", position, 15f, clip);
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Zone registration should be fast");
            
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneCount, (int)diagnostics["ActiveZones"]);
        }
        
        [Test]
        public void SpatialAudioCreation_PerformanceTest_HandlesMaximumSources()
        {
            // Arrange
            int maxSources = 20; // Based on AudioEngine's maxAudioSources
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < maxSources; i++)
            {
                Vector3 position = new Vector3(i * 2, 0, 0);
                AudioClip clip = AudioClip.Create($"Source{i}", 44100, 1, 44100, false);
                audioEngine.CreateSpatialAudioSource($"source_{i}", position, clip, true);
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 200, "Spatial audio creation should be efficient");
            
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(maxSources, (int)diagnostics["SpatialAudioSources"]);
        }
        
        [UnityTest]
        public IEnumerator AudioUpdateLoop_PerformanceTest_MaintainsLowCPUUsage()
        {
            // Arrange - Create moderate load
            int zoneCount = 10;
            int sourceCount = 15;
            
            for (int i = 0; i < zoneCount; i++)
            {
                Vector3 position = new Vector3(i * 15, 0, 0);
                AudioClip clip = AudioClip.Create($"Zone{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone($"zone_{i}", position, 20f, clip);
            }
            
            for (int i = 0; i < sourceCount; i++)
            {
                Vector3 position = new Vector3(i * 3, 0, 0);
                AudioClip clip = AudioClip.Create($"Source{i}", 44100, 1, 44100, false);
                audioEngine.CreateSpatialAudioSource($"source_{i}", position, clip, true);
            }
            
            audioEngine.StartGeneration();
            
            // Act - Monitor performance over multiple frames
            yield return new WaitForSeconds(0.5f); // Let it stabilize
            
            float totalPerformanceCost = 0f;
            int sampleCount = 0;
            
            for (int i = 0; i < 60; i++) // Monitor for 60 frames
            {
                yield return null;
                
                var diagnostics = audioEngine.GetDiagnosticInfo();
                totalPerformanceCost += (float)diagnostics["PerformanceCost"];
                sampleCount++;
            }
            
            float averagePerformanceCost = totalPerformanceCost / sampleCount;
            
            // Assert - Should maintain good performance (less than 2ms per frame)
            Assert.Less(averagePerformanceCost, 2f, 
                $"Audio engine should maintain low CPU usage. Average: {averagePerformanceCost}ms");
        }
        
        [UnityTest]
        public IEnumerator ZoneTransitions_PerformanceTest_HandleRapidTransitions()
        {
            // Arrange
            string[] zoneIds = { "zone1", "zone2", "zone3", "zone4", "zone5" };
            Vector3[] positions = {
                new Vector3(0, 0, 0),
                new Vector3(30, 0, 0),
                new Vector3(60, 0, 0),
                new Vector3(90, 0, 0),
                new Vector3(120, 0, 0)
            };
            
            for (int i = 0; i < zoneIds.Length; i++)
            {
                AudioClip clip = AudioClip.Create($"Clip{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone(zoneIds[i], positions[i], 25f, clip);
            }
            
            audioEngine.StartGeneration();
            yield return new WaitForSeconds(0.3f);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Rapid zone transitions
            for (int i = 0; i < zoneIds.Length; i++)
            {
                audioEngine.TransitionToZone(zoneIds[i]);
                yield return new WaitForSeconds(0.1f); // Very rapid transitions
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Rapid transitions should complete quickly");
            
            // Verify final state
            var diagnostics = audioEngine.GetDiagnosticInfo();
            Assert.AreEqual(zoneIds[zoneIds.Length - 1], (string)diagnostics["CurrentZone"]);
        }
        
        [UnityTest]
        public IEnumerator SpatialAudioCulling_PerformanceTest_RemovesDistantSources()
        {
            // Arrange - Create sources at various distances
            int sourceCount = 20;
            float cullingDistance = 50f; // Based on AudioEngine's default
            
            for (int i = 0; i < sourceCount; i++)
            {
                // Place some sources within culling distance, some beyond
                float distance = i < sourceCount / 2 ? 25f : 75f;
                Vector3 position = new Vector3(distance, 0, 0);
                AudioClip clip = AudioClip.Create($"Source{i}", 44100, 1, 44100, false);
                audioEngine.CreateSpatialAudioSource($"source_{i}", position, clip, true);
            }
            
            audioEngine.StartGeneration();
            
            // Wait for initial setup
            yield return new WaitForSeconds(0.5f);
            
            // Act - Move player to trigger culling
            playerGameObject.transform.position = Vector3.zero;
            
            // Wait for culling to occur
            yield return new WaitForSeconds(1f);
            
            // Assert - Distant sources should be culled
            var diagnostics = audioEngine.GetDiagnosticInfo();
            int remainingSources = (int)diagnostics["SpatialAudioSources"];
            
            Assert.Less(remainingSources, sourceCount, "Distant audio sources should be culled");
            Assert.Greater(remainingSources, 0, "Some audio sources should remain");
        }
        
        [Test]
        public void AudioEngineCleanup_PerformanceTest_CleansUpResourcesEfficiently()
        {
            // Arrange - Create resources
            int resourceCount = 50;
            
            for (int i = 0; i < resourceCount; i++)
            {
                Vector3 position = new Vector3(i * 5, 0, 0);
                AudioClip clip = AudioClip.Create($"Resource{i}", 44100, 1, 44100, false);
                audioEngine.RegisterAudioZone($"zone_{i}", position, 10f, clip);
                audioEngine.CreateSpatialAudioSource($"source_{i}", position, clip, true);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Cleanup
            audioEngine.SetActive(false);
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, "Cleanup should be fast");
            Assert.IsFalse(audioEngine.IsActive);
        }
        
        [UnityTest]
        public IEnumerator MemoryUsage_PerformanceTest_DoesNotLeakMemory()
        {
            // Arrange
            long initialMemory = System.GC.GetTotalMemory(true);
            
            // Act - Create and destroy many audio sources
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Create sources
                for (int i = 0; i < 10; i++)
                {
                    Vector3 position = new Vector3(i * 3, 0, 0);
                    AudioClip clip = AudioClip.Create($"Cycle{cycle}_Source{i}", 44100, 1, 44100, false);
                    audioEngine.CreateSpatialAudioSource($"cycle_{cycle}_source_{i}", position, clip, true);
                }
                
                yield return new WaitForSeconds(0.1f);
                
                // Remove sources
                for (int i = 0; i < 10; i++)
                {
                    audioEngine.RemoveSpatialAudioSource($"cycle_{cycle}_source_{i}");
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // Force garbage collection
            System.GC.Collect();
            yield return new WaitForSeconds(0.2f);
            
            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryDifference = finalMemory - initialMemory;
            
            // Assert - Memory usage should not grow significantly
            Assert.Less(memoryDifference, 1024 * 1024, // Less than 1MB growth
                $"Memory usage should not grow significantly. Difference: {memoryDifference} bytes");
        }
    }
}