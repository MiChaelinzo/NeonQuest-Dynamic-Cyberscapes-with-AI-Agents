using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using NeonQuest.Configuration;

namespace NeonQuest.Configuration.Editor
{
    [CustomEditor(typeof(EnvironmentConfigurationAsset))]
    public class EnvironmentConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty configProperty;
        private bool showCorridorSettings = true;
        private bool showLightingSettings = true;
        private bool showAtmosphereSettings = true;
        private bool showAudioSettings = true;
        private bool showPerformanceSettings = true;
        private bool showRules = true;
        private Vector2 rulesScrollPosition;
        
        private void OnEnable()
        {
            configProperty = serializedObject.FindProperty("configuration");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var configAsset = (EnvironmentConfigurationAsset)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NeonQuest Environment Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Validation status
            DrawValidationStatus(configAsset);
            
            EditorGUILayout.Space();
            
            // Configuration sections
            DrawCorridorSettings();
            DrawLightingSettings();
            DrawAtmosphereSettings();
            DrawAudioSettings();
            DrawPerformanceSettings();
            DrawRulesSection(configAsset);
            
            EditorGUILayout.Space();
            
            // Action buttons
            DrawActionButtons(configAsset);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawValidationStatus(EnvironmentConfigurationAsset configAsset)
        {
            if (configAsset.Configuration != null)
            {
                var validationResult = ConfigurationValidator.ValidateConfiguration(configAsset.Configuration);
                
                if (validationResult.IsValid)
                {
                    EditorGUILayout.HelpBox("✓ Configuration is valid", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✗ Configuration has errors", MessageType.Error);
                    
                    foreach (var error in validationResult.Errors.Take(3)) // Show first 3 errors
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                    
                    if (validationResult.Errors.Count > 3)
                    {
                        EditorGUILayout.HelpBox($"... and {validationResult.Errors.Count - 3} more errors", MessageType.Error);
                    }
                }
                
                if (validationResult.Warnings.Count > 0)
                {
                    foreach (var warning in validationResult.Warnings.Take(2)) // Show first 2 warnings
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                    
                    if (validationResult.Warnings.Count > 2)
                    {
                        EditorGUILayout.HelpBox($"... and {validationResult.Warnings.Count - 2} more warnings", MessageType.Warning);
                    }
                }
            }
        }
        
        private void DrawCorridorSettings()
        {
            showCorridorSettings = EditorGUILayout.Foldout(showCorridorSettings, "Corridor Generation Settings", true);
            if (showCorridorSettings)
            {
                EditorGUI.indentLevel++;
                
                var genDistance = configProperty.FindPropertyRelative("CorridorGenerationDistance");
                var cleanupDistance = configProperty.FindPropertyRelative("CorridorCleanupDistance");
                var variationFactors = configProperty.FindPropertyRelative("VariationSeedFactors");
                
                EditorGUILayout.PropertyField(genDistance, new GUIContent("Generation Distance", "Distance ahead of player where new segments are generated"));
                EditorGUILayout.PropertyField(cleanupDistance, new GUIContent("Cleanup Distance", "Distance behind player where old segments are removed"));
                
                // Validation feedback
                if (cleanupDistance.floatValue <= genDistance.floatValue)
                {
                    EditorGUILayout.HelpBox("Cleanup distance should be greater than generation distance", MessageType.Warning);
                }
                
                EditorGUILayout.PropertyField(variationFactors, new GUIContent("Variation Seed Factors", "Factors that influence procedural variation"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawLightingSettings()
        {
            showLightingSettings = EditorGUILayout.Foldout(showLightingSettings, "Lighting System Settings", true);
            if (showLightingSettings)
            {
                EditorGUI.indentLevel++;
                
                var responseDistance = configProperty.FindPropertyRelative("NeonResponseDistance");
                var brightnessRange = configProperty.FindPropertyRelative("BrightnessMultiplierRange");
                var transitionDuration = configProperty.FindPropertyRelative("LightingTransitionDuration");
                
                EditorGUILayout.PropertyField(responseDistance, new GUIContent("Neon Response Distance", "Distance at which neon signs respond to player"));
                EditorGUILayout.PropertyField(brightnessRange, new GUIContent("Brightness Range", "Min/Max brightness multipliers"));
                EditorGUILayout.PropertyField(transitionDuration, new GUIContent("Transition Duration", "Duration of lighting transitions in seconds"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAtmosphereSettings()
        {
            showAtmosphereSettings = EditorGUILayout.Foldout(showAtmosphereSettings, "Atmospheric Effects Settings", true);
            if (showAtmosphereSettings)
            {
                EditorGUI.indentLevel++;
                
                var fogRange = configProperty.FindPropertyRelative("FogDensityRange");
                var volumeRange = configProperty.FindPropertyRelative("AmbientVolumeRange");
                var transitionSpeed = configProperty.FindPropertyRelative("AtmosphereTransitionSpeed");
                
                EditorGUILayout.PropertyField(fogRange, new GUIContent("Fog Density Range", "Min/Max fog density values"));
                EditorGUILayout.PropertyField(volumeRange, new GUIContent("Ambient Volume Range", "Min/Max ambient audio volume"));
                EditorGUILayout.PropertyField(transitionSpeed, new GUIContent("Transition Speed", "Speed of atmospheric changes"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAudioSettings()
        {
            showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio System Settings", true);
            if (showAudioSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Audio settings are configured in the YAML file. Use the YAML editor for detailed audio configuration.", MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPerformanceSettings()
        {
            showPerformanceSettings = EditorGUILayout.Foldout(showPerformanceSettings, "Performance Settings", true);
            if (showPerformanceSettings)
            {
                EditorGUI.indentLevel++;
                
                var maxSegments = configProperty.FindPropertyRelative("MaxActiveSegments");
                var throttleThreshold = configProperty.FindPropertyRelative("PerformanceThrottleThreshold");
                
                EditorGUILayout.PropertyField(maxSegments, new GUIContent("Max Active Segments", "Maximum number of active corridor segments"));
                EditorGUILayout.PropertyField(throttleThreshold, new GUIContent("Throttle Threshold", "FPS threshold for performance throttling"));
                
                if (maxSegments.intValue > 20)
                {
                    EditorGUILayout.HelpBox("High segment count may impact performance on lower-end hardware", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRulesSection(EnvironmentConfigurationAsset configAsset)
        {
            showRules = EditorGUILayout.Foldout(showRules, "Generation Rules", true);
            if (showRules)
            {
                EditorGUI.indentLevel++;
                
                if (configAsset.Configuration?.Rules != null)
                {
                    EditorGUILayout.LabelField($"Rules Count: {configAsset.Configuration.Rules.Count}");
                    
                    rulesScrollPosition = EditorGUILayout.BeginScrollView(rulesScrollPosition, GUILayout.Height(200));
                    
                    foreach (var rule in configAsset.Configuration.Rules)
                    {
                        DrawRulePreview(rule);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("No rules defined. Use the YAML editor to add generation rules.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRulePreview(GenerationRule rule)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(rule.RuleName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Priority: {rule.Priority:F1}, Cooldown: {rule.Cooldown:F1}s");
            EditorGUILayout.LabelField($"Conditions: {rule.Conditions?.Count ?? 0}, Actions: {rule.Actions?.Count ?? 0}");
            
            if (!rule.IsValid())
            {
                EditorGUILayout.HelpBox("Rule has validation errors", MessageType.Error);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons(EnvironmentConfigurationAsset configAsset)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open YAML Editor"))
            {
                ConfigurationEditorWindow.ShowWindow(configAsset);
            }
            
            if (GUILayout.Button("Load from YAML"))
            {
                LoadFromYAML(configAsset);
            }
            
            if (GUILayout.Button("Save to YAML"))
            {
                SaveToYAML(configAsset);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load Example Config"))
            {
                ShowExampleConfigMenu(configAsset);
            }
            
            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfiguration(configAsset);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void LoadFromYAML(EnvironmentConfigurationAsset configAsset)
        {
            var path = EditorUtility.OpenFilePanel("Load Configuration", Application.dataPath, "yaml");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var loader = new YAMLConfigLoader();
                    var config = loader.LoadConfiguration(path);
                    configAsset.Configuration = config;
                    EditorUtility.SetDirty(configAsset);
                    Debug.Log($"Configuration loaded from {path}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to load configuration: {e.Message}", "OK");
                }
            }
        }
        
        private void SaveToYAML(EnvironmentConfigurationAsset configAsset)
        {
            var path = EditorUtility.SaveFilePanel("Save Configuration", Application.dataPath, "environment_config", "yaml");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // Note: This would require implementing YAML serialization
                    EditorUtility.DisplayDialog("Info", "YAML export functionality would be implemented here", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to save configuration: {e.Message}", "OK");
                }
            }
        }
        
        private void ShowExampleConfigMenu(EnvironmentConfigurationAsset configAsset)
        {
            var menu = new GenericMenu();
            
            var exampleConfigs = new[]
            {
                ("Default Environment", "default_environment.yaml"),
                ("Industrial District", "industrial_district.yaml"),
                ("Neon Nightclub", "neon_nightclub.yaml"),
                ("Abandoned Subway", "abandoned_subway.yaml"),
                ("Corporate Tower", "corporate_tower.yaml")
            };
            
            foreach (var (name, filename) in exampleConfigs)
            {
                menu.AddItem(new GUIContent(name), false, () => LoadExampleConfig(configAsset, filename));
            }
            
            menu.ShowAsContext();
        }
        
        private void LoadExampleConfig(EnvironmentConfigurationAsset configAsset, string filename)
        {
            var path = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples", filename);
            if (File.Exists(path))
            {
                try
                {
                    var loader = new YAMLConfigLoader();
                    var config = loader.LoadConfiguration(path);
                    configAsset.Configuration = config;
                    EditorUtility.SetDirty(configAsset);
                    Debug.Log($"Example configuration '{filename}' loaded");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to load example configuration: {e.Message}", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Example configuration file not found: {path}", "OK");
            }
        }
        
        private void ValidateConfiguration(EnvironmentConfigurationAsset configAsset)
        {
            if (configAsset.Configuration != null)
            {
                var result = ConfigurationValidator.ValidateConfiguration(configAsset.Configuration);
                var message = ConfigurationValidator.FormatValidationResult(result);
                
                if (result.IsValid)
                {
                    EditorUtility.DisplayDialog("Validation Result", message, "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Errors", message, "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No configuration to validate", "OK");
            }
        }
    }
}