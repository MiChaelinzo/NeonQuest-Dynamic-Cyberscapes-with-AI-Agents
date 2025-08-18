using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

namespace NeonQuest.UI
{
    public class HolographicUISystem : MonoBehaviour
    {
        [Header("Holographic Effects")]
        public Material hologramMaterial;
        public Color primaryHoloColor = new Color(0, 1, 1, 0.8f);
        public Color secondaryHoloColor = new Color(1, 0, 1, 0.6f);
        public float flickerIntensity = 0.1f;
        public float scanlineSpeed = 2.0f;
        
        [Header("AR Interface Elements")]
        public GameObject dataStreamPrefab;
        public GameObject holoPanelPrefab;
        public GameObject neuralLinkIndicatorPrefab;
        
        [Header("Audio")]
        public AudioSource holoAudioSource;
        public AudioClip[] interfaceSounds;
        
        private List<HolographicPanel> activePanels = new List<HolographicPanel>();
        private Camera playerCamera;
        private float globalFlickerTimer;
        
        public struct DataStream
        {
            public string content;
            public float speed;
            public Color color;
            public Vector3 direction;
        }
        
        void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindObjectOfType<Camera>();
                
            InitializeHolographicMaterial();
            StartCoroutine(GlobalFlickerEffect());
        }
        
        void InitializeHolographicMaterial()
        {
            if (hologramMaterial != null)
            {
                hologramMaterial.SetColor("_HoloColor", primaryHoloColor);
                hologramMaterial.SetFloat("_FlickerIntensity", flickerIntensity);
                hologramMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
            }
        }
        
        public HolographicPanel CreateDataPanel(Vector3 worldPosition, string title, string[] dataLines)
        {
            GameObject panelObj = Instantiate(holoPanelPrefab, worldPosition, Quaternion.identity);
            HolographicPanel panel = panelObj.GetComponent<HolographicPanel>();
            
            if (panel == null)
                panel = panelObj.AddComponent<HolographicPanel>();
                
            panel.Initialize(title, dataLines, this);
            activePanels.Add(panel);
            
            PlayInterfaceSound(0); // Panel creation sound
            return panel;
        }
        
        public void CreateNeuralLinkIndicator(Transform target, string status)
        {
            GameObject indicator = Instantiate(neuralLinkIndicatorPrefab, target.position + Vector3.up * 2f, Quaternion.identity);
            indicator.transform.SetParent(target);
            
            NeuralLinkIndicator linkComp = indicator.GetComponent<NeuralLinkIndicator>();
            if (linkComp == null)
                linkComp = indicator.AddComponent<NeuralLinkIndicator>();
                
            linkComp.SetStatus(status);
        }
        
        public void SpawnDataStream(Vector3 startPos, Vector3 endPos, DataStream streamData)
        {
            StartCoroutine(AnimateDataStream(startPos, endPos, streamData));
        }
        
        IEnumerator AnimateDataStream(Vector3 start, Vector3 end, DataStream data)
        {
            GameObject streamObj = Instantiate(dataStreamPrefab, start, Quaternion.identity);
            TextMeshPro streamText = streamObj.GetComponent<TextMeshPro>();
            
            if (streamText != null)
            {
                streamText.text = data.content;
                streamText.color = data.color;
            }
            
            float journey = 0f;
            while (journey <= 1f)
            {
                journey += Time.deltaTime * data.speed;
                streamObj.transform.position = Vector3.Lerp(start, end, journey);
                
                // Add some digital noise effect
                Vector3 noise = new Vector3(
                    Mathf.Sin(Time.time * 10f) * 0.1f,
                    Mathf.Cos(Time.time * 15f) * 0.1f,
                    0
                );
                streamObj.transform.position += noise;
                
                yield return null;
            }
            
            Destroy(streamObj);
        }
        
        IEnumerator GlobalFlickerEffect()
        {
            while (true)
            {
                globalFlickerTimer += Time.deltaTime;
                
                // Random glitch effects
                if (Random.value < 0.02f) // 2% chance per frame
                {
                    yield return StartCoroutine(GlitchEffect());
                }
                
                // Update all active panels
                foreach (var panel in activePanels)
                {
                    if (panel != null)
                        panel.UpdateFlicker(globalFlickerTimer);
                }
                
                yield return null;
            }
        }
        
        IEnumerator GlitchEffect()
        {
            float originalFlicker = flickerIntensity;
            flickerIntensity = 0.5f;
            
            PlayInterfaceSound(2); // Glitch sound
            
            yield return new WaitForSeconds(0.1f);
            
            flickerIntensity = originalFlicker;
        }
        
        public void PlayInterfaceSound(int soundIndex)
        {
            if (holoAudioSource != null && interfaceSounds != null && soundIndex < interfaceSounds.Length)
            {
                holoAudioSource.PlayOneShot(interfaceSounds[soundIndex]);
            }
        }
        
        void Update()
        {
            // Face panels toward player
            foreach (var panel in activePanels)
            {
                if (panel != null && playerCamera != null)
                {
                    Vector3 directionToCamera = playerCamera.transform.position - panel.transform.position;
                    directionToCamera.y = 0; // Keep panels upright
                    panel.transform.rotation = Quaternion.LookRotation(-directionToCamera);
                }
            }
        }
        
        public void RemovePanel(HolographicPanel panel)
        {
            activePanels.Remove(panel);
        }
    }
}