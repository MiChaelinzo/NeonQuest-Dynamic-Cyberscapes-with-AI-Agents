using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using NeonQuest.Configuration;

namespace NeonQuest.Configuration.Editor
{
    public class ConfigurationEditorWindow : EditorWindow
    {
        private EnvironmentConfigurationAsset targetAsset;
        private string yamlContent = "";
        private Vector2 scrollPosition;
        private bool isDirty = false;
        private GUIStyle textAreaStyle;
        
        private readonly Dictionary<string, Color> syntaxColors = new Dictionary<string, Color>
        {
            { "comment", Color.green },
            { "key", Color.cyan },
            { "value", Color.yellow },
            { "string", Color.magenta }
        };
        
        [MenuItem("NeonQuest/Configuration Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigurationEditorWindow>("NeonQuest Config Editor");
            window.minSize = new Vector2(800, 600);
        }
        
        public static void ShowWindow(EnvironmentConfigurationAsset asset)
        {
            var window = ShowWindow();
            window.targetAsset = asset;
            window.LoadYAMLContent();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            textAreaStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                wordWrap = false
            };
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawMainContent();
            DrawStatusBar();
            
            if (isDirty)
            {
                Repaint();
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("New", EditorStyles.toolbarButton))
            {
                CreateNewConfiguration();
            }
            
            if (GUILayout.Button("Open", EditorStyles.toolbarButton))
            {
                OpenConfiguration();
            }
            
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                SaveConfiguration();
            }
            
