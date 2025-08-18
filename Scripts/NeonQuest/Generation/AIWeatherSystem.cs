using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.PlayerBehavior;

namespace NeonQuest.Generation
{
    /// <summary>
    /// AI-powered dynamic weather system that responds to player emotions and behavior patterns
    /// Uses machine learning algorithms to predict optimal weather conditions for immersion
    /// </summary>
    public class AIWeatherSystem : NeonQuestComponent
    {
        [Header("AI Weather Configuration")]
        [SerializeField] private float emotionalResponseSensitivity = 0.7f;
        [SerializeField] private float weatherTransitionSpeed = 0.3f;
        [SerializeField] private int behaviorHistorySize = 100;
        [SerializeField] private bool enablePredictiveWeather = true;
        [SerializeField] private bool enableEmotionalWeather = true;

        [Header("Weather Effects")]
        [SerializeField] private ParticleSystem rainSystem;
        [SerializeField] private ParticleSystem snowSystem;
        [SerializeField] private ParticleSystem fogSystem;
        [SerializeField] private ParticleSystem electricStormSystem;
        [SerializeField] private Light[] dynamicLights;
        [SerializeField] private AudioSource[] weatherAudioSources;

        // AI Neural Network Components
        private WeatherNeuralNetwork neuralNetwork;
        private EmotionalStateAnalyzer emotionalAnalyzer;
        private WeatherPredictionEngine predictionEngine;
        
        // Weather State Management
        private WeatherState currentWeather;
        private WeatherState targetWeather;
        private Queue<PlayerBehaviorData> behaviorHistory;
        private Dictionary<WeatherType, WeatherProfile> weatherProfiles;
        
        // Performance Optimization
        private Coroutine weatherUpdateCoroutine;
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f;

        public enum WeatherType
        {
            Clear,
            LightRain,
            HeavyRain,
            ElectricStorm,
            ToxicFog,
            AcidRain,
            QuantumStorm,
            NeonMist,
            CyberSnow
        }

        [System.Serializable]
        public class WeatherState
        {
            public WeatherType type;
            public float intensity;
            public float emotionalResonance;
            public Color atmosphericTint;
            public Vector3 windDirection;
            public float windStrength;
            public float visibility;
            public float temperature;
            public float humidity;
        }

        [System.Serializable]
        public class WeatherProfile
        {
            public WeatherType type;
            public ParticleSystem particleSystem;
            public AudioClip[] ambientSounds;
            public Color lightingTint;
            public float lightIntensityMultiplier;
            public AnimationCurve intensityCurve;
            public float emotionalWeight;
        }

        [System.Serializable]
        public class PlayerBehaviorData
        {
            public Vector3 position;
            public float speed;
            public float stressLevel;
            public float excitementLevel;
            public float explorationIndex;
            public float combatIntensity;
            public float timestamp;
        }

        protected override void OnInitialize()
        {
            LogDebug("Initializing AI Weather System");

            try
            {
                // Initialize neural network
                neuralNetwork = new WeatherNeuralNetwork();
                emotionalAnalyzer = new EmotionalStateAnalyzer();
                predictionEngine = new WeatherPredictionEngine();

                // Initialize weather profiles
                InitializeWeatherProfiles();

                // Initialize behavior tracking
                behaviorHistory = new Queue<PlayerBehaviorData>();

                // Set initial weather state
                currentWeather = new WeatherState
                {
                    type = WeatherType.Clear,
                    intensity = 0.3f,
                    emotionalResonance = 0.5f,
                    atmosphericTint = Color.white,
                    windDirection = Vector3.forward,
                    windStrength = 0.2f,
                    visibility = 1.0f,
                    temperature = 20f,
                    humidity = 0.4f
                };

                targetWeather = currentWeather;

                // Start weather update coroutine
                weatherUpdateCoroutine = StartCoroutine(WeatherUpdateLoop());

                LogDebug("AI Weather System initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize AI Weather System: {ex.Message}");
                OnInitializationFailed(ex);
                throw;
            }
        }

