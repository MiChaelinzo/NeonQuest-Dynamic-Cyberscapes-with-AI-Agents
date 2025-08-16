using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using NeonQuest.Core;
using NeonQuest.Configuration;

namespace Tests.Core
{
    [TestFixture]
    public class EnvironmentTriggerTests
    {
        private GenerationRule testRule;
        private EnvironmentTrigger trigger;

        [SetUp]
        public void SetUp()
        {
            // Create a test rule with conditions and actions
            testRule = new GenerationRule("TestTrigger")
            {
                Priority = 2.0f,
                Cooldown = 1.0f
            };

            // Add a speed condition
            testRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            ));

            // Add a lighting action
            testRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.AdjustLighting, "neon_signs"));

            trigger = new EnvironmentTrigger(testRule);
        }

        [Test]
        public void Constructor_WithValidRule_InitializesCorrectly()
        {
            // Assert
            Assert.AreEqual(testRule.Priority, trigger.Priority);
            Assert.AreEqual(testRule.Cooldown, trigger.Cooldown);
            Assert.AreEqual(testRule.RuleName, trigger.RuleName);
            Assert.IsFalse(trigger.IsOnCooldown);
        }

        [Test]
        public void Constructor_WithNullRule_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new EnvironmentTrigger(null));
        }

        [Test]
        public void EvaluateConditions_WithValidConditions_ReturnsTrue()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f } // Above threshold
            };

            // Act
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void EvaluateConditions_WithInvalidConditions_ReturnsFalse()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 3.0f } // Below threshold
            };

            // Act
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateConditions_WhenOnCooldown_ReturnsFalse()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f }
            };

            // First trigger to start cooldown
            trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            trigger.DispatchGenerationCommand(new Dictionary<string, object>());

            // Act - Second evaluation while on cooldown
            bool result = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);

            // Assert
            Assert.IsFalse(result);
            Assert.IsTrue(trigger.IsOnCooldown);
        }

        [Test]
        public void DispatchGenerationCommand_WithValidData_FiresEvents()
        {
            // Arrange
            var triggerActivatedFired = false;
            var commandDispatchedFired = false;
            string activatedTriggerName = null;
            GenerationAction dispatchedAction = null;

            trigger.OnTriggerActivated += (name, context) => 
            {
                triggerActivatedFired = true;
                activatedTriggerName = name;
            };

            trigger.OnGenerationCommandDispatched += (action) => 
            {
                commandDispatchedFired = true;
                dispatchedAction = action;
            };

            var triggerData = new Dictionary<string, object>
            {
                { "TestData", "TestValue" }
            };

            // Act
            trigger.DispatchGenerationCommand(triggerData);

            // Assert
            Assert.IsTrue(triggerActivatedFired);
            Assert.IsTrue(commandDispatchedFired);
            Assert.AreEqual(testRule.RuleName, activatedTriggerName);
            Assert.IsNotNull(dispatchedAction);
            Assert.AreEqual(GenerationAction.ActionType.AdjustLighting, dispatchedAction.Type);
        }

        [Test]
        public void ResetCooldown_WhenOnCooldown_AllowsImmediateTrigger()
        {
            // Arrange
            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>
            {
                { "PlayerSpeed", 6.0f }
            };

            // Put trigger on cooldown
            trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            trigger.DispatchGenerationCommand(new Dictionary<string, object>());
            Assert.IsTrue(trigger.IsOnCooldown);

            // Act
            trigger.ResetCooldown();

            // Assert
            Assert.IsFalse(trigger.IsOnCooldown);
            bool canTriggerAgain = trigger.EvaluateConditions(playerPosition, behaviorData, environmentState);
            Assert.IsTrue(canTriggerAgain);
        }

        [Test]
        public void GetRule_ReturnsOriginalRule()
        {
            // Act
            var retrievedRule = trigger.GetRule();

            // Assert
            Assert.AreEqual(testRule, retrievedRule);
            Assert.AreEqual(testRule.RuleName, retrievedRule.RuleName);
            Assert.AreEqual(testRule.Priority, retrievedRule.Priority);
        }

        [Test]
        public void GetTriggerContext_AfterDispatch_ContainsCorrectData()
        {
            // Arrange
            var triggerData = new Dictionary<string, object>
            {
                { "TestKey", "TestValue" }
            };

            // Act
            trigger.DispatchGenerationCommand(triggerData);
            var context = trigger.GetTriggerContext();

            // Assert
            Assert.IsNotNull(context);
            Assert.IsTrue(context.ContainsKey("RuleName"));
            Assert.IsTrue(context.ContainsKey("TriggerTime"));
            Assert.IsTrue(context.ContainsKey("Priority"));
            Assert.IsTrue(context.ContainsKey("TestKey"));
            Assert.AreEqual(testRule.RuleName, context["RuleName"]);
            Assert.AreEqual("TestValue", context["TestKey"]);
        }

        [Test]
        public void HasTriggeredRecently_WithinTimeWindow_ReturnsTrue()
        {
            // Arrange
            trigger.DispatchGenerationCommand(new Dictionary<string, object>());

            // Act & Assert
            Assert.IsTrue(trigger.HasTriggeredRecently(2.0f));
            Assert.IsFalse(trigger.HasTriggeredRecently(0.0f));
        }

        [Test]
        public void EvaluateConditions_WithMultipleConditions_RequiresAllToPass()
        {
            // Arrange - Create rule with multiple conditions
            var multiConditionRule = new GenerationRule("MultiConditionTest")
            {
                Priority = 1.0f,
                Cooldown = 0.0f
            };

            multiConditionRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                3.0f
            ));

            multiConditionRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.DwellTime,
                TriggerCondition.ComparisonOperator.LessThan,
                10.0f
            ));

            multiConditionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.TriggerEffect, "test"));

            var multiTrigger = new EnvironmentTrigger(multiConditionRule);

            var playerPosition = Vector3.zero;
            var behaviorData = new Dictionary<string, object>();

            // Test case 1: Both conditions met
            var environmentState1 = new Dictionary<string, object>
            {
                { "PlayerSpeed", 4.0f },
                { "DwellTime", 5.0f }
            };

            // Test case 2: Only first condition met
            var environmentState2 = new Dictionary<string, object>
            {
                { "PlayerSpeed", 4.0f },
                { "DwellTime", 15.0f }
            };

            // Test case 3: Only second condition met
            var environmentState3 = new Dictionary<string, object>
            {
                { "PlayerSpeed", 2.0f },
                { "DwellTime", 5.0f }
            };

            // Act & Assert
            Assert.IsTrue(multiTrigger.EvaluateConditions(playerPosition, behaviorData, environmentState1));
            Assert.IsFalse(multiTrigger.EvaluateConditions(playerPosition, behaviorData, environmentState2));
            Assert.IsFalse(multiTrigger.EvaluateConditions(playerPosition, behaviorData, environmentState3));
        }

        [Test]
        public void EvaluateConditions_WithPlayerPositionCondition_UsesCorrectContext()
        {
            // Arrange - Create rule with position condition
            var positionRule = new GenerationRule("PositionTest")
            {
                Priority = 1.0f,
                Cooldown = 0.0f
            };

            positionRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerPosition,
                TriggerCondition.ComparisonOperator.GreaterThan,
                10.0f,
                "PlayerPositionX"
            ));

            positionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.SpawnAsset, "test"));

            var positionTrigger = new EnvironmentTrigger(positionRule);

            var playerPosition = new Vector3(15.0f, 0.0f, 0.0f);
            var behaviorData = new Dictionary<string, object>();
            var environmentState = new Dictionary<string, object>();

            // Act
            bool result = positionTrigger.EvaluateConditions(playerPosition, behaviorData, environmentState);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void DispatchGenerationCommand_WithMultipleActions_DispatchesAll()
        {
            // Arrange
            var multiActionRule = new GenerationRule("MultiActionTest")
            {
                Priority = 1.0f,
                Cooldown = 0.0f
            };

            multiActionRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.GameTime,
                TriggerCondition.ComparisonOperator.GreaterThan,
                0.0f
            ));

            multiActionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.AdjustLighting, "lights"));
            multiActionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.ModifyAudio, "audio"));
            multiActionRule.Actions.Add(new GenerationAction(GenerationAction.ActionType.ChangeFogDensity, "fog"));

            var multiActionTrigger = new EnvironmentTrigger(multiActionRule);

            var dispatchedActions = new List<GenerationAction>();
            multiActionTrigger.OnGenerationCommandDispatched += (action) => dispatchedActions.Add(action);

            // Act
            multiActionTrigger.DispatchGenerationCommand(new Dictionary<string, object>());

            // Assert
            Assert.AreEqual(3, dispatchedActions.Count);
            Assert.IsTrue(dispatchedActions.Exists(a => a.Type == GenerationAction.ActionType.AdjustLighting));
            Assert.IsTrue(dispatchedActions.Exists(a => a.Type == GenerationAction.ActionType.ModifyAudio));
            Assert.IsTrue(dispatchedActions.Exists(a => a.Type == GenerationAction.ActionType.ChangeFogDensity));
        }
    }
}