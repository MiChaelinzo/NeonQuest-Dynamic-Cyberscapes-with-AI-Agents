using UnityEngine;
using NeonQuest.Core.SceneSetup;

namespace NeonQuest.Core.Examples
{
    /// <summary>
    /// Example script showing how to set up a scene with NeonQuest and Neon Underground assets
    /// This script can be added to a GameObject in your scene to automatically configure everything
    /// </summary>
    public class ExampleSceneSetup : MonoBehaviour
    {
        [Header("Scene Setup Configuration")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool showSetupProgress = true;
        
        [Header("Example Neon Underground Assets")]
        [SerializeField] private GameObject[] examplePrefabs;
        [SerializeField] private Transform assetContainer;
        
        private NeonQuestSceneManager sceneManager;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupExampleScene();
            }
        }
        
        [ContextMenu("Setup Example Scene")]
        public void SetupExampleScene()
        {
            Debug.Log("Setting up example NeonQuest scene...");
            
            // Create or find scene manager
            sceneManager = FindObjectOfType<NeonQuestSceneManager>();
            if (sceneManager == null)
            {
                var sceneManagerGO = new GameObject("NeonQuestSceneManager");
                sceneManager = sceneManagerGO.AddComponent<NeonQuestSceneManager>();
            }
            
            // Configure scene manager with example assets
            ConfigureSceneManager();
            
            // Start scene setup
            sceneManager.SetupScene();
            
            if (showSetupProgress)
            {
                InvokeRepeating(nameof(CheckSetupProgress), 1f, 1f);
            }
        }
        
        private void ConfigureSceneManager()
        {
            // Set asset container if provided
            if (assetContainer != null)
            {
                var assetParentField = typeof(NeonQuestSceneManager).GetField("assetParent", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                assetParentField?.SetValue(sceneManager, assetContainer);
            }
            
            // Add example prefabs
            if (examplePrefabs != null && examplePrefabs.Length > 0)
            {
                var prefabsField = typeof(NeonQuestSceneManager).GetField("neonUndergroundPrefabs", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prefabsField?.SetValue(sceneManager, examplePrefabs);
            }
            
            Debug.Log($"Configured scene manager with {examplePrefabs?.Length ?? 0} example prefabs");
        }
        
        private void CheckSetupProgress()
        {
            if (sceneManager == null) return;
            
            if (sceneManager.IsSceneSetupComplete)
            {
                Debug.Log("Scene setup completed successfully!");
                Debug.Log($"Created {sceneManager.PrefabVariants.Count} prefab variants");
                Debug.Log($"Instantiated {sceneManager.InstantiatedAssets.Count} assets");
                
                CancelInvoke(nameof(CheckSetupProgress));
            }
            else
            {
                Debug.Log("Scene setup in progress...");
            }
        }
        
        [ContextMenu("Create Example Assets")]
        public void CreateExampleAssets()
        {
            // Create some basic example assets if none are provided
            if (examplePrefabs == null || examplePrefabs.Length == 0)
            {
                CreateBasicExamplePrefabs();
            }
        }
        
        private void CreateBasicExamplePrefabs()
        {
            var exampleList = new System.Collections.Generic.List<GameObject>();
            
            // Create a basic corridor segment
            var corridor = CreateExampleCorridor();
            if (corridor != null) exampleList.Add(corridor);
            
            // Create a basic neon sign
            var neonSign = CreateExampleNeonSign();
            if (neonSign != null) exampleList.Add(neonSign);
            
            // Create a basic junction
            var junction = CreateExampleJunction();
            if (junction != null) exampleList.Add(junction);
            
            examplePrefabs = exampleList.ToArray();
            Debug.Log($"Created {examplePrefabs.Length} basic example prefabs");
        }
        
        private GameObject CreateExampleCorridor()
        {
            var corridor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridor.name = "ExampleCorridor";
            corridor.transform.localScale = new Vector3(2f, 3f, 10f);
            
            // Add some basic materials
            var renderer = corridor.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.2f, 0.2f, 0.3f);
                material.SetFloat("_Metallic", 0.8f);
                renderer.material = material;
            }
            
            // Make it a prefab-like object
            corridor.SetActive(false);
            if (assetContainer != null)
            {
                corridor.transform.SetParent(assetContainer);
            }
            
            return corridor;
        }
        
        private GameObject CreateExampleNeonSign()
        {
            var neonSign = GameObject.CreatePrimitive(PrimitiveType.Quad);
            neonSign.name = "ExampleNeonSign";
            neonSign.transform.localScale = new Vector3(2f, 1f, 1f);
            
            // Add emissive material
            var renderer = neonSign.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.cyan;
                material.SetColor("_EmissionColor", Color.cyan * 2f);
                material.EnableKeyword("_EMISSION");
                renderer.material = material;
            }
            
            // Add a light component
            var light = neonSign.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.cyan;
            light.intensity = 2f;
            light.range = 10f;
            
            neonSign.SetActive(false);
            if (assetContainer != null)
            {
                neonSign.transform.SetParent(assetContainer);
            }
            
            return neonSign;
        }
        
        private GameObject CreateExampleJunction()
        {
            var junction = new GameObject("ExampleJunction");
            
            // Create a cross-shaped junction
            var center = GameObject.CreatePrimitive(PrimitiveType.Cube);
            center.name = "JunctionCenter";
            center.transform.SetParent(junction.transform);
            center.transform.localScale = new Vector3(4f, 3f, 4f);
            
            // Add connecting corridors
            for (int i = 0; i < 4; i++)
            {
                var connector = GameObject.CreatePrimitive(PrimitiveType.Cube);
                connector.name = $"JunctionConnector_{i}";
                connector.transform.SetParent(junction.transform);
                connector.transform.localScale = new Vector3(2f, 3f, 6f);
                
                float angle = i * 90f;
                connector.transform.localRotation = Quaternion.Euler(0, angle, 0);
                connector.transform.localPosition = Vector3.forward * 5f;
            }
            
            junction.SetActive(false);
            if (assetContainer != null)
            {
                junction.transform.SetParent(assetContainer);
            }
            
            return junction;
        }
    }
}