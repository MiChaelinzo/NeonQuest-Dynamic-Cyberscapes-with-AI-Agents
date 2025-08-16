using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class EnvironmentRulesEngineTests
    {
        private EnvironmentRulesEngine _rulesEngine;
        private EnvironmentConfiguration _testConfig;
        
        [SetUp]
        public void Setup()
        {
            _rulesEngine = new EnvironmentRulesEngine();
            _testConfig = CreateTestConfiguration();
            _rulesEngine.LoadConfiguration(_testConfig);
        }
        
        private EnvironmentConfiguration CreateTestConfiguration()
        {
            var config = new EnvironmentConfiguration();
            
            // Create a test rule
            var rule = new GenerationRule("TestRule")
            {
                Priority = 1.0f,
                Cooldown = 2.0f
            };
            
            rule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            ));
            
            rule.Actions.Add(new GenerationAction(
                GenerationAction.ActionType.GenerateLayout,
                "corridor"
            ));
            
            config.AddRule(rule);
            return config;
        }
        
        [Test]
        public void LoadConfiguration_ValidConfiguration_LoadsSuccessfully()
        {
            // Arrange
            var config = new EnvironmentConfiguration();
            
            // Act
            _rulesEngine.LoadConfiguration(config);
            
            // Assert
            Assert.IsNotNull(_rulesEngine.Configuration);
            Assert.AreEqual(config, _rulesEngine.Configuration);
        }
        
        [Test]
        public void LoadConfiguration_NullConfiguration_LogsError()
        {
            // Act & Assert - Should not throw, but log error
            Assert.DoesNotThrow(() => _rulesEngine.LoadConfiguration(null));
        }
        
        [Test]
        public void EvaluateRules_MatchingConditions_ReturnsActions()
        {
            // Arrange
            var context = _rulesEngine.CreateContext(
                Vector3.zero, 
                10.0f, // Speed > 5.0f, should trigger rule
                0.0f
            );
            
            // Act
            var actions = _rulesEngine.EvaluateRules(context);
            
            // Assert
            Assert.IsNotNull(actions);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(GenerationAction.ActionType.GenerateLayout, actions[0].Type);
        }
        
        [Test]
        public void EvaluateRules_NonMatchingConditions_ReturnsEmptyList()
        {
            // Arrange
            var context = _rulesEngine.CreateContext(
                Vector3.zero, 
                2.0f, // Speed < 5.0f, should not trigger rule
                0.0f
            );
            
            // Act
            var actions = _rulesEngine.EvaluateRules(context);
            
            // Assert
            Assert.IsNotNull(actions);
            Assert.AreEqual(0, actions.Count);
        }
        
        [Test]
        public void EvaluateRuleConditions_AllConditionsTrue_ReturnsTrue()
        {
            // Arrange
            var rule = _testConfig.Rules[0];
            var context = new Dictionary<string, object>
            {
                { "PlayerSpeed", 10.0f }
            };
            
            // Act
            bool result = _rulesEngine.EvaluateRuleConditions(rule, context);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void EvaluateRuleConditions_SomeConditionsFalse_ReturnsFalse()
        {
            // Arrange
            var rule = _testConfig.Rules[0];
            var context = new Dictionary<string, object>
            {
                { "PlayerSpeed", 2.0f } // Less than 5.0f
            };
            
            // Act
            bool result = _rulesEngine.EvaluateRuleConditions(rule, context);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public void CreateContext_ValidParameters_ReturnsContext()
        {
            // Arrange
            var position = new Vector3(1, 2, 3);
            var speed = 5.0f;
            var gameTime = 10.0f;
            var zone = "test_zone";
            
            // Act
            var context = _rulesEngine.CreateContext(position, speed, gameTime, zone);
            
            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(position, context["PlayerPosition"]);
            Assert.AreEqual(speed, context["PlayerSpeed"]);
            Assert.AreEqual(gameTime, context["GameTime"]);
            Assert.AreEqual(zone, context["ZoneType"]);
        }
        
        [Test]
        public void AddRule_ValidRule_AddsSuccessfully()
        {
            // Arrange
            var newRule = new GenerationRule("NewTestRule");
            newRule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.GameTime,
                TriggerCondition.ComparisonOperator.GreaterThan,
                60.0f
            ));
            newRule.Actions.Add(new GenerationAction(
                GenerationAction.ActionType.AdjustLighting
            ));
            
            int initialCount = _rulesEngine.Configuration.Rules.Count;
            
            // Act
            _rulesEngine.AddRule(newRule);
            
            // Assert
            Assert.AreEqual(initialCount + 1, _rulesEngine.Configuration.Rules.Count);
            Assert.IsNotNull(_rulesEngine.Configuration.GetRuleByName("NewTestRule"));
        }
        
        [Test]
        public void RemoveRule_ExistingRule_RemovesSuccessfully()
        {
            // Arrange
            string ruleName = "TestRule";
            int initialCount = _rulesEngine.Configuration.Rules.Count;
            
            // Act
            bool removed = _rulesEngine.RemoveRule(ruleName);
            
            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(initialCount - 1, _rulesEngine.Configuration.Rules.Count);
            Assert.IsNull(_rulesEngine.Configuration.GetRuleByName(ruleName));
        }
        
        [Test]
        public void ValidateRules_ValidConfiguration_ReturnsNoErrors()
        {
            // Act
            var errors = _rulesEngine.ValidateRules();
            
            // Assert
            Assert.IsNotNull(errors);
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void ValidateRules_InvalidRule_ReturnsErrors()
        {
            // Arrange
            var invalidRule = new GenerationRule("InvalidRule");
            // Don't add conditions or actions - makes it invalid
            _rulesEngine.Configuration.AddRule(invalidRule);
            
            // Act
            var errors = _rulesEngine.ValidateRules();
            
            // Assert
            Assert.IsNotNull(errors);
            Assert.Greater(errors.Count, 0);
        }
    }
}