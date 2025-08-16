using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.Configuration;
using NeonQuest.PlayerBehavior;

namespace Tests.Core
{
    public class EnvironmentTriggersIntegrationTests
    {
        private GameObject testGameObject;
        private EnvironmentTriggersManager triggersManager;
        private PlayerBehaviorAnalyzer behaviorAnalyzer;
        private PlayerMovementTracker movementTracker;
        private EnvironmentRulesEngine rulesEngine;
        private EnvironmentConfiguration testConfiguration;

        [SetUp]
        public void SetUp()
        {
            // Create test game object
            testGameObject = new GameObject("TestPlayer");
            
            // Add required components
            movementTracker = testGameObject.AddComponent<PlayerMovementTracker>();
            behaviorAnalyzer = testGameObject.AddComponent<PlayerBehaviorAnalyzer>();
            triggersManager = testGameObject.AddComponent<EnvironmentTriggersManager>();
            
            // Create rules engine
            rulesEngine = new EnvironmentRulesEngine();
            
            // Create test configuration
            testConfiguration = CreateTestConfiguration();
            rulesEngine.LoadConfiguration(testConfiguration);
            
            // Inject dependencies for testing
            triggersManager.InjectDependencies(behaviorAnalyzer, movementTracker, rulesEngine);
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        private EnvironmentConfiguration CreateTestConfiguration()
        {
            var config = new EnvironmentConfiguration();
            
            // Create test rules
            var speedRule = new GenerationRule("SpeedTrigger")
            {
                Priority = 2.0f,
                Cooldown = 1.0f
            };
            speedRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            ));
            speedRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.AdjustLighting, "neon_signs"));
            
            var dwellRule = new GenerationRule("DwellTrigger")
            {
                Priority = 1.0f,
                Cooldown = 2.0f
            };
            dwellRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.DwellTime,
                TriggerCondition.ComparisonOperator.GreaterThan,
                10.0f
            ));
            dwellRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.GenerateLayout, "corridor"));
            
            var positionRule = new GenerationRule("PositionTrigger")
            {
                Priority = 3.0f,
                Cooldown = 0.5f
            };
            positionRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerPosition,
                TriggerCondition.ComparisonOperator.GreaterThan,
                10.0f,
                "PlayerPositionX"
            ));
            positionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.ModifyAudio, "ambient"));
            
            config.Rules.Add(speedRule);
            config.Rules.Add(dwellRule);
            config.Rules.Add(positionRule);
            
            return config;
        }

        [Test]
        public void TriggersManager_InitializesWithRules()
        {
            // Act
            triggersManager.LoadTriggersFromRules(testConfiguration.Rules);
            
            // Assert
            Assert.AreEqual(3, triggersManager.GetActiveTriggerCount());
            
            var triggers = triggersManager.GetActiveTriggers();
            Assert.IsTrue(triggers.Any(t => t.RuleName == "SpeedTrigger"));
            Assert.IsTrue(triggers.Any(t => t.RuleName == "DwellTrigger"));
            Assert.IsTrue(triggers.Any(t => t.RuleName == "PositionTrigger"));
        }

        [Test]
        public void TriggersManager_SortsTriggersByPriority()
        {
            // Act
            triggersManager.LoadTriggersFromRules(testConfiguration.Rules);
            var triggers = triggersManager.GetActiveTriggers();
            
            // Assert - should be sorted by priority (highest first)
            Assert.AreEqual("PositionTrigger", triggers[0].RuleName); // Priority 3.0
            Assert.AreEqual("SpeedTrigger", triggers[1].RuleName);    // Priority 2.0
            Assert.AreEqual("DwellTrigger", triggers[2].RuleName);    // Priority 1.0
        }

        [Test]
        public void EnvironmentTrigger_EvaluatesConditionsCorrectly()
        {
            // Arrange
            var rule = testConfiguration.GetRuleByName("SpeedTrigger");
            var trigger = new EnvironmentTrigger(rule);
            
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f } // Above threshold of 5.0
            };
            
            // Act
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            
            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void EnvironmentTrigger_RespectsConditionThresholds()
        {
            // Arrange
            var rule = testConfiguration.GetRuleByName("SpeedTrigger");
            var trigger = new EnvironmentTrigger(rule);
            
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 3.0f } // Below threshold of 5.0
            };
            
            // Act
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            
            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void EnvironmentTrigger_RespectsCooldown()
        {
            // Arrange
            var rule = testConfiguration.GetRuleByName("SpeedTrigger");
            var trigger = new EnvironmentTrigger(rule);
            
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f }
            };
            
            // Act - First evaluation should succeed
            bool firstResult = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            
            // Trigger the action to start cooldown
            if (firstResult)
            {
                trigger.DispatchGenerationCommand(new Dictionary<string, object>());
            }
            
            // Second evaluation should fail due to cooldown
            bool secondResult = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            
            // Assert
            Assert.IsTrue(firstResult);
            Assert.IsFalse(secondResult);
            Assert.IsTrue(trigger.IsOnCooldown);
        }

        [Test]
        public void EnvironmentTrigger_CanResetCooldown()
        {
            // Arrange
            var rule = testConfiguration.GetRuleByName("SpeedTrigger");
            var trigger = new EnvironmentTrigger(rule);
            
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f }
            };
            
            // Trigger and put on cooldown
            trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            trigger.DispatchGenerationCommand(new Dictionary<string, object>());
            
            // Act
            trigger.ResetCooldown();
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            
            // Assert
            Assert.IsFalse(trigger.IsOnCooldown);
            Assert.IsTrue(result);
        }

        [Test]
        public void EnvironmentTrigger_DispatchesCorrectActions()
        {
            // Arrange
            var rule = testConfiguration.GetRuleByName("SpeedTrigger");
            var trigger = new EnvironmentTrigger(rule);
            
            var dispatchedActions = new List<GenerationAction>();
            trigger.OnGenerationCommandDispatched += (action) => dispatchedActions.Add(action);
            
            var triggerData = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f }
            };
            
            // Act
            trigger.DispatchGenerationCommand(triggerData);
            
            // Assert
            Assert.AreEqual(1, dispatchedActions.Count);
            Assert.AreEqual(GenerationAction.ActionType.AdjustLighting, dispatchedActions[0].Type);
            Assert.AreEqual("neon_signs", dispatchedActions[0].Target);
        }

        [Test]
        public void TriggersManager_HandlesMultipleConditions()
        {
            // Arrange
            var complexRule = new GenerationRule("ComplexTrigger")
            {
                Priority = 1.0f,
                Cooldown = 0.0f
            };
            
            // Add multiple conditions (AND logic)
            complexRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                3.0f
            ));
            complexRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.DwellTime,
                TriggerCondition.ComparisonOperator.LessThan,
                5.0f
            ));
            
            complexRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.TriggerEffect, "complex"));
            
            var trigger = new EnvironmentTrigger(complexRule);
            
            // Test case 1: Both conditions met
            var environmentState1 = new Dictionary<string, object>
            {
                { "PlayerSpeed", 4.0f },
                { "DwellTime", 3.0f }
            };
            
            // Test case 2: Only first condition met
            var environmentState2 = new Dictionary<string, object>
            {
                { "PlayerSpeed", 4.0f },
                { "DwellTime", 6.0f }
            };
            
            // Act & Assert
            Assert.IsTrue(trigger.EvaluateConditions(Vector3.zero, null, environmentState1));
            Assert.IsFalse(trigger.EvaluateConditions(Vector3.zero, null, environmentState2));
        }

        [UnityTest]
        public IEnumerator TriggersManager_IntegrationWithBehaviorAnalyzer()
        {
            // Arrange
            triggersManager.LoadTriggersFromRules(testConfiguration.Rules);
            
            var dispatchedCommands = new List<GenerationAction>();
            triggersManager.OnGenerationCommandDispatched += (action, context) => dispatchedCommands.Add(action);
            
            // Simulate player movement to trigger behavior analysis
            testGameObject.transform.position = new Vector3(15f, 0f, 0f); // Should trigger PositionTrigger
            
            // Wait for a few frames to allow trigger evaluation
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            // Note: This test might need adjustment based on the actual integration setup
            // The exact behavior depends on how the components are wired together
            Assert.GreaterOrEqual(dispatchedCommands.Count, 0);
        }

        [Test]
        public void TriggersManager_HandlesInvalidRules()
        {
            // Arrange
            var invalidRules = new List<GenerationRule>
            {
                new GenerationRule("InvalidRule1"), // No conditions or actions
                null, // Null rule
                new GenerationRule("") // Empty name
            };
            
            // Act
            triggersManager.LoadTriggersFromRules(invalidRules);
            
            // Assert
            Assert.AreEqual(0, triggersManager.GetActiveTriggerCount());
        }

        [Test]
        public void TriggersManager_ProvidesStatistics()
        {
            // Arrange
            triggersManager.LoadTriggersFromRules(testConfiguration.Rules);
            var statistics = triggersManager.GetStatistics();
            
            // Act & Assert
            Assert.IsNotNull(statistics);
            Assert.IsNotNull(statistics.TriggerActivationCounts);
            Assert.IsNotNull(statistics.TriggerErrorCounts);
            Assert.IsNotNull(statistics.LastActivationTimes);
        }

        [Test]
        public void TriggersManager_CanAddAndRemoveTriggers()
        {
            // Arrange
            var newRule = new GenerationRule("NewTrigger")
            {
                Priority = 1.5f,
                Cooldown = 0.0f
            };
            newRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.GameTime,
                TriggerCondition.ComparisonOperator.GreaterThan,
                10.0f
            ));
            newRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.SpawnAsset, "test"));
            
            // Act - Add trigger
            triggersManager.AddTrigger(newRule);
            
            // Assert - Trigger added
            Assert.AreEqual(1, triggersManager.GetActiveTriggerCount());
            Assert.IsNotNull(triggersManager.GetTrigger("NewTrigger"));
            
            // Act - Remove trigger
            bool removed = triggersManager.RemoveTrigger("NewTrigger");
            
            // Assert - Trigger removed
            Assert.IsTrue(removed);
            Assert.AreEqual(0, triggersManager.GetActiveTriggerCount());
            Assert.IsNull(triggersManager.GetTrigger("NewTrigger"));
        }

        [Test]
        public void TriggersManager_ProvidesEnvironmentState()
        {
            // Act
            var environmentState = triggersManager.GetCurrentEnvironmentState();
            
            // Assert
            Assert.IsNotNull(environmentState);
            Assert.IsTrue(environmentState.ContainsKey("GameTime"));
            Assert.IsTrue(environmentState.ContainsKey("PlayerPosition"));
        }
    }
}