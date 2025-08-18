using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;
using NeonQuest.Generation;

namespace NeonQuest.Biometrics
{
    /// <summary>
    /// Biometric response system that adapts the environment based on player physiological data
    /// Integrates with heart rate monitors, eye tracking, and other biometric sensors
    /// </summary>
    public class BiometricResponseSystem : NeonQuestComponent
    {
        [Header("Biometric Configuration")]
        [SerializeField] private bool enableHeartRateMonitoring = true;
        [SerializeField] private bool enableEyeTracking = false;
        [SerializeField] private bool enableGSRSensor = false; // Galvanic Skin Response
        [SerializeField] private bool enableEEGMonitoring = false; // Electroencephalography
        [SerializeField] private float biometricSensitivity = 0.7f;
        [SerializeField] private float adaptationSpeed = 0.5f;

        [Header("Response Thresholds")]
        [SerializeField] private float stressThreshold = 0.7f;
        [SerializeField] private float excitementThreshold = 0.6f;
        [SerializeField] private float relaxationThreshold = 0.3f;
        [SerializeField] private float focusThreshold = 0.8f;

        [Header("Environmental Response")]
        [SerializeField] private bool adaptLighting = true;
        [SerializeField] private bool adaptAudio = true;
        [SerializeField] private bool adaptWeather = true;
        [SerializeField] private bool adaptGeneration = true;

        // Biometric Data Components
        private HeartRateMonitor heartRateMonitor;
        private EyeTrackingSystem eyeTrackingSystem;
        private GSRSensor gsrSensor;
        private EEGMonitor eegMonitor;
        private BiometricDataProcessor dataProcessor;
        
        // Environmental Adaptation
        private LightingEngine lightingEngine;
        private AudioEngine audioEngine;
        private AIWeatherSystem weatherSystem;
        private ProceduralGenerator proceduralGenerator;
        
        // Biometric State Management
        private BiometricState currentBiometricState;
        private Queue<BiometricReading> biometricHistory;
        private Dictionary<string, float> adaptationWeights;
        
        // Real-time Processing
        private Coroutine biometricUpdateCoroutine;
        private float lastBiometricUpdate;
        private const float BIOMETRIC_UPDATE_INTERVAL = 0.1f; // 10 Hz sampling

        [System.Serializable]
        public class BiometricState
        {
            public float heartRate;
            public float heartRateVariability;
            public float stressLevel;
            public float excitementLevel;
            public float relaxationLevel;
            public float focusLevel;
            public float cognitiveLoad;
            public float emotionalValence;
            public float arousalLevel;
            public Vector2 gazePosition;
            public float blinkRate;
            public float pupilDilation;
            public float skinConductance;
            public float[] eegBands; // Alpha, Beta, Theta, Delta waves
            public float timestamp;
        }

        [System.Serializable]
        public class BiometricReading
        {
            public BiometricState state;
            public float confidence;
            public string sensorSource;
            public float timestamp;
        }

        [System.Serializable]
        public class EnvironmentalAdaptation
        {
            public string adaptationType;
            public float intensity;
            public Color targetColor;
            public float targetVolume;
            public string targetWeather;
            public float generationRate;
            public float confidence;
        }

        protected override void OnInitialize()
        {
            LogDebug("Initializing Biometric Response System");

            try
            {
                // Initialize biometric sensors
                InitializeBiometricSensors();

                // Initialize data processor
                dataProcessor = new BiometricDataProcessor();

                // Find environmental systems
                FindEnvironmentalSystems();

                // Initialize biometric state
                InitializeBiometricState();

                // Initialize adaptation weights
                InitializeAdaptationWeights();

                // Start biometric monitoring
                biometricUpdateCoroutine = StartCoroutine(BiometricUpdateLoop());

                LogDebug("Biometric Response System initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize Biometric Response System: {ex.Message}");
                OnInitializationFailed(ex);
                throw;
            }
        }

        private void InitializeBiometricSensors()
        {
            // Initialize heart rate monitor
            if (enableHeartRateMonitoring)
            {
                heartRateMonitor = new HeartRateMonitor();
                heartRateMonitor.Initialize();
                LogDebug("Heart rate monitor initialized");
            }

            // Initialize eye tracking
            if (enableEyeTracking)
            {
                eyeTrackingSystem = new EyeTrackingSystem();
                eyeTrackingSystem.Initialize();
                LogDebug("Eye tracking system initialized");
            }

            // Initialize GSR sensor
            if (enableGSRSensor)
            {
                gsrSensor = new GSRSensor();
                gsrSensor.Initialize();
                LogDebug("GSR sensor initialized");
            }

            // Initialize EEG monitor
            if (enableEEGMonitoring)
            {
                eegMonitor = new EEGMonitor();
                eegMonitor.Initialize();
                LogDebug("EEG monitor initialized");
            }
        }

