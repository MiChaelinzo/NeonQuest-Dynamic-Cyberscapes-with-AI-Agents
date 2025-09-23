using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using NeonQuest.Apex;

namespace Tests.Apex
{
    /// <summary>
    /// Comprehensive tests for the Quantum Supremacy Engine
    /// Validates quantum computing capabilities and supremacy achievement
    /// </summary>
    public class QuantumSupremacyEngineTests
    {
        private GameObject testObject;
        private QuantumSupremacyEngine quantumEngine;
        
        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestQuantumSupremacyEngine");
            quantumEngine = testObject.AddComponent<QuantumSupremacyEngine>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void QuantumSupremacyEngine_InitializesCorrectly()
        {
            // Act
            quantumEngine.Initialize();
            
            // Assert
            Assert.IsTrue(quantumEngine.IsInitialized);
            Assert.IsNotNull(quantumEngine);
        }
        
        [Test]
        public void QuantumSupremacyEngine_AchievesQuantumSupremacy()
        {
            // Arrange
            quantumEngine.Initialize();
            
            // Act
            var supremacyAchieved = quantumEngine.CheckQuantumSupremacy();
            
            // Assert
            Assert.IsTrue(supremacyAchieved);
        }
        
        [UnityTest]
        public IEnumerator QuantumSupremacyEngine_ProcessesQuantumComputations()
        {
            // Arrange
            quantumEngine.Initialize();
            
            // Act
            quantumEngine.StartQuantumComputation();
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            Assert.IsTrue(quantumEngine.IsProcessingQuantumComputations());
        }
        
        [Test]
        public void QuantumSupremacyEngine_HandlesQuantumEntanglement()
        {
            // Arrange
            quantumEngine.Initialize();
            
            // Act
            var entanglementStrength = quantumEngine.GetEntanglementStrength();
            
            // Assert
            Assert.GreaterOrEqual(entanglementStrength, 0.99f);
        }
        
        [Test]
        public void QuantumSupremacyEngine_MaintainsQuantumCoherence()
        {
            // Arrange
            quantumEngine.Initialize();
            
            // Act
            var coherenceTime = quantumEngine.GetCoherenceTime();
            
            // Assert
            Assert.GreaterOrEqual(coherenceTime, 1000f);
        }
    }
}