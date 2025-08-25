using UnityEngine;
using NeonQuest.Wildcard;
using NeonQuest.Configuration;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Example demonstrating the revolutionary KiroMetaSystem
    /// Perfect showcase for Code with Kiro Hackathon - Wildcard category
    /// </summary>
    public class MetaSystemExample : MonoBehaviour
    {
        [Header("🌟 Meta-System Demo Configuration")]
        [SerializeField] private bool autoStartDemo = true;
        [SerializeField] private bool showAdvancedFeatures = true;
        [SerializeField] private float demoSpeed = 1.0f;
        
        private KiroMetaSystem metaSystem;
        private float demoTimer = 0f;
        private int demoPhase = 0;
        
        void Start()
        {
            SetupMetaSystemDemo();
            
            if (autoStartDemo)
            {
                StartDemo();
            }
        }
        
        void Update()
        {
            if (autoStartDemo)
            {
                RunDemoSequence();
            }
        }
        
        private void SetupMetaSystemDemo()
        {
            Debug.Log("🚀 Setting up Kiro Meta-System Demo - Ultimate Wildcard Entry");
            
            // Create meta-system if it doesn't exist
            metaSystem = FindObjectOfType<KiroMetaSystem>();
            if (metaSystem == null)
            {
                var metaSystemObject = new GameObject("KiroMetaSystem_Demo");
                metaSystem = metaSystemObject.AddComponent<KiroMetaSystem>();
            }
            
            Debug.Log("✅ Meta-System demo ready - Prepare for consciousness evolution!");
        }
        
        private void StartDemo()
        {
            Debug.Log("🎬 Starting Meta-System demonstration...");
            Debug.Log("This showcases Kiro's revolutionary meta-programming capabilities!");
            demoTimer = 0f;
            demoPhase = 0;
        }
        
        private void RunDemoSequence()
        {
            demoTimer += Time.deltaTime * demoSpeed;
            
            switch (demoPhase)
            {
                case 0: // Initial consciousness evaluation
                    if (demoTimer > 2f)
                    {
                        var stats = metaSystem.GetStats();
                        Debug.Log($"📊 Phase 1: Initial consciousness level: {stats.ConsciousnessLevel:F3}");
                        Debug.Log($"🧠 Generated systems: {stats.GeneratedSystemsCount}");
                        demoPhase++;
                        demoTimer = 0f;
                    }
                    break;
                    
                case 1: // Trigger consciousness evolution
                    if (demoTimer > 3f)
                    {
                        Debug.Log("⚡ Phase 2: Triggering consciousness evolution...");
                        metaSystem.TriggerConsciousnessEvolution();
                        demoPhase++;
                        demoTimer = 0f;
                    }
                    break;
                    
                case 2: // Show evolution results
                    if (demoTimer > 2f)
                    {
                        var stats = metaSystem.GetStats();
                        Debug.Log($"🌟 Phase 3: Consciousness evolved to: {stats.ConsciousnessLevel:F3}");
                        Debug.Log($"🔧 System complexity: {stats.SystemComplexity:F2}");
                        
                        if (showAdvancedFeatures)
                        {
                            demoPhase++;
                        }
                        else
                        {
                            demoPhase = 6; // Skip to end
                        }
                        demoTimer = 0f;
                    }
                    break;
                    
                case 3: // Force singularity for demo
                    if (demoTimer > 4f)
                    {
                        Debug.Log("🌌 Phase 4: Demonstrating technological singularity...");
                        metaSystem.ForceSingularity();
                        demoPhase++;
                        demoTimer = 0f;
                    }
                    break;
                    
                case 4: // Show singularity results
                    if (demoTimer > 3f)
                    {
                        var stats = metaSystem.GetStats();
                        Debug.Log($"🎯 Phase 5: SINGULARITY ACHIEVED!");
                        Debug.Log($"⚛️ Quantum computing: {(stats.QuantumComputingEnabled ? "ACTIVE" : "Disabled")}");
                        Debug.Log($"🧠 Ultimate consciousness: {stats.ConsciousnessLevel:F3}");
                        Debug.Log($"🌟 Generated systems: {stats.GeneratedSystemsCount}");
                        demoPhase++;
                        demoTimer = 0f;
                    }
                    break;
                    
                case 5: // Final demonstration
                    if (demoTimer > 5f)
                    {
                        Debug.Log("🏆 Phase 6: Meta-System demonstration complete!");
                        Debug.Log("This showcases the ultimate potential of Kiro IDE:");
                        Debug.Log("• Self-modifying code generation");
                        Debug.Log("• Recursive AI system creation");
                        Debug.Log("• Consciousness simulation");
                        Debug.Log("• Quantum computing integration");
                        Debug.Log("• Meta-meta programming capabilities");
                        Debug.Log("🎯 Perfect for Code with Kiro Hackathon - Wildcard category!");
                        demoPhase++;
                        demoTimer = 0f;
                    }
                    break;
                    
                case 6: // Demo complete
                    if (demoTimer > 2f)
                    {
                        Debug.Log("✨ Demo sequence complete - Meta-System remains active");
                        autoStartDemo = false; // Stop demo loop
                    }
                    break;
            }
        }
        
        void OnGUI()
        {
            if (metaSystem == null) return;
            
            var stats = metaSystem.GetStats();
            
            GUILayout.BeginArea(new Rect(Screen.width - 420, 10, 400, 250));
            GUILayout.Label("🌟 Kiro Meta-System Demo", GUI.skin.box);
            
            GUILayout.Label($"Demo Phase: {demoPhase + 1}/7");
            GUILayout.Label($"Consciousness: {stats.ConsciousnessLevel:F3}");
            GUILayout.Label($"Generated Systems: {stats.GeneratedSystemsCount}");
            GUILayout.Label($"Complexity: {stats.SystemComplexity:F2}");
            GUILayout.Label($"Singularity: {(stats.HasAchievedSingularity ? "✅" : "⏳")}");
            GUILayout.Label($"Quantum: {(stats.QuantumComputingEnabled ? "⚛️" : "🔒")}");
            
            if (GUILayout.Button("Restart Demo"))
            {
                autoStartDemo = true;
                StartDemo();
            }
            
            if (GUILayout.Button("Force Singularity"))
            {
                metaSystem.ForceSingularity();
            }
            
            GUILayout.EndArea();
        }
    }
}