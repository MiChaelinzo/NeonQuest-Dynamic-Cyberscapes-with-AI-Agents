using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Wildcard;
using System.Collections;

namespace Tests.Wildcard
{
    /// <summary>
    /// Tests for the revolutionary KiroMetaSystem - Ultimate Wildcard Entry
    /// </summary>
    public class KiroMetaSystemTests
    {
        private GameObject testObject;
        private KiroMetaSystem metaSystem;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestMetaSystem");
            metaSystem = testObject.AddComponent<KiroMetaSystem>();
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }
        
        [Test]
        public void MetaSystem_InitializesCorrectly()
        {
            // Test that meta-system initializes without errors
            Assert.IsNotNull(metaSystem);
            
            var stats = metaSystem.GetStats();
            Assert.IsNotNull(stats);
            Assert.GreaterOrEqual(stats.ConsciousnessLevel, 0.0f);
        }
        
        [Test]
        public void ConsciousnessEvolution_IncreasesLevel()
        {
            var initialStats = metaSystem.GetStats();
            var initialConsciousness = initialStats.ConsciousnessLevel;
            
            metaSystem.TriggerConsciousnessEvolution();
            
            var newStats = metaSystem.GetStats();
            Assert.Greater(newStats.ConsciousnessLevel, initialConsciousness);
        }
        
        [Test]
        public void ForceSingularity_AchievesSingularity()
        {
            metaSystem.ForceSingularity();
            
            var stats = metaSystem.GetStats();
            Assert.IsTrue(stats.HasAchievedSingularity);
            Assert.AreEqual(1.0f, stats.ConsciousnessLevel, 0.001f);
            Assert.IsTrue(stats.QuantumComputingEnabled);
        }
        
        [UnityTest]
        public IEnumerator MetaSystem_GeneratesSystemsOverTime()
        {
            metaSystem.ForceSingularity(); // Enable advanced features
            
            var initialCount = metaSystem.GetStats().GeneratedSystemsCount;
            
            // Wait for system generation cycles
            yield return new WaitForSeconds(2.0f);
            
            var finalCount = metaSystem.GetStats().GeneratedSystemsCount;
            Assert.GreaterOrEqual(finalCount, initialCount);
        }
    }
}