            if (GUILayout.Button("Save As", EditorStyles.toolbarButton))
            {
                SaveConfigurationAs();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
            {
                ValidateCurrentConfiguration();
            }
            
            if (GUILayout.Button("Format", EditorStyles.toolbarButton))
            {
                FormatYAML();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Examples", EditorStyles.toolbarDropDown))
            {
                ShowExamplesMenu();
            }
            
            GUILayout.FlexibleSpace();
            
            if (targetAsset != null)
            {
                EditorGUILayout.LabelField($"Editing: {targetAsset.name}", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Main editor area
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("YAML Configuration", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUI.BeginChangeCheck();
            yamlContent = EditorGUILayout.TextArea(yamlContent, textAreaStyle, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            
            // Side panel with help and validation
            DrawSidePanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSidePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            EditorGUILayout.LabelField("Configuration Help", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (GUILayout.Button("Insert Corridor Settings"))
            {
                InsertTemplate(GetCorridorTemplate());
            }
            
            if (GUILayout.Button("Insert Lighting Settings"))
            {
                InsertTemplate(GetLightingTemplate());
            }
            
            if (GUILayout.Button("Insert Atmosphere Settings"))
            {
                InsertTemplate(GetAtmosphereTemplate());
            }
            
            if (GUILayout.Button("Insert Audio Settings"))
            {
                InsertTemplate(GetAudioTemplate());
            }
            
            if (GUILayout.Button("Insert Performance Settings"))
            {
                InsertTemplate(GetPerformanceTemplate());
            }
            
            if (GUILayout.Button("Insert Example Rule"))
            {
                InsertTemplate(GetExampleRuleTemplate());
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Validation results
            DrawValidationPanel();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawValidationPanel()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (!string.IsNullOrEmpty(yamlContent))
            {
                try
                {
                    var loader = new YAMLConfigLoader();
                    var config = loader.LoadConfigurationFromString(yamlContent);
                    var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                    
                    if (validationResult.IsValid)
                    {
                        EditorGUILayout.HelpBox("✓ Configuration is valid", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Configuration has errors:", MessageType.Error);
                        
                        var scrollPos = Vector2.zero;
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
                        
                        foreach (var error in validationResult.Errors)
                        {
                            EditorGUILayout.LabelField($"• {error}", EditorStyles.wordWrappedMiniLabel);
                        }
                        
                        EditorGUILayout.EndScrollView();
                    }
                    
                    if (validationResult.Warnings.Count > 0)
                    {
                        EditorGUILayout.HelpBox($"{validationResult.Warnings.Count} warnings", MessageType.Warning);
                    }
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.HelpBox($"YAML Parse Error: {e.Message}", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No configuration to validate", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var lines = yamlContent.Split('\n').Length;
            var chars = yamlContent.Length;
            
            EditorGUILayout.LabelField($"Lines: {lines}, Characters: {chars}", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            if (isDirty)
            {
                EditorGUILayout.LabelField("Modified", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateNewConfiguration()
        {
            yamlContent = GetTemplateConfiguration();
            targetAsset = null;
            isDirty = true;
        }
        
        private void OpenConfiguration()
        {
            var path = EditorUtility.OpenFilePanel("Open Configuration", Application.dataPath, "yaml");
            if (!string.IsNullOrEmpty(path))
            {
                yamlContent = File.ReadAllText(path);
                isDirty = false;
            }
        }
        
        private void SaveConfiguration()
        {
            if (targetAsset != null)
            {
                SaveToAsset();
            }
            else
            {
                SaveConfigurationAs();
            }
        }
        
        private void SaveConfigurationAs()
        {
            var path = EditorUtility.SaveFilePanel("Save Configuration", Application.dataPath, "environment_config", "yaml");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, yamlContent);
                isDirty = false;
                AssetDatabase.Refresh();
            }
        }
        
        private void SaveToAsset()
        {
            if (targetAsset != null)
            {
                try
                {
                    var loader = new YAMLConfigLoader();
                    var config = loader.LoadConfigurationFromString(yamlContent);
                    targetAsset.Configuration = config;
                    EditorUtility.SetDirty(targetAsset);
                    isDirty = false;
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to save to asset: {e.Message}", "OK");
                }
            }
        }
        
        private void LoadYAMLContent()
        {
            if (targetAsset?.Configuration != null)
            {
                // This would require implementing YAML serialization from the configuration object
                yamlContent = "# Configuration loaded from asset\n# YAML serialization would be implemented here";
            }
        }
        
        private void ValidateCurrentConfiguration()
        {
            if (!string.IsNullOrEmpty(yamlContent))
            {
                try
                {
                    var loader = new YAMLConfigLoader();
                    var config = loader.LoadConfigurationFromString(yamlContent);
                    var result = ConfigurationValidator.ValidateConfiguration(config);
                    var message = ConfigurationValidator.FormatValidationResult(result);
                    
                    EditorUtility.DisplayDialog("Validation Result", message, "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Validation Error", $"Failed to parse YAML: {e.Message}", "OK");
                }
            }
        }
        
        private void FormatYAML()
        {
            // Basic YAML formatting - would be enhanced with proper YAML parser
            var lines = yamlContent.Split('\n');
            var formatted = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    formatted.Add(line);
                }
                else if (trimmed.Contains(":"))
                {
                    // Basic key-value formatting
                    var parts = trimmed.Split(':');
                    if (parts.Length >= 2)
                    {
                        var key = parts[0].Trim();
                        var value = string.Join(":", parts, 1, parts.Length - 1).Trim();
                        formatted.Add($"{key}: {value}");
                    }
                    else
                    {
                        formatted.Add(line);
                    }
                }
                else
                {
                    formatted.Add(line);
                }
            }
            
            yamlContent = string.Join("\n", formatted);
            isDirty = true;
        }
        
        private void ShowExamplesMenu()
        {
            var menu = new GenericMenu();
            
            var examples = new[]
            {
                ("Default Environment", "default_environment.yaml"),
                ("Industrial District", "industrial_district.yaml"),
                ("Neon Nightclub", "neon_nightclub.yaml"),
                ("Abandoned Subway", "abandoned_subway.yaml"),
                ("Corporate Tower", "corporate_tower.yaml"),
                ("Configuration Template", "configuration_template.yaml")
            };
            
            foreach (var (name, filename) in examples)
            {
                menu.AddItem(new GUIContent(name), false, () => LoadExample(filename));
            }
            
            menu.ShowAsContext();
        }
        
        private void LoadExample(string filename)
        {
            var path = Path.Combine(Application.dataPath, "Scripts", "NeonQuest", "Configuration", "Examples", filename);
            if (File.Exists(path))
            {
                yamlContent = File.ReadAllText(path);
                isDirty = true;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Example file not found: {filename}", "OK");
            }
        }
        
        private void InsertTemplate(string template)
        {
            yamlContent += "\n" + template;
            isDirty = true;
        }
        
        private string GetTemplateConfiguration()
        {
            return @"# NeonQuest Environment Configuration
# Basic template for creating custom environments

corridors:
  generation_distance: 50.0
  cleanup_distance: 100.0
  variation_seed_factors: [player_speed, time_of_day, zone_type]

lighting:
  neon_response_distance: 5.0
  brightness_multiplier_range: [0.5, 2.0]
  transition_duration: 2.0

atmosphere:
  fog_density_range: [0.1, 0.8]
  ambient_volume_range: [0.3, 0.9]
  transition_speed: 0.1

performance:
  max_active_segments: 10
  throttle_threshold: 60.0

rules: []";
        }
        
        private string GetCorridorTemplate()
        {
            return @"corridors:
  generation_distance: 50.0
  cleanup_distance: 100.0
  variation_seed_factors: [player_speed, time_of_day, zone_type]";
        }
        
        private string GetLightingTemplate()
        {
            return @"lighting:
  neon_response_distance: 5.0
  brightness_multiplier_range: [0.5, 2.0]
  transition_duration: 2.0";
        }
        
        private string GetAtmosphereTemplate()
        {
            return @"atmosphere:
  fog_density_range: [0.1, 0.8]
  ambient_volume_range: [0.3, 0.9]
  transition_speed: 0.1";
        }
        
        private string GetAudioTemplate()
        {
            return @"audio:
  zone_transition_distance: 10.0
  transition_duration: 3.0
  spatial_audio_range: 20.0
  max_audio_sources: 20
  culling_distance: 50.0";
        }
        
        private string GetPerformanceTemplate()
        {
            return @"performance:
  max_active_segments: 10
  throttle_threshold: 60.0";
        }
        
        private string GetExampleRuleTemplate()
        {
            return @"  - name: ExampleRule
    priority: 2.0
    cooldown: 5.0
    conditions:
      - type: PlayerSpeed
        operator: GreaterThan
        value: 5.0
    actions:
      - action: AdjustLighting
        target: neon_signs
        parameters:
          intensity: 1.2";
        }
    }
}