        private void FindEnvironmentalSystems()
        {
            lightingEngine = FindObjectOfType<LightingEngine>();
            audioEngine = FindObjectOfType<AudioEngine>();
            weatherSystem = FindObjectOfType<AIWeatherSystem>();
            proceduralGenerator = FindObjectOfType<ProceduralGenerator>();

            LogDebug($"Found environmental systems: Lighting={lightingEngine != null}, Audio={audioEngine != null}, Weather={weatherSystem != null}, Generation={proceduralGenerator != null}");
        }

        private void InitializeBiometricState()
        {
            currentBiometricState = new BiometricState
            {
                heartRate = 70f,
                heartRateVariability = 0.5f,
                stressLevel = 0.3f,
                excitementLevel = 0.4f,
                relaxationLevel = 0.6f,
                focusLevel = 0.5f,
                cognitiveLoad = 0.4f,
                emotionalValence = 0.5f,
                arousalLevel = 0.4f,
                gazePosition = Vector2.zero,
                blinkRate = 15f,
                pupilDilation = 0.5f,
                skinConductance = 0.3f,
                eegBands = new float[4] { 0.3f, 0.4f, 0.2f, 0.1f },
                timestamp = Time.time
            };

            biometricHistory = new Queue<BiometricReading>();
        }

        private void InitializeAdaptationWeights()
        {
            adaptationWeights = new Dictionary<string, float>
            {
                ["lighting_stress"] = 0.8f,
                ["lighting_excitement"] = 0.7f,
                ["lighting_focus"] = 0.6f,
                ["audio_stress"] = 0.9f,
                ["audio_excitement"] = 0.8f,
                ["audio_relaxation"] = 0.7f,
                ["weather_stress"] = 0.6f,
                ["weather_excitement"] = 0.5f,
                ["generation_stress"] = 0.7f,
                ["generation_focus"] = 0.8f
            };
        }

        private IEnumerator BiometricUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(BIOMETRIC_UPDATE_INTERVAL);

