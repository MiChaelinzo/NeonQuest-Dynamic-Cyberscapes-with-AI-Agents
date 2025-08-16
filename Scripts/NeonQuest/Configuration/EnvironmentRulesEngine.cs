using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Configuration
{
    public class EnvironmentRulesEngine
    {
        private readonly NeonQuestLogger _logger;
        private EnvironmentConfiguration _configuration;
        private Dictionary<string, float> _ruleCooldowns;
        
        public EnvironmentConfiguration Configuration => _configuration;
        
        public EnvironmentConfiguration GetConfiguration()
        {
            return _configuration;
        }
        
        // Events
        public event Action<EnvironmentConfiguration> OnRulesUpdated;
        
        public EnvironmentRulesEngine()
        {
            _logger = new NeonQuestLogger("EnvironmentRulesEngine");
            _ruleCooldowns = new Dictionary<string, float>();
        }
        
        public void LoadConfiguration(EnvironmentConfiguration configuration)
        {
            if (configuration == null)
            {
                _logger.LogError("Cannot load null configuration");
                return;
            }
            
            _configuration = configuration;
            _ruleCooldowns.Clear();
            
            ValidateRules();
            _logger.LogInfo($"Loaded configuration with {_configuration.Rules.Count} rules");
            
            // Notify listeners that rules have been updated
            OnRulesUpdated?.Invoke(_configuration);
        }
        
        public List<GenerationAction> EvaluateRules(Dictionary<string, object> context)
        {
            if (_configuration == null || context == null)
                return new List<GenerationAction>();
                
            var triggeredActions = new List<GenerationAction>();
            var currentTime = Time.time;
            
            // Get rules sorted by priority
            var sortedRules = _configuration.GetRulesByPriority();
            
            foreach (var rule in sortedRules)
            {
                if (IsRuleOnCooldown(rule.RuleName, currentTime))
                    continue;
                    
                if (EvaluateRuleConditions(rule, context))
                {
                    triggeredActions.AddRange(rule.Actions);
                    SetRuleCooldown(rule.RuleName, currentTime, rule.Cooldown);
                    
                    _logger.LogDebug($"Rule '{rule.RuleName}' triggered with {rule.Actions.Count} actions");
                }
            }
            
            return triggeredActions;
        }
        
        public bool EvaluateRuleConditions(GenerationRule rule, Dictionary<string, object> context)
        {
            if (rule == null || rule.Conditions == null || rule.Conditions.Count == 0)
                return false;
                
            // All conditions must be true (AND logic)
            foreach (var condition in rule.Conditions)
            {
                if (!condition.Evaluate(context))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public GenerationRule CreateRule(string name, float priority = 1.0f, float cooldown = 0.0f)
        {
            var rule = new GenerationRule(name)
            {
                Priority = priority,
                Cooldown = cooldown
            };
            
            return rule;
        }
        
        public TriggerCondition CreateCondition(TriggerCondition.ConditionType type, 
                                              TriggerCondition.ComparisonOperator op, 
                                              object value, 
                                              string parameter = null)
        {
            return new TriggerCondition(type, op, value, parameter);
        }
        
        public GenerationAction CreateAction(GenerationAction.ActionType type, string target = null)
        {
            return new GenerationAction(type, target);
        }
        
        public void AddRule(GenerationRule rule)
        {
            if (_configuration == null)
            {
                _logger.LogError("Cannot add rule: configuration not loaded");
                return;
            }
            
            if (rule == null || !rule.IsValid())
            {
                _logger.LogError("Cannot add invalid rule");
                return;
            }
            
            // Check for duplicate rule names
            if (_configuration.GetRuleByName(rule.RuleName) != null)
            {
                _logger.LogWarning($"Rule with name '{rule.RuleName}' already exists. Replacing.");
                _configuration.RemoveRule(rule.RuleName);
            }
            
            _configuration.AddRule(rule);
            _logger.LogInfo($"Added rule: {rule.RuleName}");
            
            // Notify listeners that rules have been updated
            OnRulesUpdated?.Invoke(_configuration);
        }
        
        public bool RemoveRule(string ruleName)
        {
            if (_configuration == null)
                return false;
                
            bool removed = _configuration.RemoveRule(ruleName);
            if (removed)
            {
                _ruleCooldowns.Remove(ruleName);
                _logger.LogInfo($"Removed rule: {ruleName}");
                
                // Notify listeners that rules have been updated
                OnRulesUpdated?.Invoke(_configuration);
            }
            
            return removed;
        }
        
        public List<string> ValidateRules()
        {
            var errors = new List<string>();
            
            if (_configuration == null)
            {
                errors.Add("Configuration is null");
                return errors;
            }
            
            var ruleNames = new HashSet<string>();
            
            foreach (var rule in _configuration.Rules)
            {
                // Check for duplicate names
                if (ruleNames.Contains(rule.RuleName))
                {
                    errors.Add($"Duplicate rule name: {rule.RuleName}");
                }
                else
                {
                    ruleNames.Add(rule.RuleName);
                }
                
                // Validate rule structure
                if (!rule.IsValid())
                {
                    errors.Add($"Invalid rule: {rule.RuleName}");
                }
                
                // Validate conditions
                foreach (var condition in rule.Conditions)
                {
                    if (!condition.IsValid())
                    {
                        errors.Add($"Invalid condition in rule: {rule.RuleName}");
                    }
                }
                
                // Validate actions
                foreach (var action in rule.Actions)
                {
                    if (!action.IsValid())
                    {
                        errors.Add($"Invalid action in rule: {rule.RuleName}");
                    }
                }
            }
            
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    _logger.LogError($"Rule validation error: {error}");
                }
            }
            
            return errors;
        }
        
        public Dictionary<string, object> CreateContext(Vector3 playerPosition, 
                                                       float playerSpeed, 
                                                       float gameTime, 
                                                       string currentZone = null,
                                                       float dwellTime = 0.0f,
                                                       string movementPattern = null)
        {
            return new Dictionary<string, object>
            {
                { "PlayerPosition", playerPosition },
                { "PlayerSpeed", playerSpeed },
                { "GameTime", gameTime },
                { "ZoneType", currentZone ?? "default" },
                { "DwellTime", dwellTime },
                { "MovementPattern", movementPattern ?? "exploring" }
            };
        }
        
        private bool IsRuleOnCooldown(string ruleName, float currentTime)
        {
            if (!_ruleCooldowns.ContainsKey(ruleName))
                return false;
                
            return currentTime < _ruleCooldowns[ruleName];
        }
        
        private void SetRuleCooldown(string ruleName, float currentTime, float cooldownDuration)
        {
            if (cooldownDuration > 0)
            {
                _ruleCooldowns[ruleName] = currentTime + cooldownDuration;
            }
        }
        
        public void ClearCooldowns()
        {
            _ruleCooldowns.Clear();
        }
        
        public float GetRuleCooldownRemaining(string ruleName)
        {
            if (!_ruleCooldowns.ContainsKey(ruleName))
                return 0.0f;
                
            float remaining = _ruleCooldowns[ruleName] - Time.time;
            return Mathf.Max(0.0f, remaining);
        }
    }
}