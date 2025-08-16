using NUnit.Framework;
using System.Collections.Generic;
using NeonQuest.Configuration;

namespace NeonQuest.Tests.Configuration
{
    [TestFixture]
    public class GenerationRuleTests
    {
        [Test]
        public void Constructor_WithName_SetsNameCorrectly()
        {
            // Arrange & Act
            var rule = new GenerationRule("TestRule");
            
            // Assert
            Assert.AreEqual("TestRule", rule.RuleName);
            Assert.IsNotNull(rule.Conditions);
            Assert.IsNotNull(rule.Actions);
        }
        
        [Test]
        public void IsValid_WithNameConditionsAndActions_ReturnsTrue()
        {
            // Arrange
            var rule = new GenerationRule("ValidRule");
            rule.Conditions.Add(new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            ));
            rule.Actions.Add(new GenerationAction(
                GenerationAction.ActionType.GenerateLayout
            ));
            
            // Act & Assert
            Assert.IsTrue(rule.IsValid());
        }
        
        [Test]
        public void IsValid_WithoutName_ReturnsFalse()
        {
            // Arrange
            var rule = new GenerationRule();
            rule.Conditions.Add(new TriggerCondition());
            rule.Actions.Add(new GenerationAction());
            
            // Act & Assert
            Assert.IsFalse(rule.IsValid());
        }
        
        [Test]
        public void IsValid_WithoutConditions_ReturnsFalse()
        {
            // Arrange
            var rule = new GenerationRule("TestRule");
            rule.Actions.Add(new GenerationAction());
            
            // Act & Assert
            Assert.IsFalse(rule.IsValid());
        }
        
        [Test]
        public void IsValid_WithoutActions_ReturnsFalse()
        {
            // Arrange
            var rule = new GenerationRule("TestRule");
            rule.Conditions.Add(new TriggerCondition());
            
            // Act & Assert
            Assert.IsFalse(rule.IsValid());
        }
    }
    
    [TestFixture]
    public class TriggerConditionTests
    {
        [Test]
        public void Constructor_WithParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var condition = new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                10.0f,
                "speed_param"
            );
            
            // Assert
            Assert.AreEqual(TriggerCondition.ConditionType.PlayerSpeed, condition.Type);
            Assert.AreEqual(TriggerCondition.ComparisonOperator.GreaterThan, condition.Operator);
            Assert.AreEqual(10.0f, condition.Value);
            Assert.AreEqual("speed_param", condition.Parameter);
        }
        
        [Test]
        public void IsValid_WithValue_ReturnsTrue()
        {
            // Arrange
            var condition = new TriggerCondition();
            condition.Value = 5.0f;
            
            // Act & Assert
            Assert.IsTrue(condition.IsValid());
        }
        
        [Test]
        public void IsValid_WithoutValue_ReturnsFalse()
        {
            // Arrange
            var condition = new TriggerCondition();
            
            // Act & Assert
            Assert.IsFalse(condition.IsValid());
        }
        
        [Test]
        public void Evaluate_GreaterThanCondition_ReturnsCorrectResult()
        {
            // Arrange
            var condition = new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            );
            
            var context = new Dictionary<string, object>
            {
                { "PlayerSpeed", 10.0f }
            };
            
            // Act & Assert
            Assert.IsTrue(condition.Evaluate(context));
        }
        
        [Test]
        public void Evaluate_LessThanCondition_ReturnsCorrectResult()
        {
            // Arrange
            var condition = new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.LessThan,
                5.0f
            );
            
            var context = new Dictionary<string, object>
            {
                { "PlayerSpeed", 3.0f }
            };
            
            // Act & Assert
            Assert.IsTrue(condition.Evaluate(context));
        }
        
        [Test]
        public void Evaluate_EqualsCondition_ReturnsCorrectResult()
        {
            // Arrange
            var condition = new TriggerCondition(
                TriggerCondition.ConditionType.ZoneType,
                TriggerCondition.ComparisonOperator.Equals,
                "corridor"
            );
            
            var context = new Dictionary<string, object>
            {
                { "ZoneType", "corridor" }
            };
            
            // Act & Assert
            Assert.IsTrue(condition.Evaluate(context));
        }
        
        [Test]
        public void Evaluate_MissingContextKey_ReturnsFalse()
        {
            // Arrange
            var condition = new TriggerCondition(
                TriggerCondition.ConditionType.PlayerSpeed,
                TriggerCondition.ComparisonOperator.GreaterThan,
                5.0f
            );
            
            var context = new Dictionary<string, object>();
            
            // Act & Assert
            Assert.IsFalse(condition.Evaluate(context));
        }
    }
    
    [TestFixture]
    public class GenerationActionTests
    {
        [Test]
        public void Constructor_WithTypeAndTarget_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var action = new GenerationAction(
                GenerationAction.ActionType.GenerateLayout,
                "corridor"
            );
            
            // Assert
            Assert.AreEqual(GenerationAction.ActionType.GenerateLayout, action.Type);
            Assert.AreEqual("corridor", action.Target);
            Assert.IsNotNull(action.Parameters);
        }
        
        [Test]
        public void IsValid_WithParameters_ReturnsTrue()
        {
            // Arrange
            var action = new GenerationAction();
            
            // Act & Assert
            Assert.IsTrue(action.IsValid()); // Parameters is initialized as empty dictionary
        }
        
        [Test]
        public void GetParameter_ExistingParameter_ReturnsValue()
        {
            // Arrange
            var action = new GenerationAction();
            action.SetParameter("intensity", 2.5f);
            
            // Act
            float intensity = action.GetParameter<float>("intensity");
            
            // Assert
            Assert.AreEqual(2.5f, intensity);
        }
        
        [Test]
        public void GetParameter_NonExistingParameter_ReturnsDefault()
        {
            // Arrange
            var action = new GenerationAction();
            
            // Act
            float intensity = action.GetParameter<float>("intensity", 1.0f);
            
            // Assert
            Assert.AreEqual(1.0f, intensity);
        }
        
        [Test]
        public void SetParameter_ValidKeyValue_SetsParameter()
        {
            // Arrange
            var action = new GenerationAction();
            
            // Act
            action.SetParameter("test_param", "test_value");
            
            // Assert
            Assert.IsTrue(action.HasParameter("test_param"));
            Assert.AreEqual("test_value", action.GetParameter<string>("test_param"));
        }
        
        [Test]
        public void HasParameter_ExistingParameter_ReturnsTrue()
        {
            // Arrange
            var action = new GenerationAction();
            action.SetParameter("test", 123);
            
            // Act & Assert
            Assert.IsTrue(action.HasParameter("test"));
        }
        
        [Test]
        public void HasParameter_NonExistingParameter_ReturnsFalse()
        {
            // Arrange
            var action = new GenerationAction();
            
            // Act & Assert
            Assert.IsFalse(action.HasParameter("nonexistent"));
        }
    }
}