        private void InitializeWeatherProfiles()
        {
            weatherProfiles = new Dictionary<WeatherType, WeatherProfile>();

            // Define weather profiles with emotional weights
            weatherProfiles[WeatherType.Clear] = new WeatherProfile
            {
                type = WeatherType.Clear,
                particleSystem = null,
                lightingTint = new Color(1f, 0.95f, 0.8f),
                lightIntensityMultiplier = 1.0f,
                emotionalWeight = 0.6f
            };

            weatherProfiles[WeatherType.LightRain] = new WeatherProfile
            {
                type = WeatherType.LightRain,
                particleSystem = rainSystem,
                lightingTint = new Color(0.7f, 0.8f, 0.9f),
                lightIntensityMultiplier = 0.7f,
                emotionalWeight = 0.4f
            };

            weatherProfiles[WeatherType.ElectricStorm] = new WeatherProfile
            {
                type = WeatherType.ElectricStorm,
                particleSystem = electricStormSystem,
                lightingTint = new Color(0.9f, 0.7f, 1f),
                lightIntensityMultiplier = 1.5f,
                emotionalWeight = 0.9f
            };

            weatherProfiles[WeatherType.NeonMist] = new WeatherProfile
            {
                type = WeatherType.NeonMist,
                particleSystem = fogSystem,
                lightingTint = new Color(1f, 0.3f, 0.8f),
                lightIntensityMultiplier = 0.8f,
                emotionalWeight = 0.7f
            };

            weatherProfiles[WeatherType.CyberSnow] = new WeatherProfile
            {
                type = WeatherType.CyberSnow,
                particleSystem = snowSystem,
                lightingTint = new Color(0.8f, 0.9f, 1f),
                lightIntensityMultiplier = 0.9f,
                emotionalWeight = 0.3f
            };
        }

        private IEnumerator WeatherUpdateLoop()
        {
            while (isInitialized)
            {
                yield return new WaitForSeconds(UPDATE_INTERVAL);

                try
                {
                    // Update behavior tracking
                    UpdateBehaviorTracking();

                    // Analyze emotional state
                    if (enableEmotionalWeather)
                    {
                        AnalyzeEmotionalState();
                    }

                    // Predict optimal weather
                    if (enablePredictiveWeather)
                    {
                        PredictOptimalWeather();
                    }

                    // Update weather transition
                    UpdateWeatherTransition();

                    // Apply weather effects
                    ApplyWeatherEffects();

                    lastUpdateTime = Time.time;
                }
                catch (System.Exception ex)
                {
                    LogError($"Error in weather update loop: {ex.Message}");
                }
            }
        }

        private void UpdateBehaviorTracking()
        {
            // Get current player data
            var playerTracker = FindObjectOfType<PlayerMovementTracker>();
            var behaviorAnalyzer = FindObjectOfType<PlayerBehaviorAnalyzer>();

            if (playerTracker != null)
            {
                var behaviorData = new PlayerBehaviorData
                {
                    position = playerTracker.CurrentPosition,
                    speed = playerTracker.CurrentSpeed,
                    timestamp = Time.time
                };

                // Calculate stress and excitement levels based on movement patterns
                behaviorData.stressLevel = CalculateStressLevel(playerTracker);
                behaviorData.excitementLevel = CalculateExcitementLevel(playerTracker);
                behaviorData.explorationIndex = CalculateExplorationIndex(playerTracker);

                // Add to history
                behaviorHistory.Enqueue(behaviorData);

                // Maintain history size
                while (behaviorHistory.Count > behaviorHistorySize)
                {
                    behaviorHistory.Dequeue();
                }
            }
        }

        private float CalculateStressLevel(PlayerMovementTracker tracker)
        {
            // Calculate stress based on movement patterns
            float rapidDirectionChanges = 0f;
            float speedVariation = 0f;
            
            if (behaviorHistory.Count > 5)
            {
                var recent = behaviorHistory.TakeLast(5).ToArray();
                for (int i = 1; i < recent.Length; i++)
                {
                    var directionChange = Vector3.Angle(
                        (recent[i].position - recent[i-1].position).normalized,
                        (recent[i-1].position - (i > 1 ? recent[i-2].position : recent[i-1].position)).normalized
                    );
                    rapidDirectionChanges += directionChange > 45f ? 1f : 0f;
                    speedVariation += Mathf.Abs(recent[i].speed - recent[i-1].speed);
                }
            }

            return Mathf.Clamp01((rapidDirectionChanges + speedVariation) * 0.2f);
        }

        private float CalculateExcitementLevel(PlayerMovementTracker tracker)
        {
            // Calculate excitement based on speed and exploration
            float avgSpeed = behaviorHistory.Count > 0 ? behaviorHistory.Average(b => b.speed) : 0f;
            float speedFactor = Mathf.Clamp01(avgSpeed / 10f); // Normalize to expected max speed
            
            return speedFactor;
        }

