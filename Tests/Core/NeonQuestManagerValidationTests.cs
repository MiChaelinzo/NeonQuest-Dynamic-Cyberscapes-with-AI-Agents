using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Core;
using NeonQuest.Configuration;

namespace Tests.Core
{
    /// <summary>
    /// Validation tests for NeonQuestManager main controller functionality
    /// Tests Requirements: 4.1, 4.3 - System initialization and configuration handling
    /// </summary>
    public class NeonQuestManagerValidationTests
    {
        private GameObject testGameObject;
        private NeonQuestManager neonQuestManager;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("NeonQuestManagerValidationTest");
            neonQuestManager = testGameObject.AddComponent<NeonQuestManager>();
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
        public void NeonQuestManager_Initialization_CreatesRequiredSystems()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsTrue(neonQuestManager.IsInitialized, "Manager should be initialized");
            Assert.IsNotNull(neonQuestManager.ConfigurationManager, "Configuration manager should be created");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsNotNull(healthStatus, "Health status should be available");
            Assert.IsTrue(healthStatus.ContainsKey("isInitialized"), "Health status should contain initialization flag");
        }

        [Test]
        public void NeonQuestManager_ConfigurationLoading_HandlesYAMLSpecs()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            
            // Act - Test configuration update (Requirement 4.1)
            var testConfig = new Dictionary<string, object>
            {
                ["corridorGenerationDistance"] = 50.0f,
                ["performanceThrottleThreshold"] = 60.0f,
                ["neonResponseDistance"] = 5.0f
            };
            
            // This should not throw exceptions
            Assert.DoesNotThrow(() => neonQuestManager.UpdateSystemConfiguration(testConfig));
            
            // Assert
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["isInitialized"], "System should remain initialized after configuration update");
        }

        [Test]
        public void NeonQuestManager_ErrorHandling_FallsBackGracefully()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            
            // Act - Test error handling with invalid configuration (Requirement 4.3)
            var invalidConfig = new Dictionary<string, object>
            {
                ["invalidParameter"] = "invalid_value",
                ["corridorGenerationDistance"] = -1.0f // Invalid negative value
            };
            
            // This should not crash the system
            Assert.DoesNotThrow(() => neonQuestManager.UpdateSystemConfiguration(invalidConfig));
            
            // Assert - System should still be functional
            Assert.IsTrue(neonQuestManager.IsInitialized, "System should remain initialized despite invalid config");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["isInitialized"], "Health status should show system is still initialized");
        }

        [Test]
        public void NeonQuestManager_SystemLifecycle_HandlesGracefulShutdown()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            Assert.IsTrue(neonQuestManager.IsInitialized, "System should be initialized");

            // Act
            neonQuestManager.ShutdownSystems();

            // Assert
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsFalse((bool)healthStatus["allSystemsReady"], "Systems should not be ready after shutdown");
            Assert.IsFalse((bool)healthStatus["updateLoopActive"], "Update loop should not be active after shutdown");
        }

        [Test]
        public void NeonQuestManager_DependencyInjection_RegistersSystemComponents()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue(healthStatus.ContainsKey("systemCount"), "Should track system component count");
            
            var systemCount = (int)healthStatus["systemCount"];
            Assert.GreaterOrEqual(systemCount, 1, "Should have at least the configuration manager registered");
        }

        [Test]
        public void NeonQuestManager_SystemCoordination_EstablishesProperConnections()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsNotNull(neonQuestManager.ConfigurationManager, "Configuration manager should be accessible");
            
            // Verify that the manager can provide system health information
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue(healthStatus.ContainsKey("systemStatus"), "Should provide individual system status");
        }

        [UnityTest]
        public IEnumerator NeonQuestManager_SystemUpdateLoop_RunsWithoutErrors()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            
            // Act - Let the system run for a brief period
            yield return new WaitForSeconds(0.2f);

            // Assert
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["isInitialized"], "System should remain initialized during updates");
            
            // No exceptions should be thrown during the update loop
            LogAssert.NoUnexpectedReceived();
        }
    }
}