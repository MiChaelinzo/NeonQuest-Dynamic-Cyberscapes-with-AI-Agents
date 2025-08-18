using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace NeonQuest.UI
{
    public class HolographicPanel : MonoBehaviour
    {
        [Header("Panel Components")]
        public TextMeshPro titleText;
        public TextMeshPro[] dataLines;
        public Image panelBackground;
        public ParticleSystem holoParticles;
        
        [Header("Animation")]
        public float typewriterSpeed = 0.05f;
        public float fadeInDuration = 1f;
        public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private HolographicUISystem parentSystem;
        private CanvasGroup canvasGroup;
        private bool isInitialized = false;
        private Coroutine typewriterCoroutine;
        
        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                
            canvasGroup.alpha = 0f;
        }
        
        public void Initialize(string title, string[] content, HolographicUISystem system)
        {
            parentSystem = system;
            
            SetupPanelComponents();
            
            if (titleText != null)
                titleText.text = "";
                
            // Clear data lines
            for (int i = 0; i < dataLines.Length && i < content.Length; i++)
            {
                if (dataLines[i] != null)
                    dataLines[i].text = "";
            }
            
            StartCoroutine(InitializeSequence(title, content));
            isInitialized = true;
        }
        
        void SetupPanelComponents()
        {
            // Create title if not assigned
            if (titleText == null)
            {
                GameObject titleObj = new GameObject("HoloTitle");
                titleObj.transform.SetParent(transform);
                titleObj.transform.localPosition = new Vector3(0, 1f, 0);
                
                titleText = titleObj.AddComponent<TextMeshPro>();
                titleText.fontSize = 24;
                titleText.color = parentSystem.primaryHoloColor;
                titleText.alignment = TextAlignmentOptions.Center;
            }
            
            // Create data lines if not assigned
            if (dataLines == null || dataLines.Length == 0)
            {
                dataLines = new TextMeshPro[5]; // Default 5 lines
                for (int i = 0; i < dataLines.Length; i++)
                {
                    GameObject lineObj = new GameObject($"HoloLine_{i}");
                    lineObj.transform.SetParent(transform);
                    lineObj.transform.localPosition = new Vector3(0, 0.5f - (i * 0.2f), 0);
                    
                    dataLines[i] = lineObj.AddComponent<TextMeshPro>();
                    dataLines[i].fontSize = 16;
                    dataLines[i].color = parentSystem.secondaryHoloColor;
                    dataLines[i].alignment = TextAlignmentOptions.Left;
                }
            }
            
            // Setup particle effects
            if (holoParticles == null)
            {
                GameObject particleObj = new GameObject("HoloParticles");
                particleObj.transform.SetParent(transform);
                particleObj.transform.localPosition = Vector3.zero;
                
                holoParticles = particleObj.AddComponent<ParticleSystem>();
                SetupHolographicParticles();
            }
        }
        
        void SetupHolographicParticles()
        {
            var main = holoParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;
            main.startColor = parentSystem.primaryHoloColor;
            main.maxParticles = 50;
            
            var emission = holoParticles.emission;
            emission.rateOverTime = 10f;
            
            var shape = holoParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2f, 2f, 0.1f);
            
            var velocityOverLifetime = holoParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        }
        
        IEnumerator InitializeSequence(string title, string[] content)
        {
            // Fade in panel
            yield return StartCoroutine(FadeIn());
            
            // Typewriter effect for title
            if (titleText != null)
            {
                yield return StartCoroutine(TypewriterEffect(titleText, title));
            }
            
            // Typewriter effect for content lines
            for (int i = 0; i < dataLines.Length && i < content.Length; i++)
            {
                if (dataLines[i] != null)
                {
                    yield return StartCoroutine(TypewriterEffect(dataLines[i], content[i]));
                    yield return new WaitForSeconds(0.1f); // Small delay between lines
                }
            }
            
            // Start particle effects
            if (holoParticles != null)
                holoParticles.Play();
        }
        
        IEnumerator FadeIn()
        {
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                canvasGroup.alpha = fadeInCurve.Evaluate(progress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        IEnumerator TypewriterEffect(TextMeshPro textComponent, string fullText)
        {
            textComponent.text = "";
            
            for (int i = 0; i <= fullText.Length; i++)
            {
                textComponent.text = fullText.Substring(0, i);
                
                // Add cursor effect
                if (i < fullText.Length)
                    textComponent.text += "<color=#00FFFF>|</color>";
                
                // Play typing sound occasionally
                if (i % 3 == 0 && parentSystem != null)
                    parentSystem.PlayInterfaceSound(1);
                
                yield return new WaitForSeconds(typewriterSpeed);
            }
            
            // Remove cursor
            textComponent.text = fullText;
        }
        
        public void UpdateFlicker(float globalTimer)
        {
            if (!isInitialized) return;
            
            float flicker = Mathf.Sin(globalTimer * 20f) * 0.1f + 1f;
            
            if (titleText != null)
            {
                Color titleColor = parentSystem.primaryHoloColor;
                titleColor.a *= flicker;
                titleText.color = titleColor;
            }
            
            foreach (var line in dataLines)
            {
                if (line != null)
                {
                    Color lineColor = parentSystem.secondaryHoloColor;
                    lineColor.a *= flicker;
                    line.color = lineColor;
                }
            }
        }
        
        public void Dismiss()
        {
            StartCoroutine(DismissSequence());
        }
        
        IEnumerator DismissSequence()
        {
            if (holoParticles != null)
                holoParticles.Stop();
                
            // Fade out
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }
            
            if (parentSystem != null)
                parentSystem.RemovePanel(this);
                
            Destroy(gameObject);
        }
        
        void OnDestroy()
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
        }
    }
}