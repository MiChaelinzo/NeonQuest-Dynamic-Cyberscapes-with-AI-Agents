using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Effects
{
    /// <summary>
    /// Quantum-inspired lighting system that creates dynamic, particle-based lighting effects
    /// Uses quantum mechanics principles like superposition and entanglement for unique visual effects
    /// </summary>
    public class QuantumLightingEngine : NeonQuestComponent
    {
        [Header("Quantum Lighting Configuration")]
        [SerializeField] private int maxQuantumParticles = 1000;
        [SerializeField] private float quantumCoherenceRadius = 10f;
        [SerializeField] private float entanglementStrength = 0.8f;
        [SerializeField] private float superpositionIntensity = 0.6f;
        [SerializeField] private bool enableQuantumTunneling = true;
        [SerializeField] private bool enableWaveParticleInterference = true;

        [Header("Lighting Effects")]
        [SerializeField] private Light[] quantumLights;
        [SerializeField] private ParticleSystem[] quantumParticleSystems;
        [SerializeField] private Material quantumLightMaterial;
        [SerializeField] private Shader quantumShader;
        [SerializeField] private Volume postProcessVolume;

        [Header("Color Quantum States")]
        [SerializeField] private Color[] quantumColorStates = new Color[]
        {
            new Color(1f, 0f, 1f, 1f),    // Magenta
            new Color(0f, 1f, 1f, 1f),    // Cyan
            new Color(1f, 1f, 0f, 1f),    // Yellow
            new Color(1f, 0.5f, 0f, 1f),  // Orange
            new Color(0.5f, 0f, 1f, 1f),  // Purple
            new Color(0f, 1f, 0.5f, 1f)   // Green-Cyan
        };

        // Quantum System Components
        private QuantumLightParticle[] quantumParticles;
        private QuantumField quantumField;
        private WaveFunction lightWaveFunction;
        private EntanglementNetwork entanglementNetwork;
        
        // Rendering and Effects
        private ComputeShader quantumComputeShader;
        private RenderTexture quantumFieldTexture;
        private MaterialPropertyBlock quantumPropertyBlock;
        private Coroutine quantumUpdateCoroutine;
        
        // Performance Optimization
        private Camera mainCamera;
        private float lastQuantumUpdate;
        private const float QUANTUM_UPDATE_INTERVAL = 0.016f; // 60 FPS

        [System.Serializable]
        public class QuantumLightParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public Color quantumState;
            public float wavePhase;
            public float amplitude;
            public float coherenceTime;
            public bool isEntangled;
            public int entanglementGroup;
            public float superpositionWeight;
            public Vector3 probabilityCloud;
            public float tunnelProbability;
            public bool isObserved;
        }

        [System.Serializable]
        public class QuantumField
        {
            public Vector3 fieldCenter;
            public float fieldRadius;
            public float fieldStrength;
            public float[] waveAmplitudes;
            public float[] phaseShifts;
            public Color fieldColor;
            public float coherenceLevel;
        }

        [System.Serializable]
        public class WaveFunction
        {
            public float[] realComponents;
            public float[] imaginaryComponents;
            public float normalizationFactor;
            public float collapseThreshold;
            public bool isCollapsed;
        }

        [System.Serializable]
        public class EntanglementPair
        {
            public int particle1Index;
            public int particle2Index;
            public float entanglementStrength;
            public float correlationPhase;
            public Vector3 sharedQuantumState;
        }

        protected override void OnInitialize()
        {
            LogDebug("Initializing Quantum Lighting Engine");

            try
            {
                // Initialize quantum components
                InitializeQuantumParticles();
                InitializeQuantumField();
                InitializeWaveFunction();
                InitializeEntanglementNetwork();

                // Setup rendering components
                SetupQuantumRendering();

                // Initialize lighting systems
                InitializeQuantumLights();

                // Setup post-processing effects
                SetupPostProcessing();

                // Start quantum update loop
                quantumUpdateCoroutine = StartCoroutine(QuantumUpdateLoop());

                LogDebug($"Quantum Lighting Engine initialized with {maxQuantumParticles} particles");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize Quantum Lighting Engine: {ex.Message}");
                OnInitializationFailed(ex);
                throw;
            }
        }

        private void InitializeQuantumParticles()
        {
            quantumParticles = new QuantumLightParticle[maxQuantumParticles];

            for (int i = 0; i < maxQuantumParticles; i++)
            {
                quantumParticles[i] = new QuantumLightParticle
                {
                    position = Random.insideUnitSphere * quantumCoherenceRadius,
                    velocity = Random.insideUnitSphere * 2f,
                    quantumState = quantumColorStates[Random.Range(0, quantumColorStates.Length)],
                    wavePhase = Random.Range(0f, Mathf.PI * 2f),
                    amplitude = Random.Range(0.3f, 1f),
                    coherenceTime = Random.Range(5f, 15f),
                    isEntangled = false,
                    entanglementGroup = -1,
                    superpositionWeight = Random.Range(0.2f, 0.8f),
                    probabilityCloud = Random.insideUnitSphere * 0.5f,
                    tunnelProbability = Random.Range(0.1f, 0.3f),
                    isObserved = false
                };
            }

            LogDebug($"Initialized {maxQuantumParticles} quantum light particles");
        }

        private void InitializeQuantumField()
        {
            quantumField = new QuantumField
            {
                fieldCenter = transform.position,
                fieldRadius = quantumCoherenceRadius,
                fieldStrength = 1f,
                waveAmplitudes = new float[64], // 8x8 grid
                phaseShifts = new float[64],
                fieldColor = Color.white,
                coherenceLevel = 1f
            };

            // Initialize wave amplitudes and phases
            for (int i = 0; i < 64; i++)
            {
                quantumField.waveAmplitudes[i] = Random.Range(0.1f, 1f);
                quantumField.phaseShifts[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void InitializeWaveFunction()
        {
            int waveComponents = 128;
            lightWaveFunction = new WaveFunction
            {
                realComponents = new float[waveComponents],
                imaginaryComponents = new float[waveComponents],
                normalizationFactor = 1f,
                collapseThreshold = 0.8f,
                isCollapsed = false
            };

            // Initialize wave function with random quantum states
            for (int i = 0; i < waveComponents; i++)
            {
                lightWaveFunction.realComponents[i] = Random.Range(-1f, 1f);
                lightWaveFunction.imaginaryComponents[i] = Random.Range(-1f, 1f);
            }

            NormalizeWaveFunction();
        }

        private void InitializeEntanglementNetwork()
        {
            entanglementNetwork = new EntanglementNetwork();
            
            // Create random entanglement pairs
            int entanglementPairs = Mathf.Min(maxQuantumParticles / 4, 100);
            
            for (int i = 0; i < entanglementPairs; i++)
            {
                int particle1 = Random.Range(0, maxQuantumParticles);
                int particle2 = Random.Range(0, maxQuantumParticles);
                
                if (particle1 != particle2)
                {
                    CreateEntanglement(particle1, particle2);
                }
            }
        }

        private void CreateEntanglement(int particle1Index, int particle2Index)
        {
            var entanglement = new EntanglementPair
            {
                particle1Index = particle1Index,
                particle2Index = particle2Index,
                entanglementStrength = entanglementStrength * Random.Range(0.7f, 1f),
                correlationPhase = Random.Range(0f, Mathf.PI * 2f),
                sharedQuantumState = Vector3.Lerp(
                    quantumParticles[particle1Index].position,
                    quantumParticles[particle2Index].position,
                    0.5f
                )
            };

            entanglementNetwork.AddEntanglement(entanglement);
            
            quantumParticles[particle1Index].isEntangled = true;
            quantumParticles[particle2Index].isEntangled = true;
            quantumParticles[particle1Index].entanglementGroup = entanglementNetwork.GetGroupCount();
            quantumParticles[particle2Index].entanglementGroup = entanglementNetwork.GetGroupCount();
        }

        private void SetupQuantumRendering()
        {
            // Find main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            // Create quantum field render texture
            quantumFieldTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBFloat);
            quantumFieldTexture.enableRandomWrite = true;
            quantumFieldTexture.Create();

            // Setup material property block
            quantumPropertyBlock = new MaterialPropertyBlock();

            // Load quantum compute shader
            quantumComputeShader = Resources.Load<ComputeShader>("Shaders/QuantumLightingCompute");
            if (quantumComputeShader == null)
            {
                LogWarning("Quantum compute shader not found, using fallback rendering");
            }
        }

        private void InitializeQuantumLights()
        {
            if (quantumLights == null || quantumLights.Length == 0)
            {
                // Create default quantum lights
                quantumLights = new Light[4];
                for (int i = 0; i < 4; i++)
                {
                    var lightGO = new GameObject($"QuantumLight_{i}");
                    lightGO.transform.SetParent(transform);
                    lightGO.transform.localPosition = Random.insideUnitSphere * quantumCoherenceRadius;
                    
                    var light = lightGO.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 10f;
                    light.intensity = 2f;
                    light.color = quantumColorStates[Random.Range(0, quantumColorStates.Length)];
                    
                    quantumLights[i] = light;
                }
            }

            // Configure quantum lights
            foreach (var light in quantumLights)
            {
                if (light != null)
                {
                    light.renderMode = LightRenderMode.ForcePixel;
                    light.shadows = LightShadows.Soft;
                }
            }
        }

        private void SetupPostProcessing()
        {
            if (postProcessVolume == null)
            {
                var volumeGO = new GameObject("QuantumPostProcessVolume");
                volumeGO.transform.SetParent(transform);
                postProcessVolume = volumeGO.AddComponent<Volume>();
                postProcessVolume.isGlobal = true;
                postProcessVolume.priority = 1;
            }

            // Setup quantum-specific post-processing effects
            SetupQuantumBloom();
            SetupQuantumColorGrading();
        }

        private void SetupQuantumBloom()
        {
            // Custom bloom settings for quantum effects
            if (postProcessVolume.profile != null)
            {
                if (postProcessVolume.profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.intensity.value = 0.8f;
                    bloom.threshold.value = 0.3f;
                    bloom.scatter.value = 0.7f;
                    bloom.tint.value = Color.cyan;
                }
            }
        }

        private void SetupQuantumColorGrading()
        {
            // Custom color grading for quantum aesthetic
            if (postProcessVolume.profile != null)
            {
                if (postProcessVolume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
                {
                    colorAdjustments.saturation.value = 20f;
                    colorAdjustments.contrast.value = 10f;
                    colorAdjustments.colorFilter.value = new Color(0.9f, 0.95f, 1f);
                }
            }
        }

        private IEnumerator QuantumUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(QUANTUM_UPDATE_INTERVAL);

                try
                {
                    // Update quantum particles
                    UpdateQuantumParticles();

                    // Update quantum field
                    UpdateQuantumField();

                    // Update wave function
                    UpdateWaveFunction();

                    // Update entanglements
                    UpdateEntanglements();

                    // Apply quantum effects to lights
                    ApplyQuantumLightingEffects();

                    // Update particle systems
                    UpdateQuantumParticleSystems();

                    // Handle quantum tunneling
                    if (enableQuantumTunneling)
                    {
                        ProcessQuantumTunneling();
                    }

                    // Handle wave-particle interference
                    if (enableWaveParticleInterference)
                    {
                        ProcessWaveInterference();
                    }

                    lastQuantumUpdate = Time.time;
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in quantum update loop: {ex.Message}");
                }
            }
        }

        private void UpdateQuantumParticles()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            Vector3 playerPosition = playerTracker != null ? playerTracker.CurrentPosition : Vector3.zero;

            for (int i = 0; i < quantumParticles.Length; i++)
            {
                var particle = quantumParticles[i];

                // Update wave phase
                particle.wavePhase += Time.deltaTime * 2f * Mathf.PI;
                if (particle.wavePhase > Mathf.PI * 2f)
                    particle.wavePhase -= Mathf.PI * 2f;

                // Update position based on quantum mechanics
                UpdateParticlePosition(particle, playerPosition);

                // Update quantum state based on observation
                UpdateQuantumObservation(particle, playerPosition);

                // Update superposition
                UpdateSuperposition(particle);

                // Decrease coherence time
                particle.coherenceTime -= Time.deltaTime;
                if (particle.coherenceTime <= 0f)
                {
                    // Decoherence - collapse to classical state
                    CollapseParticleState(particle);
                }
            }
        }

        private void UpdateParticlePosition(QuantumLightParticle particle, Vector3 playerPosition)
        {
            // Quantum harmonic oscillator motion
            float frequency = 1f + particle.amplitude * 0.5f;
            Vector3 quantumForce = -particle.position * frequency * frequency;

            // Add player influence (observation effect)
            float distanceToPlayer = Vector3.Distance(particle.position, playerPosition);
            if (distanceToPlayer < quantumCoherenceRadius)
            {
                float observationStrength = 1f - (distanceToPlayer / quantumCoherenceRadius);
                Vector3 observationForce = (playerPosition - particle.position).normalized * observationStrength * 2f;
                quantumForce += observationForce;
                
                particle.isObserved = observationStrength > 0.5f;
            }
            else
            {
                particle.isObserved = false;
            }

            // Update velocity and position
            particle.velocity += quantumForce * Time.deltaTime;
            particle.velocity *= 0.98f; // Damping
            particle.position += particle.velocity * Time.deltaTime;

            // Add quantum uncertainty (Heisenberg principle)
            if (!particle.isObserved)
            {
                Vector3 uncertainty = Random.insideUnitSphere * 0.1f * particle.superpositionWeight;
                particle.position += uncertainty;
            }
        }

        private void UpdateQuantumObservation(QuantumLightParticle particle, Vector3 playerPosition)
        {
            float distanceToPlayer = Vector3.Distance(particle.position, playerPosition);
            
            if (distanceToPlayer < quantumCoherenceRadius * 0.5f && !particle.isObserved)
            {
                // Wave function collapse due to observation
                particle.isObserved = true;
                particle.superpositionWeight *= 0.5f;
                
                // Collapse to definite color state
                int stateIndex = Random.Range(0, quantumColorStates.Length);
                particle.quantumState = quantumColorStates[stateIndex];
            }
            else if (distanceToPlayer > quantumCoherenceRadius && particle.isObserved)
            {
                // Return to superposition when unobserved
                particle.isObserved = false;
                particle.superpositionWeight = Random.Range(0.3f, 0.8f);
            }
        }

        private void UpdateSuperposition(QuantumLightParticle particle)
        {
            if (!particle.isObserved && particle.superpositionWeight > 0.1f)
            {
                // Particle exists in superposition of multiple color states
                Color superpositionColor = Color.black;
                float totalWeight = 0f;

                for (int i = 0; i < quantumColorStates.Length; i++)
                {
                    float stateWeight = Mathf.Sin(particle.wavePhase + i * Mathf.PI / 3f) * particle.superpositionWeight;
                    stateWeight = Mathf.Abs(stateWeight);
                    
                    superpositionColor += quantumColorStates[i] * stateWeight;
                    totalWeight += stateWeight;
                }

                if (totalWeight > 0f)
                {
                    particle.quantumState = superpositionColor / totalWeight;
                }
            }
        }

        private void CollapseParticleState(QuantumLightParticle particle)
        {
            // Reset coherence time
            particle.coherenceTime = Random.Range(5f, 15f);
            
            // Reset superposition
            particle.superpositionWeight = Random.Range(0.2f, 0.8f);
            
            // Reset quantum state
            particle.quantumState = quantumColorStates[Random.Range(0, quantumColorStates.Length)];
            
            // Reset observation
            particle.isObserved = false;
        }

        private void UpdateQuantumField()
        {
            // Update field based on particle positions
            Vector3 centerOfMass = Vector3.zero;
            float totalMass = 0f;

            foreach (var particle in quantumParticles)
            {
                float mass = particle.amplitude;
                centerOfMass += particle.position * mass;
                totalMass += mass;
            }

            if (totalMass > 0f)
            {
                quantumField.fieldCenter = centerOfMass / totalMass;
            }

            // Update wave amplitudes based on particle interference
            for (int i = 0; i < quantumField.waveAmplitudes.Length; i++)
            {
                float x = (i % 8) / 8f * quantumField.fieldRadius;
                float z = (i / 8) / 8f * quantumField.fieldRadius;
                Vector3 fieldPoint = quantumField.fieldCenter + new Vector3(x, 0, z);

                float totalAmplitude = 0f;
                foreach (var particle in quantumParticles)
                {
                    float distance = Vector3.Distance(particle.position, fieldPoint);
                    if (distance < quantumCoherenceRadius)
                    {
                        float waveContribution = particle.amplitude * Mathf.Cos(particle.wavePhase + distance * 0.5f);
                        totalAmplitude += waveContribution;
                    }
                }

                quantumField.waveAmplitudes[i] = Mathf.Lerp(quantumField.waveAmplitudes[i], totalAmplitude, Time.deltaTime);
            }

            // Update field color based on dominant particle colors
            Color dominantColor = Color.black;
            float totalIntensity = 0f;

            foreach (var particle in quantumParticles)
            {
                float intensity = particle.amplitude * (particle.isObserved ? 1f : particle.superpositionWeight);
                dominantColor += particle.quantumState * intensity;
                totalIntensity += intensity;
            }

            if (totalIntensity > 0f)
            {
                quantumField.fieldColor = dominantColor / totalIntensity;
            }
        }

        private void UpdateWaveFunction()
        {
            // Update wave function components
            for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
            {
                float frequency = (i + 1) * 0.1f;
                float phase = Time.time * frequency;
                
                lightWaveFunction.realComponents[i] = Mathf.Cos(phase) * quantumField.waveAmplitudes[i % quantumField.waveAmplitudes.Length];
                lightWaveFunction.imaginaryComponents[i] = Mathf.Sin(phase) * quantumField.waveAmplitudes[i % quantumField.waveAmplitudes.Length];
            }

            // Check for wave function collapse
            float probability = CalculateWaveFunctionProbability();
            if (probability > lightWaveFunction.collapseThreshold)
            {
                CollapseWaveFunction();
            }

            NormalizeWaveFunction();
        }

        private float CalculateWaveFunctionProbability()
        {
            float totalProbability = 0f;
            
            for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
            {
                float real = lightWaveFunction.realComponents[i];
                float imag = lightWaveFunction.imaginaryComponents[i];
                totalProbability += real * real + imag * imag;
            }

            return totalProbability / lightWaveFunction.realComponents.Length;
        }

        private void CollapseWaveFunction()
        {
            lightWaveFunction.isCollapsed = true;
            
            // Find dominant frequency component
            int dominantIndex = 0;
            float maxAmplitude = 0f;
            
            for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
            {
                float amplitude = Mathf.Sqrt(lightWaveFunction.realComponents[i] * lightWaveFunction.realComponents[i] + 
                                           lightWaveFunction.imaginaryComponents[i] * lightWaveFunction.imaginaryComponents[i]);
                if (amplitude > maxAmplitude)
                {
                    maxAmplitude = amplitude;
                    dominantIndex = i;
                }
            }

            // Collapse to single state
            for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
            {
                if (i == dominantIndex)
                {
                    lightWaveFunction.realComponents[i] = 1f;
                    lightWaveFunction.imaginaryComponents[i] = 0f;
                }
                else
                {
                    lightWaveFunction.realComponents[i] = 0f;
                    lightWaveFunction.imaginaryComponents[i] = 0f;
                }
            }

            // Reset collapse after some time
            StartCoroutine(ResetWaveFunctionCollapse());
        }

        private IEnumerator ResetWaveFunctionCollapse()
        {
            yield return new WaitForSeconds(2f);
            lightWaveFunction.isCollapsed = false;
        }

        private void NormalizeWaveFunction()
        {
            float norm = 0f;
            
            for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
            {
                norm += lightWaveFunction.realComponents[i] * lightWaveFunction.realComponents[i] + 
                        lightWaveFunction.imaginaryComponents[i] * lightWaveFunction.imaginaryComponents[i];
            }

            lightWaveFunction.normalizationFactor = Mathf.Sqrt(norm);
            
            if (lightWaveFunction.normalizationFactor > 0f)
            {
                for (int i = 0; i < lightWaveFunction.realComponents.Length; i++)
                {
                    lightWaveFunction.realComponents[i] /= lightWaveFunction.normalizationFactor;
                    lightWaveFunction.imaginaryComponents[i] /= lightWaveFunction.normalizationFactor;
                }
            }
        }

        private void UpdateEntanglements()
        {
            var entanglements = entanglementNetwork.GetAllEntanglements();
            
            foreach (var entanglement in entanglements)
            {
                if (entanglement.particle1Index < quantumParticles.Length && 
                    entanglement.particle2Index < quantumParticles.Length)
                {
                    var particle1 = quantumParticles[entanglement.particle1Index];
                    var particle2 = quantumParticles[entanglement.particle2Index];

                    // Entangled particles share quantum states
                    if (particle1.isObserved && !particle2.isObserved)
                    {
                        // Instant correlation - spooky action at a distance
                        particle2.quantumState = GetEntangledColor(particle1.quantumState);
                        particle2.wavePhase = particle1.wavePhase + entanglement.correlationPhase;
                    }
                    else if (particle2.isObserved && !particle1.isObserved)
                    {
                        particle1.quantumState = GetEntangledColor(particle2.quantumState);
                        particle1.wavePhase = particle2.wavePhase + entanglement.correlationPhase;
                    }

                    // Update shared quantum state
                    entanglement.sharedQuantumState = Vector3.Lerp(particle1.position, particle2.position, 0.5f);
                }
            }
        }

        private Color GetEntangledColor(Color originalColor)
        {
            // Return complementary or correlated color for entangled particle
            return new Color(1f - originalColor.r, 1f - originalColor.g, 1f - originalColor.b, originalColor.a);
        }

        private void ApplyQuantumLightingEffects()
        {
            for (int i = 0; i < quantumLights.Length && i < quantumParticles.Length; i++)
            {
                var light = quantumLights[i];
                var particle = quantumParticles[i * (quantumParticles.Length / quantumLights.Length)];

                if (light != null)
                {
                    // Update light position with quantum uncertainty
                    Vector3 targetPosition = particle.position;
                    if (!particle.isObserved)
                    {
                        targetPosition += particle.probabilityCloud;
                    }
                    
                    light.transform.position = Vector3.Lerp(light.transform.position, targetPosition, Time.deltaTime * 2f);

                    // Update light color with quantum state
                    light.color = Color.Lerp(light.color, particle.quantumState, Time.deltaTime * 3f);

                    // Update light intensity based on wave function
                    float waveIntensity = Mathf.Abs(Mathf.Sin(particle.wavePhase)) * particle.amplitude;
                    light.intensity = Mathf.Lerp(light.intensity, waveIntensity * 3f, Time.deltaTime * 2f);

                    // Add quantum flickering
                    if (!particle.isObserved)
                    {
                        float quantumFlicker = Mathf.PerlinNoise(Time.time * 10f, i * 0.1f) * 0.3f;
                        light.intensity += quantumFlicker;
                    }
                }
            }
        }

        private void UpdateQuantumParticleSystems()
        {
            foreach (var particleSystem in quantumParticleSystems)
            {
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    var emission = particleSystem.emission;
                    var colorOverLifetime = particleSystem.colorOverLifetime;

                    // Update emission rate based on quantum field strength
                    emission.rateOverTime = quantumField.fieldStrength * 50f;

                    // Update colors based on quantum field color
                    main.startColor = quantumField.fieldColor;

                    // Create color gradient based on quantum states
                    var gradient = new Gradient();
                    var colorKeys = new GradientColorKey[quantumColorStates.Length];
                    var alphaKeys = new GradientAlphaKey[2];

                    for (int i = 0; i < quantumColorStates.Length; i++)
                    {
                        colorKeys[i] = new GradientColorKey(quantumColorStates[i], i / (float)(quantumColorStates.Length - 1));
                    }

                    alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                    alphaKeys[1] = new GradientAlphaKey(0f, 1f);

                    gradient.SetKeys(colorKeys, alphaKeys);
                    colorOverLifetime.color = gradient;
                }
            }
        }

        private void ProcessQuantumTunneling()
        {
            foreach (var particle in quantumParticles)
            {
                if (Random.Range(0f, 1f) < particle.tunnelProbability * Time.deltaTime)
                {
                    // Quantum tunneling - particle can pass through barriers
                    Vector3 tunnelDestination = particle.position + Random.insideUnitSphere * 5f;
                    
                    // Check if tunneling is beneficial (moves closer to field center)
                    float currentDistance = Vector3.Distance(particle.position, quantumField.fieldCenter);
                    float tunnelDistance = Vector3.Distance(tunnelDestination, quantumField.fieldCenter);
                    
                    if (tunnelDistance < currentDistance)
                    {
                        particle.position = tunnelDestination;
                        particle.wavePhase += Mathf.PI; // Phase shift due to tunneling
                        
                        LogDebug($"Quantum tunneling occurred for particle at {particle.position}");
                    }
                }
            }
        }

        private void ProcessWaveInterference()
        {
            // Calculate interference patterns between nearby particles
            for (int i = 0; i < quantumParticles.Length; i++)
            {
                for (int j = i + 1; j < quantumParticles.Length; j++)
                {
                    var particle1 = quantumParticles[i];
                    var particle2 = quantumParticles[j];

                    float distance = Vector3.Distance(particle1.position, particle2.position);
                    
                    if (distance < quantumCoherenceRadius * 0.3f)
                    {
                        // Calculate wave interference
                        float phaseDifference = particle1.wavePhase - particle2.wavePhase;
                        float interference = Mathf.Cos(phaseDifference);

                        if (interference > 0.5f) // Constructive interference
                        {
                            particle1.amplitude = Mathf.Min(particle1.amplitude * 1.1f, 2f);
                            particle2.amplitude = Mathf.Min(particle2.amplitude * 1.1f, 2f);
                        }
                        else if (interference < -0.5f) // Destructive interference
                        {
                            particle1.amplitude = Mathf.Max(particle1.amplitude * 0.9f, 0.1f);
                            particle2.amplitude = Mathf.Max(particle2.amplitude * 0.9f, 0.1f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Manually trigger quantum effect at specific position
        /// </summary>
        public void TriggerQuantumEffect(Vector3 position, Color quantumColor, float intensity = 1f)
        {
            // Find nearest particle and modify its quantum state
            float nearestDistance = float.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < quantumParticles.Length; i++)
            {
                float distance = Vector3.Distance(quantumParticles[i].position, position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            var particle = quantumParticles[nearestIndex];
            particle.position = position;
            particle.quantumState = quantumColor;
            particle.amplitude = intensity;
            particle.coherenceTime = 10f;
            particle.isObserved = true;

            LogDebug($"Quantum effect triggered at {position} with color {quantumColor}");
        }

        /// <summary>
        /// Get current quantum field state
        /// </summary>
        public QuantumField GetQuantumFieldState()
        {
            return quantumField;
        }

        /// <summary>
        /// Get all quantum particles for external analysis
        /// </summary>
        public QuantumLightParticle[] GetQuantumParticles()
        {
            return quantumParticles;
        }

        protected override void OnCleanup()
        {
            if (quantumUpdateCoroutine != null)
            {
                StopCoroutine(quantumUpdateCoroutine);
                quantumUpdateCoroutine = null;
            }

            // Clean up render textures
            if (quantumFieldTexture != null)
            {
                quantumFieldTexture.Release();
                DestroyImmediate(quantumFieldTexture);
            }

            // Clean up entanglement network
            entanglementNetwork?.Dispose();

            LogDebug("Quantum Lighting Engine cleaned up");
        }
    }

    // Supporting class for entanglement network
    public class EntanglementNetwork
    {
        private List<EntanglementPair> entanglements;
        private int groupCount;

        public EntanglementNetwork()
        {
            entanglements = new List<EntanglementPair>();
            groupCount = 0;
        }

        public void AddEntanglement(EntanglementPair entanglement)
        {
            entanglements.Add(entanglement);
            groupCount++;
        }

        public EntanglementPair[] GetAllEntanglements()
        {
            return entanglements.ToArray();
        }

        public int GetGroupCount()
        {
            return groupCount;
        }

        public void Dispose()
        {
            entanglements.Clear();
        }
    }
}