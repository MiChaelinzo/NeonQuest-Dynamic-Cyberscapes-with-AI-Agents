using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonQuest.Configuration
{
    [Serializable]
    public class GenerationRule
    {
        public string RuleName { get; set; }
        public float Priority { get; set; } = 1.0f;
        public float Cooldown { get; set; } = 0.0f;
        public List<TriggerCondition> Conditions { get; set; }
        public List<GenerationAction> Actions { get; set; }
        
        public GenerationRule()
        {
            Conditions = new List<TriggerCondition>();
            Actions = new List<GenerationAction>();
        }
        
        public GenerationRule(string ruleName) : this()
        {
            RuleName = ruleName;
        }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(RuleName) && 
                   Conditions != null && Conditions.Count > 0 && 
                   Actions != null && Actions.Count > 0 &&
                   Conditions.All(c => c.IsValid()) &&
                   Actions.All(a => a.IsValid());
        }
        
        public bool EvaluateConditions(Dictionary<string, object> context)
        {
            if (Conditions == null || Conditions.Count == 0)
                return false;
                
            return Conditions.All(condition => condition.Evaluate(context));
        }
    }
}