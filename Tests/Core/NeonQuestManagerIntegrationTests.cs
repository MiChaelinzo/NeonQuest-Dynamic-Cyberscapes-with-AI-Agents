using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;
using NeonQuest.Generation;
using NeonQuest.Assets;
using NeonQuest.Core.Diagnostics;

namespace Tests.Core
{
    /// <summary>
    /// Integration tests for NeonQuestManager system initialization and coordination
    /// Tests Requirements: 4.1, 4.3
    /// </summary>
    public class NeonQuestManagerIntegrationTests
    {
        private GameObject testGameObject;
        private NeonQuestManager neonQuestManager;
        private GameObject configManagerGO;
        private GameObject playerTrackerGO;
        private GameObject behaviorAnalyzerGO;
        private GameObject proceduralGeneratorGO;
        private GameObject assetIntegratorGO;
        private GameObject performanceThrottlerGO;
        private GameObject diagnosticsManagerGO;

        [SetUp]
        public void SetUp()
        {
            // Create main test GameObject
            testGameObject = new GameObject("NeonQuestManagerTest");
            neonQuestManager = testGameObject.AddComponent<NeonQuestManager>();

            // Create system components
            SetupSystemComponents();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            CleanupSystemComponents();
        }

        private void SetupSystemComponents()
        {
            // Configuration Manager
            configManagerGO = new GameObject("ConfigurationManager");
            configManagerGO.transform.SetParent(testGameObject.transform);
            var configManager = configManagerGO.AddComponent<ConfigurationManager>();

            // Player Movement Tracker
            playerTrackerGO = new GameObject("PlayerMovementTracker");
            playerTrackerGO.transform.SetParent(testGameObject.transform);
            var playerTracker = playerTrackerGO.AddComponent<PlayerMovementTracker>();

            // Player Behavior Analyzer
            behaviorAnalyzerGO = new GameObject("PlayerBehaviorAnalyzer");
            behaviorAnalyzerGO.transform.SetParent(testGameObject.transform);
            var behaviorAnalyzer = behaviorAnalyzerGO.AddComponent<PlayerBehaviorAnalyzer>();

            // Procedural Generator
            proceduralGeneratorGO = new GameObject("ProceduralGenerator");
            proceduralGeneratorGO.transform.SetParent(testGameObject.transform);
            var proceduralGenerator = proceduralGeneratorGO.AddComponent<ProceduralGenerator>();

            // Asset Integrator
            assetIntegratorGO = new GameObject("AssetIntegrator");
            assetIntegratorGO.transform.SetParent(testGameObject.transform);
            var assetIntegrator = assetIntegratorGO.AddComponent<AssetIntegrator>();

            // Performance Throttler
            performanceThrottlerGO = new GameObject("PerformanceThrottler");
            performanceThrottlerGO.transform.SetParent(testGameObject.transform);
            var performanceThrottler = performanceThrottlerGO.AddComponent<PerformanceThrottler>();

            // Diagnostics Manager
            diagnosticsManagerGO = new GameObject("DiagnosticsManager");
            diagnosticsManagerGO.transform.SetParent(testGameObject.transform);
            var diagnosticsManager = diagnosticsManagerGO.AddComponent<DiagnosticsManager>();

            // Assign components to manager using reflection to set private fields
            var managerType = typeof(NeonQuestManager);
            managerType.GetField("configurationManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, configManager);
            managerType.GetField("playerMovementTracker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, playerTracker);
            managerType.GetField("playerBehaviorAnalyzer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, behaviorAnalyzer);
            managerType.GetField("proceduralGenerator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, proceduralGenerator);
            managerType.GetField("assetIntegrator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, assetIntegrator);
            managerType.GetField("performanceThrottler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, performanceThrottler);
            managerType.GetField("diagnosticsManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(neonQuestManager, diagnosticsManager);
        }

        private void CleanupSystemComponents()
        {
            var gameObjects = new[] { configManagerGO, playerTrackerGO, behaviorAnalyzerGO, 
                proceduralGeneratorGO, assetIntegratorGO, performanceThrottlerGO, diagnosticsManagerGO };

            foreach (var go in gameObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void SystemInitialization_WithAllComponents_InitializesSuccessfully()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsTrue(neonQuestManager.IsInitialized, "NeonQuestManager should be initialized");
            Assert.IsTrue(neonQuestManager.AllSystemsReady, "All systems should be ready");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["allSystemsReady"], "Health status should indicate all systems ready");
            Assert.IsTrue((bool)healthStatus["isInitialized"], "Health status should indicate initialization");
            Assert.IsFalse((bool)healthStatus["isShuttingDown"], "Health status should not indicate shutdown");
        }

        [Test]
        public void SystemInitialization_WithMissingComponents_HandlesGracefully()
        {
            // Arrange - Remove some components
            Object.DestroyImmediate(assetIntegratorGO);
            assetIntegratorGO = null;

            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsTrue(neonQuestManager.IsInitialized, "NeonQuestManager should still initialize");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue(healthStatus.ContainsKey("systemErrors"), "Should report system errors");
        }

        [Test]
        public void ConfigurationManager_Integration_LoadsAndUpdatesConfiguration()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            var configManager = neonQuestManager.ConfigurationManager;
            
            // Act
            var testConfig = new Dictionary<string, object>
            {
                ["corridorGenerationDistance"] = 75.0f,
                ["performanceThrottleThreshold"] = 45.0f
            };
            neonQuestManager.UpdateSystemConfiguration(testConfig);

            // Assert
            Assert.IsNotNull(configManager, "Configuration manager should be available");
            // Configuration update should not throw exceptions
        }

        [Test]
        public void SystemComponents_Registration_RegistersAllExpectedTypes()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            var systemStatus = neonQuestManager.SystemInitializationStatus;
            
            // Check that key system types are registered
            var expectedTypes = new[]
            {
                typeof(ConfigurationManager),
                typeof(PlayerMovementTracker),
                typeof(PlayerBehaviorAnalyzer),
                typeof(ProceduralGenerator),
                typeof(AssetIntegrator),
                typeof(PerformanceThrottler),
                typeof(DiagnosticsManager)
            };

            foreach (var expectedType in expectedTypes)
            {
                Assert.IsTrue(systemStatus.ContainsKey(expectedType), 
                    $"System should register {expectedType.Name}");
            }
        }

        [Test]
        public void AssetIntegrator_Integration_IsAccessibleThroughManager()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsNotNull(neonQuestManager.AssetIntegrator, "Asset integrator should be accessible");
        }

        [Test]
        public void ProceduralGenerators_Integration_AreAccessibleThroughManager()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert
            Assert.IsNotNull(neonQuestManager.ProceduralGenerators, "Procedural generators list should be accessible");
            Assert.Greater(neonQuestManager.ProceduralGenerators.Count, 0, "Should have at least one procedural generator");
        }

        [UnityTest]
        public IEnumerator SystemUpdateLoop_WithInitializedSystems_UpdatesWithoutErrors()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            
            // Wait for initialization to complete
            yield return new WaitForSeconds(0.1f);

            // Act - Let the system run for a few update cycles
            float startTime = Time.time;
            while (Time.time - startTime < 0.5f)
            {
                yield return null;
            }

            // Assert
            Assert.IsTrue(neonQuestManager.AllSystemsReady, "Systems should remain ready during updates");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthStatus["updateLoopActive"], "Update loop should be active");
        }

        [Test]
        public void SystemShutdown_WithInitializedSystems_ShutsDownGracefully()
        {
            // Arrange
            neonQuestManager.InitializeComponent();
            Assert.IsTrue(neonQuestManager.AllSystemsReady, "Systems should be ready before shutdown");

            // Act
            neonQuestManager.ShutdownSystems();

            // Assert
            Assert.IsFalse(neonQuestManager.AllSystemsReady, "Systems should not be ready after shutdown");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            Assert.IsFalse((bool)healthStatus["updateLoopActive"], "Update loop should not be active after shutdown");
        }

        [Test]
        public void SystemHealthStatus_WithVariousStates_ReportsAccurately()
        {
            // Test uninitialized state
            var healthBefore = neonQuestManager.GetSystemHealthStatus();
            Assert.IsFalse((bool)healthBefore["allSystemsReady"], "Should not be ready before initialization");
            Assert.IsFalse((bool)healthBefore["isInitialized"], "Should not be initialized");

            // Test initialized state
            neonQuestManager.InitializeComponent();
            var healthAfter = neonQuestManager.GetSystemHealthStatus();
            Assert.IsTrue((bool)healthAfter["allSystemsReady"], "Should be ready after initialization");
            Assert.IsTrue((bool)healthAfter["isInitialized"], "Should be initialized");

            // Test shutdown state
            neonQuestManager.ShutdownSystems();
            var healthShutdown = neonQuestManager.GetSystemHealthStatus();
            Assert.IsFalse((bool)healthShutdown["allSystemsReady"], "Should not be ready after shutdown");
        }

        [Test]
        public void DependencyInjection_WithSystemComponents_InjectsCorrectly()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert - Test that components can be retrieved by type
            var configManager = neonQuestManager.GetSystemComponent<ConfigurationManager>();
            Assert.IsNotNull(configManager, "Should be able to retrieve ConfigurationManager");

            var playerTracker = neonQuestManager.GetSystemComponent<PlayerMovementTracker>();
            Assert.IsNotNull(playerTracker, "Should be able to retrieve PlayerMovementTracker");

            var behaviorAnalyzer = neonQuestManager.GetSystemComponent<PlayerBehaviorAnalyzer>();
            Assert.IsNotNull(behaviorAnalyzer, "Should be able to retrieve PlayerBehaviorAnalyzer");
        }

        [Test]
        public void SystemCoordination_WithConnectedSystems_EstablishesEventConnections()
        {
            // Act
            neonQuestManager.InitializeComponent();

            // Assert - Verify that systems are properly connected
            // This is tested indirectly by ensuring all systems initialize without errors
            // and that the manager reports all systems as ready
            Assert.IsTrue(neonQuestManager.AllSystemsReady, "All systems should be ready and coordinated");
            
            var healthStatus = neonQuestManager.GetSystemHealthStatus();
            var systemStatus = (Dictionary<string, bool>)healthStatus["systemStatus"];
            
            // Verify key systems are initialized
            Assert.IsTrue(systemStatus["PlayerBehaviorAnalyzer"], "PlayerBehaviorAnalyzer should be initialized");
            Assert.IsTrue(systemStatus["ProceduralGenerator"], "ProceduralGenerator should be initialized");
            Assert.IsTrue(systemStatus["PerformanceThrottler"], "PerformanceThrottler should be initialized");
        }
    }
}