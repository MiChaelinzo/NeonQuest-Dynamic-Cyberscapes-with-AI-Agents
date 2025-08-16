using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonQuest.Configuration
{
    [Serializable]
    public class TriggerCondition
    {
        public enum ConditionType
        {
            PlayerPosition,
            PlayerSpeed,
            GameTime,
            ZoneType,
            DwellTime,
            MovementPattern
        }
        
        public enum ComparisonOperator
        {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
            Contains,
            NotContains
        }
        
        public ConditionType Type { get; set; }
        public ComparisonOperator Operator { get; set; }
        public object Value { get; set; }
        public string Parameter { get; set; }
        
        public TriggerCondition()
        {
        }
        
        public TriggerCondition(ConditionType type, ComparisonOperator op, object value, string parameter = null)
        {
            Type = type;
            Operator = op;
            Value = value;
            Parameter = parameter;
        }
        
        public bool IsValid()
        {
            return Value != null;
        }
        
        public bool Evaluate(Dictionary<string, object> context)
        {
            if (!context.ContainsKey(GetContextKey()))
                return false;
                
            var contextValue = context[GetContextKey()];
            
            return CompareValues(contextValue, Value, Operator);
        }
        
        private string GetContextKey()
        {
            return string.IsNullOrEmpty(Parameter) ? Type.ToString() : Parameter;
        }
        
        private bool CompareValues(object contextValue, object targetValue, ComparisonOperator op)
        {
            if (contextValue == null || targetValue == null)
                return false;
                
            try
            {
                switch (op)
                {
                    case ComparisonOperator.Equals:
                        return contextValue.Equals(targetValue);
                    case ComparisonOperator.NotEquals:
                        return !contextValue.Equals(targetValue);
                    case ComparisonOperator.GreaterThan:
                        return Convert.ToDouble(contextValue) > Convert.ToDouble(targetValue);
                    case ComparisonOperator.LessThan:
                        return Convert.ToDouble(contextValue) < Convert.ToDouble(targetValue);
                    case ComparisonOperator.GreaterThanOrEqual:
                        return Convert.ToDouble(contextValue) >= Convert.ToDouble(targetValue);
                    case ComparisonOperator.LessThanOrEqual:
                        return Convert.ToDouble(contextValue) <= Convert.ToDouble(targetValue);
                    case ComparisonOperator.Contains:
                        return contextValue.ToString().Contains(targetValue.ToString());
                    case ComparisonOperator.NotContains:
                        return !contextValue.ToString().Contains(targetValue.ToString());
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}