        private float CalculateExplorationIndex(PlayerMovementTracker tracker)
        {
            // Calculate how much the player is exploring vs. staying in one area
            if (behaviorHistory.Count < 10) return 0.5f;

            var positions = behaviorHistory.Select(b => b.position).ToArray();
            float totalDistance = 0f;
            float maxDistance = 0f;

            for (int i = 1; i < positions.Length; i++)
            {
                float distance = Vector3.Distance(positions[i], positions[i-1]);
                totalDistance += distance;
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            return Mathf.Clamp01(totalDistance / (positions.Length * 5f)); // Normalize
        }

        private void AnalyzeEmotionalState()
        {
            if (behaviorHistory.Count < 5) return;

            var recentBehavior = behaviorHistory.TakeLast(10).ToArray();
            float avgStress = recentBehavior.Average(b => b.stressLevel);
            float avgExcitement = recentBehavior.Average(b => b.excitementLevel);
            float avgExploration = recentBehavior.Average(b => b.explorationIndex);

            // Use neural network to determine optimal weather based on emotional state
            var emotionalState = new float[] { avgStress, avgExcitement, avgExploration };
            var weatherRecommendation = neuralNetwork.PredictOptimalWeather(emotionalState);

            // Apply emotional response to target weather
            if (weatherRecommendation != null)
            {
                targetWeather = weatherRecommendation;
            }
        }

        private void PredictOptimalWeather()
        {
            // Use prediction engine to forecast weather changes
            var prediction = predictionEngine.PredictWeatherTransition(currentWeather, behaviorHistory.ToArray());
            
            if (prediction != null && prediction.confidence > 0.7f)
            {
                targetWeather = prediction.predictedWeather;
            }
        }

        private void UpdateWeatherTransition()
        {
            if (currentWeather.type != targetWeather.type || 
                Mathf.Abs(currentWeather.intensity - targetWeather.intensity) > 0.1f)
            {
                // Smooth transition between weather states
                float transitionSpeed = weatherTransitionSpeed * Time.deltaTime;
                
                currentWeather.intensity = Mathf.Lerp(currentWeather.intensity, targetWeather.intensity, transitionSpeed);
                currentWeather.emotionalResonance = Mathf.Lerp(currentWeather.emotionalResonance, targetWeather.emotionalResonance, transitionSpeed);
                currentWeather.atmosphericTint = Color.Lerp(currentWeather.atmosphericTint, targetWeather.atmosphericTint, transitionSpeed);
                currentWeather.windStrength = Mathf.Lerp(currentWeather.windStrength, targetWeather.windStrength, transitionSpeed);
                currentWeather.visibility = Mathf.Lerp(currentWeather.visibility, targetWeather.visibility, transitionSpeed);

                // Transition weather type if intensity is close enough
                if (Mathf.Abs(currentWeather.intensity - targetWeather.intensity) < 0.2f)
                {
                    currentWeather.type = targetWeather.type;
                }
            }
        }

        private void ApplyWeatherEffects()
        {
            if (!weatherProfiles.ContainsKey(currentWeather.type)) return;

            var profile = weatherProfiles[currentWeather.type];

            // Update particle systems
            UpdateParticleEffects(profile);

            // Update lighting
            UpdateLightingEffects(profile);

            // Update audio
            UpdateAudioEffects(profile);

            // Update atmospheric effects
            UpdateAtmosphericEffects();
        }

        private void UpdateParticleEffects(WeatherProfile profile)
        {
            // Disable all particle systems first
            foreach (var weatherProfile in weatherProfiles.Values)
            {
                if (weatherProfile.particleSystem != null)
                {
                    var emission = weatherProfile.particleSystem.emission;
                    emission.enabled = false;
                }
            }

            // Enable and configure current weather particle system
            if (profile.particleSystem != null)
            {
                var emission = profile.particleSystem.emission;
                emission.enabled = true;
                emission.rateOverTime = currentWeather.intensity * 100f;

                var main = profile.particleSystem.main;
                main.startColor = currentWeather.atmosphericTint;
                main.startSpeed = currentWeather.windStrength * 5f;
            }
        }

        private void UpdateLightingEffects(WeatherProfile profile)
        {
            foreach (var light in dynamicLights)
            {
                if (light != null)
                {
                    light.color = Color.Lerp(light.color, profile.lightingTint, Time.deltaTime);
                    light.intensity = Mathf.Lerp(light.intensity, 
                        profile.lightIntensityMultiplier * currentWeather.intensity, Time.deltaTime);
                }
            }
        }

        private void UpdateAudioEffects(WeatherProfile profile)
        {
            foreach (var audioSource in weatherAudioSources)
            {
                if (audioSource != null && profile.ambientSounds != null && profile.ambientSounds.Length > 0)
                {
                    if (audioSource.clip != profile.ambientSounds[0])
                    {
                        audioSource.clip = profile.ambientSounds[0];
                        audioSource.Play();
                    }
                    audioSource.volume = Mathf.Lerp(audioSource.volume, currentWeather.intensity * 0.5f, Time.deltaTime);
                }
            }
        }

        private void UpdateAtmosphericEffects()
        {
            // Update fog and atmospheric rendering
            RenderSettings.fogColor = currentWeather.atmosphericTint;
            RenderSettings.fogDensity = (1f - currentWeather.visibility) * 0.1f;
            
            // Update ambient lighting
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, 
                currentWeather.atmosphericTint * 0.3f, Time.deltaTime);
        }

