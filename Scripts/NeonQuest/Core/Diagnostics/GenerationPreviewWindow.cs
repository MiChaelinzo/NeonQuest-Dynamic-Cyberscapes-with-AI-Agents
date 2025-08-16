using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NeonQuest.Configuration;
using NeonQuest.Generation;
using NeonQuest.Core;

namespace NeonQuest.Core.Diagnostics
{
    public class GenerationPreviewWindow : EditorWindow
    {
        private EnvironmentConfigurationAsset configAsset;
        private Vector2 scrollPosition;
        private bool showRuleDetails = true;
        private bool showPerformanceMetrics = true;
        private bool showGenerationPreview = true;
        
        // Simulation parameters
        private float simulatedPlayerSpeed = 5.0f;
        private float simulatedGameTime = 0.0f;
        private string simulatedZoneType = "corridor";
        private float simulatedDwellTime = 0.0f;
        private string simulatedMovementPattern = "exploration";
        
        // Preview data
        private List<GenerationRule> activeRules = new List<GenerationRule>();
        private Dictionary<string, object> simulationContext = new Dictionary<string, object>();
        
        [MenuItem("NeonQuest/Generation Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenerationPreviewWindow>("Generation Preview");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (configAsset == null)
            {
                DrawNoConfigurationMessage();
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawSimulationControls();
            DrawRuleEvaluation();
            DrawPerformancePreview();
            DrawGenerationPreview();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var newConfigAsset = (EnvironmentConfigurationAsset)EditorGUILayout.ObjectField(
                configAsset, typeof(EnvironmentConfigurationAsset), false, GUILayout.Width(200));
            
            if (newConfigAsset != configAsset)
            {
                configAsset = newConfigAsset;
                UpdateSimulation();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                UpdateSimulation();
            }
            
            if (GUILayout.Button("Reset Simulation", EditorStyles.toolbarButton))
            {
                ResetSimulation();
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoConfigurationMessage()
        {
            EditorGUILayout.Space(50);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("No Configuration Selected", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select an Environment Configuration Asset to preview generation behavior.");
            
            if (GUILayout.Button("Create New Configuration"))
            {
                CreateNewConfiguration();
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSimulationControls()
        {
            EditorGUILayout.LabelField("Simulation Parameters", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUI.BeginChangeCheck();
            
            simulatedPlayerSpeed = EditorGUILayout.Slider("Player Speed", simulatedPlayerSpeed, 0f, 20f);
            simulatedGameTime = EditorGUILayout.FloatField("Game Time (seconds)", simulatedGameTime);
            simulatedZoneType = EditorGUILayout.TextField("Zone Type", simulatedZoneType);
            simulatedDwellTime = EditorGUILayout.Slider("Dwell Time", simulatedDwellTime, 0f, 60f);
            
            var movementPatterns = new[] { "exploration", "backtracking", "cautious_exploration", "thorough_exploration", "social_interaction" };
            var currentIndex = System.Array.IndexOf(movementPatterns, simulatedMovementPattern);
            if (currentIndex == -1) currentIndex = 0;
            
            var newIndex = EditorGUILayout.Popup("Movement Pattern", currentIndex, movementPatterns);
            simulatedMovementPattern = movementPatterns[newIndex];
            
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSimulation();
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Play Simulation"))
            {
                PlaySimulation();
            }
            
            if (GUILayout.Button("Step Forward"))
            {
                StepSimulation();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuleEvaluation()
        {
            showRuleDetails = EditorGUILayout.Foldout(showRuleDetails, "Rule Evaluation", true);
            if (!showRuleDetails) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (configAsset.Configuration?.Rules != null)
            {
                EditorGUILayout.LabelField($"Total Rules: {configAsset.Configuration.Rules.Count}");
                EditorGUILayout.LabelField($"Active Rules: {activeRules.Count}");
                
                EditorGUILayout.Space();
                
                foreach (var rule in configAsset.Configuration.Rules)
                {
                    DrawRuleEvaluationResult(rule);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No rules defined in configuration");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuleEvaluationResult(GenerationRule rule)
        {
            var isActive = activeRules.Contains(rule);
            var bgColor = isActive ? Color.green : Color.gray;
            bgColor.a = 0.2f;
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(rule.RuleName, EditorStyles.boldLabel);
            
            if (isActive)
            {
                EditorGUILayout.LabelField("ACTIVE", EditorStyles.miniLabel, GUILayout.Width(50));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"Priority: {rule.Priority:F1}, Cooldown: {rule.Cooldown:F1}s");
            
            // Show condition evaluation
            if (rule.Conditions != null)
            {
                EditorGUI.indentLevel++;
                foreach (var condition in rule.Conditions)
                {
                    var conditionMet = EvaluateCondition(condition);
                    var conditionColor = conditionMet ? Color.green : Color.red;
                    
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = conditionColor;
                    
                    EditorGUILayout.LabelField($"• {condition.Type} {condition.Operator} {condition.Value}", style);
                }
                EditorGUI.indentLevel--;
            }
            
            // Show actions that would be triggered
            if (isActive && rule.Actions != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Actions:", EditorStyles.miniLabel);
                foreach (var action in rule.Actions)
                {
                    EditorGUILayout.LabelField($"• {action.Type} → {action.Target}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformancePreview()
        {
            showPerformanceMetrics = EditorGUILayout.Foldout(showPerformanceMetrics, "Performance Preview", true);
            if (!showPerformanceMetrics) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (configAsset.Configuration != null)
            {
                var config = configAsset.Configuration;
                
                EditorGUILayout.LabelField("Performance Settings:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Max Active Segments: {config.MaxActiveSegments}");
                EditorGUILayout.LabelField($"Throttle Threshold: {config.PerformanceThrottleThreshold} FPS");
                
                EditorGUILayout.Space();
                
                // Estimated performance impact
                var estimatedComplexity = CalculateEstimatedComplexity();
                var complexityColor = estimatedComplexity > 0.7f ? Color.red : estimatedComplexity > 0.4f ? Color.yellow : Color.green;
                
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = complexityColor;
                
                EditorGUILayout.LabelField($"Estimated Complexity: {estimatedComplexity:P0}", style);
                
                // Performance recommendations
                if (estimatedComplexity > 0.7f)
                {
                    EditorGUILayout.HelpBox("High complexity detected. Consider reducing max active segments or rule frequency.", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGenerationPreview()
        {
            showGenerationPreview = EditorGUILayout.Foldout(showGenerationPreview, "Generation Preview", true);
            if (!showGenerationPreview) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Predicted Generation Actions:", EditorStyles.boldLabel);
            
            if (activeRules.Count > 0)
            {
                foreach (var rule in activeRules)
                {
                    if (rule.Actions != null)
                    {
                        foreach (var action in rule.Actions)
                        {
                            DrawActionPreview(action, rule.RuleName);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No active rules - no generation would occur");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionPreview(GenerationAction action, string ruleName)
        {
            EditorGUILayout.BeginHorizontal();
            
            var actionColor = GetActionTypeColor(action.Type);
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = actionColor;
            
            EditorGUILayout.LabelField($"[{ruleName}]", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"{action.Type}", style, GUILayout.Width(120));
            EditorGUILayout.LabelField($"→ {action.Target}", GUILayout.Width(100));
            
            if (action.HasParameter("intensity"))
            {
                var intensity = action.GetParameter<float>("intensity");
                EditorGUILayout.LabelField($"Intensity: {intensity:F1}", GUILayout.Width(80));
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private Color GetActionTypeColor(GenerationAction.ActionType actionType)
        {
            switch (actionType)
            {
                case GenerationAction.ActionType.GenerateLayout: return Color.blue;
                case GenerationAction.ActionType.AdjustLighting: return Color.yellow;
                case GenerationAction.ActionType.ChangeFogDensity: return Color.gray;
                case GenerationAction.ActionType.ModifyAudio: return Color.magenta;
                case GenerationAction.ActionType.TransitionAudioZone: return Color.cyan;
                case GenerationAction.ActionType.TriggerEffect: return Color.red;
                case GenerationAction.ActionType.CreateSpatialAudio: return Color.green;
                default: return Color.white;
            }
        }
        
        private void UpdateSimulation()
        {
            if (configAsset?.Configuration == null) return;
            
            // Update simulation context
            simulationContext.Clear();
            simulationContext["PlayerSpeed"] = simulatedPlayerSpeed;
            simulationContext["GameTime"] = simulatedGameTime;
            simulationContext["ZoneType"] = simulatedZoneType;
            simulationContext["DwellTime"] = simulatedDwellTime;
            simulationContext["MovementPattern"] = simulatedMovementPattern;
            
            // Evaluate which rules would be active
            activeRules.Clear();
            foreach (var rule in configAsset.Configuration.Rules)
            {
                if (rule.EvaluateConditions(simulationContext))
                {
                    activeRules.Add(rule);
                }
            }
            
            // Sort by priority
            activeRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            Repaint();
        }
        
        private bool EvaluateCondition(TriggerCondition condition)
        {
            return condition.Evaluate(simulationContext);
        }
        
        private float CalculateEstimatedComplexity()
        {
            if (configAsset?.Configuration == null) return 0f;
            
            var complexity = 0f;
            
            // Factor in number of active rules
            complexity += activeRules.Count * 0.1f;
            
            // Factor in max active segments
            complexity += configAsset.Configuration.MaxActiveSegments * 0.05f;
            
            // Factor in number of actions
            foreach (var rule in activeRules)
            {
                if (rule.Actions != null)
                {
                    complexity += rule.Actions.Count * 0.08f;
                }
            }
            
            return Mathf.Clamp01(complexity);
        }
        
        private void PlaySimulation()
        {
            // Simulate time progression
            simulatedGameTime += 10f;
            simulatedDwellTime += 2f;
            UpdateSimulation();
        }
        
        private void StepSimulation()
        {
            simulatedGameTime += 1f;
            simulatedDwellTime += 0.5f;
            UpdateSimulation();
        }
        
        private void ResetSimulation()
        {
            simulatedPlayerSpeed = 5.0f;
            simulatedGameTime = 0.0f;
            simulatedZoneType = "corridor";
            simulatedDwellTime = 0.0f;
            simulatedMovementPattern = "exploration";
            UpdateSimulation();
        }
        
        private void CreateNewConfiguration()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Environment Configuration",
                "NewEnvironmentConfig",
                "asset",
                "Create a new environment configuration asset");
            
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<EnvironmentConfigurationAsset>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                configAsset = asset;
                UpdateSimulation();
            }
        }
    }
}