                try
                {
                    // Collect biometric data
                    CollectBiometricData();

                    // Process biometric signals
                    ProcessBiometricSignals();

                    // Analyze biometric patterns
                    AnalyzeBiometricPatterns();

                    // Generate environmental adaptations
                    GenerateEnvironmentalAdaptations();

                    // Apply adaptations to environment
                    ApplyEnvironmentalAdaptations();

                    lastBiometricUpdate = Time.time;
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in biometric update loop: {ex.Message}");
                }
            }
        }

        private void CollectBiometricData()
        {
            var newState = new BiometricState
            {
                timestamp = Time.time
            };

            // Collect heart rate data
            if (heartRateMonitor != null && heartRateMonitor.IsConnected())
            {
                var hrData = heartRateMonitor.GetCurrentReading();
                newState.heartRate = hrData.heartRate;
                newState.heartRateVariability = hrData.variability;
            }
            else
            {
                // Simulate heart rate based on player behavior
                newState.heartRate = SimulateHeartRate();
                newState.heartRateVariability = SimulateHRV();
            }

            // Collect eye tracking data
            if (eyeTrackingSystem != null && eyeTrackingSystem.IsConnected())
            {
                var eyeData = eyeTrackingSystem.GetCurrentReading();
                newState.gazePosition = eyeData.gazePosition;
                newState.blinkRate = eyeData.blinkRate;
                newState.pupilDilation = eyeData.pupilDilation;
            }
            else
            {
                // Simulate eye tracking based on camera movement
                newState.gazePosition = SimulateGazePosition();
                newState.blinkRate = SimulateBlinkRate();
                newState.pupilDilation = SimulatePupilDilation();
            }

            // Collect GSR data
            if (gsrSensor != null && gsrSensor.IsConnected())
            {
                newState.skinConductance = gsrSensor.GetCurrentReading().conductance;
            }
            else
            {
                newState.skinConductance = SimulateSkinConductance();
            }

            // Collect EEG data
            if (eegMonitor != null && eegMonitor.IsConnected())
            {
                var eegData = eegMonitor.GetCurrentReading();
                newState.eegBands = eegData.bandPowers;
            }
            else
            {
                newState.eegBands = SimulateEEGBands();
            }

            currentBiometricState = newState;
        }

        private float SimulateHeartRate()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return 70f;

            // Base heart rate + activity influence
            float baseHR = 70f;
            float activityInfluence = playerTracker.CurrentSpeed * 5f;
            float stressInfluence = CalculateSimulatedStress() * 20f;
            
            return Mathf.Clamp(baseHR + activityInfluence + stressInfluence, 60f, 120f);
        }

        private float SimulateHRV()
        {
            // Heart rate variability decreases with stress
            float stress = CalculateSimulatedStress();
            return Mathf.Clamp01(0.8f - stress * 0.5f);
        }

        private Vector2 SimulateGazePosition()
        {
            // Simulate gaze based on camera direction
            var camera = Camera.main;
            if (camera == null) return Vector2.zero;

            // Convert camera forward to screen space
            Vector3 forward = camera.transform.forward;
            return new Vector2(forward.x * 0.5f + 0.5f, forward.y * 0.5f + 0.5f);
        }

        private float SimulateBlinkRate()
        {
            // Normal blink rate with stress influence
            float baseRate = 15f;
            float stress = CalculateSimulatedStress();
            return baseRate + stress * 10f; // Increased blinking when stressed
        }

        private float SimulatePupilDilation()
        {
            // Pupil dilation based on lighting and arousal
            float lightLevel = RenderSettings.ambientIntensity;
            float arousal = CalculateSimulatedArousal();
            return Mathf.Clamp01(0.5f - lightLevel * 0.3f + arousal * 0.4f);
        }

        private float SimulateSkinConductance()
        {
            // GSR increases with stress and excitement
            float stress = CalculateSimulatedStress();
            float excitement = CalculateSimulatedExcitement();
            return Mathf.Clamp01(0.3f + stress * 0.4f + excitement * 0.3f);
        }

        private float[] SimulateEEGBands()
        {
            float stress = CalculateSimulatedStress();
            float focus = CalculateSimulatedFocus();
            float relaxation = 1f - stress;

            return new float[]
            {
                relaxation * 0.4f + Random.Range(-0.1f, 0.1f), // Alpha (relaxation)
                stress * 0.5f + focus * 0.3f + Random.Range(-0.1f, 0.1f), // Beta (alertness)
                (1f - focus) * 0.3f + Random.Range(-0.1f, 0.1f), // Theta (creativity)
                relaxation * 0.2f + Random.Range(-0.05f, 0.05f) // Delta (deep states)
            };
        }

        private float CalculateSimulatedStress()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return 0.3f;

            // Calculate stress based on movement patterns
            float speedVariation = Mathf.Abs(playerTracker.CurrentSpeed - playerTracker.AverageSpeed);
            float directionChanges = playerTracker.DirectionChangeFrequency;
            
            return Mathf.Clamp01((speedVariation + directionChanges) * 0.4f);
        }

        private float CalculateSimulatedExcitement()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return 0.4f;

            // Excitement based on exploration and speed
            float speedFactor = Mathf.Clamp01(playerTracker.CurrentSpeed / 8f);
            float explorationFactor = playerTracker.ExplorationIndex;
            
            return (speedFactor + explorationFactor) * 0.5f;
        }

        private float CalculateSimulatedArousal()
        {
            return (CalculateSimulatedStress() + CalculateSimulatedExcitement()) * 0.5f;
        }

        private float CalculateSimulatedFocus()
        {
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            if (playerTracker == null) return 0.5f;

            // Focus based on movement consistency
            float speedConsistency = 1f - Mathf.Abs(playerTracker.CurrentSpeed - playerTracker.AverageSpeed) / Mathf.Max(playerTracker.AverageSpeed, 1f);
            float directionConsistency = 1f - playerTracker.DirectionChangeFrequency;
            
            return Mathf.Clamp01((speedConsistency + directionConsistency) * 0.5f);
        }

        private void ProcessBiometricSignals()
        {
            // Calculate derived biometric metrics
            currentBiometricState.stressLevel = CalculateStressLevel();
            currentBiometricState.excitementLevel = CalculateExcitementLevel();
            currentBiometricState.relaxationLevel = CalculateRelaxationLevel();
            currentBiometricState.focusLevel = CalculateFocusLevel();
            currentBiometricState.cognitiveLoad = CalculateCognitiveLoad();
            currentBiometricState.emotionalValence = CalculateEmotionalValence();
            currentBiometricState.arousalLevel = CalculateArousalLevel();
        }

        private float CalculateStressLevel()
        {
            float hrStress = Mathf.Clamp01((currentBiometricState.heartRate - 70f) / 30f);
            float hrvStress = 1f - currentBiometricState.heartRateVariability;
            float gsrStress = currentBiometricState.skinConductance;
            float eegStress = currentBiometricState.eegBands[1]; // Beta waves
            
            return (hrStress + hrvStress + gsrStress + eegStress) * 0.25f;
        }

        private float CalculateExcitementLevel()
        {
            float hrExcitement = Mathf.Clamp01((currentBiometricState.heartRate - 60f) / 40f);
            float pupilExcitement = currentBiometricState.pupilDilation;
            float gsrExcitement = currentBiometricState.skinConductance;
            float eegExcitement = currentBiometricState.eegBands[1]; // Beta waves
            
            return (hrExcitement + pupilExcitement + gsrExcitement + eegExcitement) * 0.25f;
        }

        private float CalculateRelaxationLevel()
        {
            float hrRelaxation = 1f - Mathf.Clamp01((currentBiometricState.heartRate - 60f) / 30f);
            float hrvRelaxation = currentBiometricState.heartRateVariability;
            float eegRelaxation = currentBiometricState.eegBands[0]; // Alpha waves
            float blinkRelaxation = 1f - Mathf.Clamp01((currentBiometricState.blinkRate - 15f) / 15f);
            
            return (hrRelaxation + hrvRelaxation + eegRelaxation + blinkRelaxation) * 0.25f;
        }

        private float CalculateFocusLevel()
        {
            float pupilFocus = currentBiometricState.pupilDilation;
            float blinkFocus = 1f - Mathf.Clamp01((currentBiometricState.blinkRate - 10f) / 20f);
            float eegFocus = currentBiometricState.eegBands[1] - currentBiometricState.eegBands[2]; // Beta - Theta
            
            return Mathf.Clamp01((pupilFocus + blinkFocus + eegFocus) / 3f);
        }

        private float CalculateCognitiveLoad()
        {
            float eegLoad = currentBiometricState.eegBands[1] + currentBiometricState.eegBands[2]; // Beta + Theta
            float pupilLoad = currentBiometricState.pupilDilation;
            float hrLoad = Mathf.Clamp01((currentBiometricState.heartRate - 70f) / 20f);
            
            return (eegLoad + pupilLoad + hrLoad) / 3f;
        }

        private float CalculateEmotionalValence()
        {
            // Positive valence from relaxation and focus, negative from stress
            float positiveValence = (currentBiometricState.relaxationLevel + currentBiometricState.focusLevel) * 0.5f;
            float negativeValence = currentBiometricState.stressLevel;
            
            return Mathf.Clamp01(positiveValence - negativeValence + 0.5f);
        }

        private float CalculateArousalLevel()
        {
            return (currentBiometricState.stressLevel + currentBiometricState.excitementLevel) * 0.5f;
        }

        private void AnalyzeBiometricPatterns()
        {
            // Store current reading in history
            var reading = new BiometricReading
            {
                state = currentBiometricState,
                confidence = CalculateReadingConfidence(),
                sensorSource = "integrated",
                timestamp = Time.time
            };

            biometricHistory.Enqueue(reading);

            // Maintain history size
            while (biometricHistory.Count > 100)
            {
                biometricHistory.Dequeue();
            }

            // Analyze trends if we have enough data
            if (biometricHistory.Count >= 10)
            {
                AnalyzeBiometricTrends();
            }
        }

        private float CalculateReadingConfidence()
        {
            // Calculate confidence based on sensor availability and signal quality
            float confidence = 0.5f; // Base confidence for simulated data

            if (heartRateMonitor != null && heartRateMonitor.IsConnected())
                confidence += 0.2f;
            if (eyeTrackingSystem != null && eyeTrackingSystem.IsConnected())
                confidence += 0.15f;
            if (gsrSensor != null && gsrSensor.IsConnected())
                confidence += 0.1f;
            if (eegMonitor != null && eegMonitor.IsConnected())
                confidence += 0.05f;

            return Mathf.Clamp01(confidence);
        }

        private void AnalyzeBiometricTrends()
        {
            var recentReadings = biometricHistory.TakeLast(10).ToArray();
            
            // Calculate trends
            float stressTrend = CalculateTrend(recentReadings.Select(r => r.state.stressLevel).ToArray());
            float excitementTrend = CalculateTrend(recentReadings.Select(r => r.state.excitementLevel).ToArray());
            float focusTrend = CalculateTrend(recentReadings.Select(r => r.state.focusLevel).ToArray());

            // Update adaptation weights based on trends
            UpdateAdaptationWeights(stressTrend, excitementTrend, focusTrend);
        }

        private float CalculateTrend(float[] values)
        {
            if (values.Length < 2) return 0f;

            float sum = 0f;
            for (int i = 1; i < values.Length; i++)
            {
                sum += values[i] - values[i - 1];
            }
            return sum / (values.Length - 1);
        }

        private void UpdateAdaptationWeights(float stressTrend, float excitementTrend, float focusTrend)
        {
            // Increase adaptation weights for trending biometric signals
            if (stressTrend > 0.1f)
            {
                adaptationWeights["lighting_stress"] = Mathf.Min(adaptationWeights["lighting_stress"] + 0.1f, 1f);
                adaptationWeights["audio_stress"] = Mathf.Min(adaptationWeights["audio_stress"] + 0.1f, 1f);
            }

            if (excitementTrend > 0.1f)
            {
                adaptationWeights["lighting_excitement"] = Mathf.Min(adaptationWeights["lighting_excitement"] + 0.1f, 1f);
                adaptationWeights["weather_excitement"] = Mathf.Min(adaptationWeights["weather_excitement"] + 0.1f, 1f);
            }

            if (focusTrend < -0.1f) // Decreasing focus
            {
                adaptationWeights["generation_focus"] = Mathf.Min(adaptationWeights["generation_focus"] + 0.1f, 1f);
            }
        }

        private void GenerateEnvironmentalAdaptations()
        {
            var adaptations = new List<EnvironmentalAdaptation>();

            // Generate lighting adaptations
            if (adaptLighting)
            {
                adaptations.AddRange(GenerateLightingAdaptations());
            }

            // Generate audio adaptations
            if (adaptAudio)
            {
                adaptations.AddRange(GenerateAudioAdaptations());
            }

            // Generate weather adaptations
            if (adaptWeather)
            {
                adaptations.AddRange(GenerateWeatherAdaptations());
            }

            // Generate procedural generation adaptations
            if (adaptGeneration)
            {
                adaptations.AddRange(GenerateGenerationAdaptations());
            }

            // Apply adaptations
            foreach (var adaptation in adaptations)
            {
                ApplyAdaptation(adaptation);
            }
        }

        private List<EnvironmentalAdaptation> GenerateLightingAdaptations()
        {
            var adaptations = new List<EnvironmentalAdaptation>();

            // Stress-based lighting adaptation
            if (currentBiometricState.stressLevel > stressThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "lighting_stress_reduction",
                    intensity = currentBiometricState.stressLevel * adaptationWeights["lighting_stress"],
                    targetColor = new Color(0.6f, 0.8f, 1f), // Cool, calming blue
                    confidence = 0.8f
                });
            }

            // Excitement-based lighting adaptation
            if (currentBiometricState.excitementLevel > excitementThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "lighting_excitement_enhancement",
                    intensity = currentBiometricState.excitementLevel * adaptationWeights["lighting_excitement"],
                    targetColor = new Color(1f, 0.4f, 0.8f), // Vibrant magenta
                    confidence = 0.7f
                });
            }

            // Focus-based lighting adaptation
            if (currentBiometricState.focusLevel > focusThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "lighting_focus_enhancement",
                    intensity = currentBiometricState.focusLevel * adaptationWeights["lighting_focus"],
                    targetColor = new Color(1f, 1f, 0.8f), // Warm white for focus
                    confidence = 0.6f
                });
            }

            return adaptations;
        }

        private List<EnvironmentalAdaptation> GenerateAudioAdaptations()
        {
            var adaptations = new List<EnvironmentalAdaptation>();

            // Stress-based audio adaptation
            if (currentBiometricState.stressLevel > stressThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "audio_stress_reduction",
                    intensity = currentBiometricState.stressLevel * adaptationWeights["audio_stress"],
                    targetVolume = 0.3f, // Reduce volume when stressed
                    confidence = 0.9f
                });
            }

            // Relaxation-based audio adaptation
            if (currentBiometricState.relaxationLevel > relaxationThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "audio_relaxation_enhancement",
                    intensity = currentBiometricState.relaxationLevel * adaptationWeights["audio_relaxation"],
                    targetVolume = 0.6f, // Moderate volume for relaxation
                    confidence = 0.7f
                });
            }

            return adaptations;
        }

        private List<EnvironmentalAdaptation> GenerateWeatherAdaptations()
        {
            var adaptations = new List<EnvironmentalAdaptation>();

            // Stress-based weather adaptation
            if (currentBiometricState.stressLevel > stressThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "weather_stress_reduction",
                    intensity = currentBiometricState.stressLevel * adaptationWeights["weather_stress"],
                    targetWeather = "CyberSnow", // Calming snow effect
                    confidence = 0.6f
                });
            }

            // Excitement-based weather adaptation
            if (currentBiometricState.excitementLevel > excitementThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "weather_excitement_enhancement",
                    intensity = currentBiometricState.excitementLevel * adaptationWeights["weather_excitement"],
                    targetWeather = "ElectricStorm", // Exciting storm effect
                    confidence = 0.5f
                });
            }

            return adaptations;
        }

        private List<EnvironmentalAdaptation> GenerateGenerationAdaptations()
        {
            var adaptations = new List<EnvironmentalAdaptation>();

            // Stress-based generation adaptation
            if (currentBiometricState.stressLevel > stressThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "generation_stress_reduction",
                    intensity = currentBiometricState.stressLevel * adaptationWeights["generation_stress"],
                    generationRate = 0.3f, // Reduce generation when stressed
                    confidence = 0.7f
                });
            }

            // Focus-based generation adaptation
            if (currentBiometricState.focusLevel < focusThreshold)
            {
                adaptations.Add(new EnvironmentalAdaptation
                {
                    adaptationType = "generation_focus_enhancement",
                    intensity = (1f - currentBiometricState.focusLevel) * adaptationWeights["generation_focus"],
                    generationRate = 0.8f, // Increase generation to regain focus
                    confidence = 0.8f
                });
            }

            return adaptations;
        }

        private void ApplyAdaptation(EnvironmentalAdaptation adaptation)
        {
            float adaptationStrength = adaptation.intensity * adaptation.confidence * adaptationSpeed;

            switch (adaptation.adaptationType)
            {
                case "lighting_stress_reduction":
                case "lighting_excitement_enhancement":
                case "lighting_focus_enhancement":
                    ApplyLightingAdaptation(adaptation, adaptationStrength);
                    break;

                case "audio_stress_reduction":
                case "audio_relaxation_enhancement":
                    ApplyAudioAdaptation(adaptation, adaptationStrength);
                    break;

                case "weather_stress_reduction":
                case "weather_excitement_enhancement":
                    ApplyWeatherAdaptation(adaptation, adaptationStrength);
                    break;

                case "generation_stress_reduction":
                case "generation_focus_enhancement":
                    ApplyGenerationAdaptation(adaptation, adaptationStrength);
                    break;
            }
        }

        private void ApplyLightingAdaptation(EnvironmentalAdaptation adaptation, float strength)
        {
            if (lightingEngine != null)
            {
                // Apply color adaptation
                lightingEngine.SetTargetColor(adaptation.targetColor, strength);
                
                // Apply intensity adaptation
                float targetIntensity = adaptation.intensity;
                lightingEngine.SetTargetIntensity(targetIntensity, strength);
            }
        }

        private void ApplyAudioAdaptation(EnvironmentalAdaptation adaptation, float strength)
        {
            if (audioEngine != null)
            {
                // Apply volume adaptation
                audioEngine.SetTargetVolume(adaptation.targetVolume, strength);
            }
        }

        private void ApplyWeatherAdaptation(EnvironmentalAdaptation adaptation, float strength)
        {
            if (weatherSystem != null && !string.IsNullOrEmpty(adaptation.targetWeather))
            {
                // Parse weather type
                if (System.Enum.TryParse<AIWeatherSystem.WeatherType>(adaptation.targetWeather, out var weatherType))
                {
                    weatherSystem.TriggerWeather(weatherType, adaptation.intensity);
                }
            }
        }

        private void ApplyGenerationAdaptation(EnvironmentalAdaptation adaptation, float strength)
        {
            if (proceduralGenerator != null)
            {
                // Apply generation rate adaptation
                proceduralGenerator.SetGenerationRate(adaptation.generationRate, strength);
            }
        }

        private void ApplyEnvironmentalAdaptations()
        {
            // This method is called from the main update loop
            // Individual adaptations are applied in ApplyAdaptation method
        }

        /// <summary>
        /// Get current biometric state for external systems
        /// </summary>
        public BiometricState GetCurrentBiometricState()
        {
            return currentBiometricState;
        }

        /// <summary>
        /// Get biometric history for analysis
        /// </summary>
        public BiometricReading[] GetBiometricHistory()
        {
            return biometricHistory.ToArray();
        }

        /// <summary>
        /// Manually set biometric sensitivity
        /// </summary>
        public void SetBiometricSensitivity(float sensitivity)
        {
            biometricSensitivity = Mathf.Clamp01(sensitivity);
            LogDebug($"Biometric sensitivity set to {biometricSensitivity}");
        }

        /// <summary>
        /// Enable or disable specific biometric sensors
        /// </summary>
        public void SetSensorEnabled(string sensorType, bool enabled)
        {
            switch (sensorType.ToLower())
            {
                case "heartrate":
                    enableHeartRateMonitoring = enabled;
                    break;
                case "eyetracking":
                    enableEyeTracking = enabled;
                    break;
                case "gsr":
                    enableGSRSensor = enabled;
                    break;
                case "eeg":
                    enableEEGMonitoring = enabled;
                    break;
            }

            LogDebug($"{sensorType} sensor {(enabled ? "enabled" : "disabled")}");
        }

        protected override void OnCleanup()
        {
            if (biometricUpdateCoroutine != null)
            {
                StopCoroutine(biometricUpdateCoroutine);
                biometricUpdateCoroutine = null;
            }

            // Clean up biometric sensors
            heartRateMonitor?.Dispose();
            eyeTrackingSystem?.Dispose();
            gsrSensor?.Dispose();
            eegMonitor?.Dispose();
            dataProcessor?.Dispose();

            LogDebug("Biometric Response System cleaned up");
        }
    }

    // Supporting classes for biometric sensors (simplified implementations)
    public class HeartRateMonitor
    {
        private bool isConnected = false;

        public void Initialize()
        {
            // In a real implementation, this would connect to actual hardware
            isConnected = true;
        }

        public bool IsConnected() => isConnected;

        public HeartRateData GetCurrentReading()
        {
            return new HeartRateData
            {
                heartRate = 70f + Random.Range(-10f, 20f),
                variability = Random.Range(0.3f, 0.8f)
            };
        }

        public void Dispose() { isConnected = false; }
    }

    public class HeartRateData
    {
        public float heartRate;
        public float variability;
    }

    public class EyeTrackingSystem
    {
        private bool isConnected = false;

        public void Initialize() { isConnected = true; }
        public bool IsConnected() => isConnected;

        public EyeTrackingData GetCurrentReading()
        {
            return new EyeTrackingData
            {
                gazePosition = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)),
                blinkRate = Random.Range(10f, 25f),
                pupilDilation = Random.Range(0.3f, 0.8f)
            };
        }

        public void Dispose() { isConnected = false; }
    }

    public class EyeTrackingData
    {
        public Vector2 gazePosition;
        public float blinkRate;
        public float pupilDilation;
    }

    public class GSRSensor
    {
        private bool isConnected = false;

        public void Initialize() { isConnected = true; }
        public bool IsConnected() => isConnected;

        public GSRData GetCurrentReading()
        {
            return new GSRData
            {
                conductance = Random.Range(0.2f, 0.8f)
            };
        }

        public void Dispose() { isConnected = false; }
    }

    public class GSRData
    {
        public float conductance;
    }

    public class EEGMonitor
    {
        private bool isConnected = false;

        public void Initialize() { isConnected = true; }
        public bool IsConnected() => isConnected;

        public EEGData GetCurrentReading()
        {
            return new EEGData
            {
                bandPowers = new float[]
                {
                    Random.Range(0.2f, 0.6f), // Alpha
                    Random.Range(0.3f, 0.7f), // Beta
                    Random.Range(0.1f, 0.4f), // Theta
                    Random.Range(0.05f, 0.2f) // Delta
                }
            };
        }

        public void Dispose() { isConnected = false; }
    }

    public class EEGData
    {
        public float[] bandPowers;
    }

    public class BiometricDataProcessor
    {
        public void Dispose() { }
    }
}