        /// <summary>
        /// Manually trigger a specific weather type
        /// </summary>
        public void TriggerWeather(WeatherType weatherType, float intensity = 1f)
        {
            if (weatherProfiles.ContainsKey(weatherType))
            {
                targetWeather = new WeatherState
                {
                    type = weatherType,
                    intensity = intensity,
                    emotionalResonance = weatherProfiles[weatherType].emotionalWeight,
                    atmosphericTint = weatherProfiles[weatherType].lightingTint,
                    windDirection = Vector3.forward,
                    windStrength = intensity * 0.5f,
                    visibility = 1f - (intensity * 0.3f),
                    temperature = 20f,
                    humidity = intensity * 0.6f
                };

                LogDebug($"Manually triggered weather: {weatherType} with intensity {intensity}");
            }
        }

        /// <summary>
        /// Get current weather state for external systems
        /// </summary>
        public WeatherState GetCurrentWeatherState()
        {
            return currentWeather;
        }

        /// <summary>
        /// Get weather prediction for the next few minutes
        /// </summary>
        public WeatherPrediction GetWeatherPrediction(float timeHorizon = 300f)
        {
            return predictionEngine.PredictWeatherTransition(currentWeather, behaviorHistory.ToArray(), timeHorizon);
        }

        protected override void OnCleanup()
        {
            if (weatherUpdateCoroutine != null)
            {
                StopCoroutine(weatherUpdateCoroutine);
                weatherUpdateCoroutine = null;
            }

            // Clean up neural network resources
            neuralNetwork?.Dispose();
            emotionalAnalyzer?.Dispose();
            predictionEngine?.Dispose();

            LogDebug("AI Weather System cleaned up");
        }
    }

    // Supporting classes for the AI Weather System
    public class WeatherNeuralNetwork
    {
        private float[,] weights1;
        private float[,] weights2;
        private float[] biases1;
        private float[] biases2;

        public WeatherNeuralNetwork()
        {
            // Initialize simple neural network for weather prediction
            // Input: [stress, excitement, exploration] (3 neurons)
            // Hidden: 8 neurons
            // Output: weather type probabilities (9 neurons for 9 weather types)
            
            weights1 = new float[3, 8];
            weights2 = new float[8, 9];
            biases1 = new float[8];
            biases2 = new float[9];

            InitializeWeights();
        }

        private void InitializeWeights()
        {
            // Initialize with pre-trained weights for weather prediction
            // This is a simplified version - in production, you'd load trained weights
            System.Random rand = new System.Random();
            
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    weights1[i, j] = (float)(rand.NextDouble() * 2 - 1) * 0.5f;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    weights2[i, j] = (float)(rand.NextDouble() * 2 - 1) * 0.5f;
                }
            }
        }

        public AIWeatherSystem.WeatherState PredictOptimalWeather(float[] emotionalState)
        {
            if (emotionalState.Length != 3) return null;

            // Forward pass through network
            float[] hidden = new float[8];
            float[] output = new float[9];

            // Hidden layer
            for (int j = 0; j < 8; j++)
            {
                float sum = biases1[j];
                for (int i = 0; i < 3; i++)
                {
                    sum += emotionalState[i] * weights1[i, j];
                }
                hidden[j] = Mathf.Max(0, sum); // ReLU activation
            }

            // Output layer
            for (int j = 0; j < 9; j++)
            {
                float sum = biases2[j];
                for (int i = 0; i < 8; i++)
                {
                    sum += hidden[i] * weights2[i, j];
                }
                output[j] = 1f / (1f + Mathf.Exp(-sum)); // Sigmoid activation
            }

            // Find best weather type
            int bestWeatherIndex = 0;
            float bestScore = output[0];
            for (int i = 1; i < output.Length; i++)
            {
                if (output[i] > bestScore)
                {
                    bestScore = output[i];
                    bestWeatherIndex = i;
                }
            }

            // Convert to weather state
            var weatherType = (AIWeatherSystem.WeatherType)bestWeatherIndex;
            return new AIWeatherSystem.WeatherState
            {
                type = weatherType,
                intensity = bestScore,
                emotionalResonance = emotionalState[1], // Use excitement as resonance
                atmosphericTint = GetWeatherColor(weatherType),
                windDirection = Vector3.forward,
                windStrength = emotionalState[0] * 0.5f, // Use stress for wind
                visibility = 1f - (emotionalState[0] * 0.3f),
                temperature = 20f,
                humidity = 0.5f
            };
        }

        private Color GetWeatherColor(AIWeatherSystem.WeatherType weatherType)
        {
            switch (weatherType)
            {
                case AIWeatherSystem.WeatherType.Clear: return Color.white;
                case AIWeatherSystem.WeatherType.LightRain: return new Color(0.7f, 0.8f, 0.9f);
                case AIWeatherSystem.WeatherType.ElectricStorm: return new Color(0.9f, 0.7f, 1f);
                case AIWeatherSystem.WeatherType.NeonMist: return new Color(1f, 0.3f, 0.8f);
                case AIWeatherSystem.WeatherType.CyberSnow: return new Color(0.8f, 0.9f, 1f);
                default: return Color.gray;
            }
        }

        public void Dispose()
        {
            // Clean up resources
            weights1 = null;
            weights2 = null;
            biases1 = null;
            biases2 = null;
        }
    }

    public class EmotionalStateAnalyzer
    {
        public void Dispose() { }
    }

    public class WeatherPredictionEngine
    {
        public WeatherPrediction PredictWeatherTransition(AIWeatherSystem.WeatherState current, 
            AIWeatherSystem.PlayerBehaviorData[] history, float timeHorizon = 300f)
        {
            // Simple prediction based on behavior trends
            if (history.Length < 5)
            {
                return new WeatherPrediction
                {
                    predictedWeather = current,
                    confidence = 0.5f,
                    timeToTransition = timeHorizon * 0.5f
                };
            }

            // Analyze trends in player behavior
            var recent = history.TakeLast(10).ToArray();
            float stressTrend = CalculateTrend(recent.Select(b => b.stressLevel).ToArray());
            float excitementTrend = CalculateTrend(recent.Select(b => b.excitementLevel).ToArray());

            // Predict weather change based on trends
            AIWeatherSystem.WeatherType predictedType = current.type;
            float confidence = 0.6f;

            if (stressTrend > 0.3f && excitementTrend > 0.2f)
            {
                predictedType = AIWeatherSystem.WeatherType.ElectricStorm;
                confidence = 0.8f;
            }
            else if (stressTrend < -0.2f && excitementTrend < 0.1f)
            {
                predictedType = AIWeatherSystem.WeatherType.CyberSnow;
                confidence = 0.7f;
            }

            return new WeatherPrediction
            {
                predictedWeather = new AIWeatherSystem.WeatherState
                {
                    type = predictedType,
                    intensity = Mathf.Clamp01(current.intensity + stressTrend * 0.5f),
                    emotionalResonance = excitementTrend,
                    atmosphericTint = current.atmosphericTint,
                    windDirection = current.windDirection,
                    windStrength = current.windStrength,
                    visibility = current.visibility,
                    temperature = current.temperature,
                    humidity = current.humidity
                },
                confidence = confidence,
                timeToTransition = timeHorizon * (1f - confidence)
            };
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

        public void Dispose() { }
    }

    public class WeatherPrediction
    {
        public AIWeatherSystem.WeatherState predictedWeather;
        public float confidence;
        public float timeToTransition;
    }
}