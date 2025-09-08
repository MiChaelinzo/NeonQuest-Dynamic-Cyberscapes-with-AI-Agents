using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.Effects;

namespace NeonQuest.TimeTravel
{
    /// <summary>
    /// Revolutionary Temporal Manipulation Engine
    /// Enables time travel, temporal loops, and timeline manipulation
    /// Control the flow of time itself within the game universe
    /// </summary>
    public class TemporalManipulationEngine : NeonQuestComponent
    {
        [Header("⏰ Temporal Configuration")]
        [SerializeField] private bool enableTimeTravel = true;
        [SerializeField] private bool enableTemporalLoops = true;
        [SerializeField] private bool enableTimelineManipulation = true;
        [SerializeField] private bool enableParadoxPrevention = true;
        
        [Header("🕐 Time Parameters")]
        [SerializeField] private float maxTimeDistortion = 10f;
        [SerializeField] private float temporalEnergyCapacity = 1000f;
        [SerializeField] private int maxActiveTimelines = 5;
        [SerializeField] private float paradoxThreshold = 0.8f;
        
        // Temporal Components
        private Dictionary<string, Timeline> activeTimelines;
        private Dictionary<string, TemporalLoop> activeLoops;
        private List<TimelineSnapshot> timelineHistory;
        private TemporalNavigator navigator;
        private ParadoxDetector paradoxDetector;
        private ChronoStabilizer chronoStabilizer;
        
        // Current State
        private Timeline currentTimeline;
        private float temporalEnergy;
        private float currentTimeDistortion;
        private Dictionary<GameObject, TemporalProperties> temporalObjects;
        private List<TemporalAnomaly> activeAnomalies;
        
        protected override void OnInitialize()
        {
            LogDebug("⏰ Initializing Temporal Manipulation Engine");
            
            try
            {
                InitializeTemporalSystem();
                
                navigator = new TemporalNavigator();
                paradoxDetector = new ParadoxDetector(paradoxThreshold);
                chronoStabilizer = new ChronoStabilizer();
                
                // Create the prime timeline
                CreatePrimeTimeline();
                
                LogDebug("✅ Temporal Manipulation Engine initialized");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Failed to initialize Temporal Manipulation Engine: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeTemporalSystem()
        {
            activeTimelines = new Dictionary<string, Timeline>();
            activeLoops = new Dictionary<string, TemporalLoop>();
            timelineHistory = new List<TimelineSnapshot>();
            temporalObjects = new Dictionary<GameObject, TemporalProperties>();
            activeAnomalies = new List<TemporalAnomaly>();
            
            temporalEnergy = temporalEnergyCapacity;
            currentTimeDistortion = 1f;
        }
        
        private void CreatePrimeTimeline()
        {
            currentTimeline = new Timeline
            {
                timelineId = "PRIME_TIMELINE",
                timelineName = "Prime Timeline",
                creationTime = Time.time,
                currentTimeIndex = 0f,
                timeFlow = 1f,
                stabilityIndex = 1f,
                branchPoint = -1f,
                parentTimelineId = null,
                isActive = true,
                accessLevel = TimelineAccessLevel.Public,
                temporalEvents = new List<TemporalEvent>()
            };
            
            activeTimelines[currentTimeline.timelineId] = currentTimeline;
            
            // Create initial snapshot
            CreateTimelineSnapshot("Timeline Initialization");
            
            LogDebug($"🕐 Prime timeline created: {currentTimeline.timelineName}");
        }
        
        void Update()
        {
            if (!isInitialized || !enableTimeTravel) return;
            
            // Update temporal energy
            UpdateTemporalEnergy();
            
            // Process time distortion
            ProcessTimeDistortion();
            
            // Update temporal loops
            if (enableTemporalLoops)
            {
                ProcessTemporalLoops();
            }
            
            // Monitor timeline stability
            MonitorTimelineStability();
            
            // Detect and handle paradoxes
            if (enableParadoxPrevention)
            {
                DetectTemporalParadoxes();
            }
            
            // Process temporal anomalies
            ProcessTemporalAnomalies();
            
            // Update temporal objects
            UpdateTemporalObjects();
        }
        
        public void TravelToTime(float targetTime, GameObject traveler = null)
        {
            if (!CanTravelToTime(targetTime))
            {
                LogWarning($"⚠️ Cannot travel to time: {targetTime}");
                return;
            }
            
            LogDebug($"🚀 Initiating time travel to: {targetTime}");
            
            // Calculate energy cost
            float energyCost = Mathf.Abs(targetTime - currentTimeline.currentTimeIndex) * 10f;
            
            if (temporalEnergy < energyCost)
            {
                LogWarning("⚠️ Insufficient temporal energy for time travel");
                return;
            }
            
            // Create temporal transition effect
            CreateTimeTransitionEffect(traveler?.transform.position ?? transform.position);
            
            // Store current state
            CreateTimelineSnapshot($"Time Travel from {currentTimeline.currentTimeIndex} to {targetTime}");
            
            // Perform time travel
            PerformTimeTravel(targetTime, traveler);
            
            // Consume energy
            temporalEnergy -= energyCost;
            
            LogDebug($"✅ Time travel completed to: {targetTime}");
        }
        
        private bool CanTravelToTime(float targetTime)
        {
            // Check if target time is within bounds
            if (targetTime < 0f || targetTime > Time.time + 3600f) // Max 1 hour into future
            {
                return false;
            }
            
            // Check temporal energy
            float energyCost = Mathf.Abs(targetTime - currentTimeline.currentTimeIndex) * 10f;
            if (temporalEnergy < energyCost)
            {
                return false;
            }
            
            // Check for temporal locks
            foreach (var anomaly in activeAnomalies)
            {
                if (anomaly.anomalyType == TemporalAnomalyType.TemporalLock && 
                    Mathf.Abs(anomaly.timeIndex - targetTime) < 60f)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void CreateTimeTransitionEffect(Vector3 position)
        {
            var transitionEffect = new GameObject("TimeTransition");
            transitionEffect.transform.position = position;
            
            var particleSystem = transitionEffect.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.8f);
            main.startSize = 3f;
            main.maxParticles = 300;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;
            
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
            
            Destroy(transitionEffect, 3f);
            
            LogDebug($"✨ Time transition effect created at {position}");
        }
        
        private void PerformTimeTravel(float targetTime, GameObject traveler)
        {
            // Update timeline time index
            float previousTime = currentTimeline.currentTimeIndex;
            currentTimeline.currentTimeIndex = targetTime;
            
            // Create temporal event
            var temporalEvent = new TemporalEvent
            {
                eventId = System.Guid.NewGuid().ToString(),
                eventType = TemporalEventType.TimeTravel,
                timeIndex = targetTime,
                description = $"Time travel from {previousTime} to {targetTime}",
                causedBy = traveler?.name ?? "System",
                timestamp = Time.time
            };
            
            currentTimeline.temporalEvents.Add(temporalEvent);
            
            // Apply temporal effects to environment
            ApplyTemporalEnvironmentEffects(targetTime);
            
            // Check for timeline branching
            if (ShouldCreateTimelineBranch(targetTime))
            {
                CreateTimelineBranch(targetTime);
            }
        }
        
        private void ApplyTemporalEnvironmentEffects(float targetTime)
        {
            // Apply time-based environmental changes
            var allObjects = FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                ApplyTemporalEffectsToObject(obj, targetTime);
            }
            
            // Update lighting based on time
            UpdateTemporalLighting(targetTime);
            
            // Update weather based on time
            UpdateTemporalWeather(targetTime);
        }
        
        private void ApplyTemporalEffectsToObject(GameObject obj, float targetTime)
        {
            if (!temporalObjects.ContainsKey(obj))
            {
                temporalObjects[obj] = new TemporalProperties
                {
                    originalPosition = obj.transform.position,
                    originalRotation = obj.transform.rotation,
                    originalScale = obj.transform.localScale,
                    temporalInfluence = 0f,
                    lastUpdateTime = Time.time
                };
            }
            
            var props = temporalObjects[obj];
            
            // Calculate temporal influence based on time difference
            float timeDifference = Mathf.Abs(targetTime - Time.time);
            props.temporalInfluence = Mathf.Clamp01(timeDifference / 3600f); // Max influence at 1 hour difference
            
            // Apply temporal transformations
            ApplyTemporalTransformations(obj, props, targetTime);
        }
        
        private void ApplyTemporalTransformations(GameObject obj, TemporalProperties props, float targetTime)
        {
            float influence = props.temporalInfluence;
            
            // Position changes based on temporal flow
            Vector3 temporalOffset = new Vector3(
                Mathf.Sin(targetTime * 0.1f) * influence,
                Mathf.Cos(targetTime * 0.08f) * influence * 0.5f,
                Mathf.Sin(targetTime * 0.12f) * influence
            );
            
            obj.transform.position = Vector3.Lerp(obj.transform.position, 
                props.originalPosition + temporalOffset, Time.deltaTime);
            
            // Scale changes based on temporal distortion
            float scaleModifier = 1f + Mathf.Sin(targetTime * 0.05f) * influence * 0.2f;
            obj.transform.localScale = Vector3.Lerp(obj.transform.localScale, 
                props.originalScale * scaleModifier, Time.deltaTime);
            
            // Color changes based on temporal energy
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color temporalColor = Color.Lerp(Color.white, 
                    new Color(0.8f, 0.9f, 1f), influence * 0.5f);
                renderer.material.color = Color.Lerp(renderer.material.color, temporalColor, Time.deltaTime);
            }
        }
        
        private void UpdateTemporalLighting(float targetTime)
        {
            // Simulate day/night cycle based on temporal time
            float dayProgress = (targetTime % 86400f) / 86400f; // 24 hours in seconds
            
            var sunLight = FindObjectOfType<Light>();
            if (sunLight != null)
            {
                // Update sun intensity and color based on time of day
                float intensity = Mathf.Clamp01(Mathf.Sin(dayProgress * Mathf.PI * 2f));
                sunLight.intensity = intensity;
                
                Color dayColor = new Color(1f, 0.95f, 0.8f);
                Color nightColor = new Color(0.2f, 0.3f, 0.8f);
                sunLight.color = Color.Lerp(nightColor, dayColor, intensity);
            }
        }
        
        private void UpdateTemporalWeather(float targetTime)
        {
            // Simulate weather changes based on temporal time
            // This would integrate with weather systems
            LogDebug($"🌤️ Temporal weather updated for time: {targetTime}");
        }
        
        private bool ShouldCreateTimelineBranch(float targetTime)
        {
            // Check if traveling to past creates significant changes
            if (targetTime < Time.time - 300f) // 5 minutes in past
            {
                return Random.value < 0.3f; // 30% chance of branching
            }
            
            return false;
        }
        
        private void CreateTimelineBranch(float branchPoint)
        {
            if (activeTimelines.Count >= maxActiveTimelines)
            {
                LogWarning("⚠️ Maximum timeline limit reached");
                return;
            }
            
            var branchTimeline = new Timeline
            {
                timelineId = System.Guid.NewGuid().ToString(),
                timelineName = $"Branch Timeline {activeTimelines.Count}",
                creationTime = Time.time,
                currentTimeIndex = branchPoint,
                timeFlow = 1f,
                stabilityIndex = 0.8f,
                branchPoint = branchPoint,
                parentTimelineId = currentTimeline.timelineId,
                isActive = false,
                accessLevel = TimelineAccessLevel.Public,
                temporalEvents = new List<TemporalEvent>(currentTimeline.temporalEvents)
            };
            
            activeTimelines[branchTimeline.timelineId] = branchTimeline;
            
            LogDebug($"🌿 Timeline branch created at time: {branchPoint}");
        }
        
        public void CreateTemporalLoop(Vector3 position, float duration, float radius = 10f)
        {
            if (temporalEnergy < 200f)
            {
                LogWarning("⚠️ Insufficient temporal energy for loop creation");
                return;
            }
            
            var loop = new TemporalLoop
            {
                loopId = System.Guid.NewGuid().ToString(),
                position = position,
                radius = radius,
                duration = duration,
                loopCount = 0,
                maxLoops = Random.Range(3, 10),
                startTime = Time.time,
                isActive = true,
                loopType = TemporalLoopType.Spatial
            };
            
            activeLoops[loop.loopId] = loop;
            
            // Create loop visual effect
            CreateTemporalLoopEffect(loop);
            
            // Consume energy
            temporalEnergy -= 200f;
            
            LogDebug($"🔄 Temporal loop created at {position} for {duration} seconds");
        }
        
        private void CreateTemporalLoopEffect(TemporalLoop loop)
        {
            var loopEffect = new GameObject($"TemporalLoop_{loop.loopId}");
            loopEffect.transform.position = loop.position;
            
            var particleSystem = loopEffect.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startColor = new Color(1f, 0.8f, 0.2f, 0.6f);
            main.startSize = 1f;
            main.maxParticles = 150;
            main.startLifetime = loop.duration;
            main.startSpeed = 3f;
            
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = loop.radius;
            
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(0.5f);
            
            // Add collider for loop detection
            var collider = loopEffect.AddComponent<SphereCollider>();
            collider.radius = loop.radius;
            collider.isTrigger = true;
            
            var loopTrigger = loopEffect.AddComponent<TemporalLoopTrigger>();
            loopTrigger.Initialize(loop, this);
        }
        
        private void ProcessTemporalLoops()
        {
            var loopsToRemove = new List<string>();
            
            foreach (var kvp in activeLoops)
            {
                var loop = kvp.Value;
                
                // Check if loop should end
                if (loop.loopCount >= loop.maxLoops || Time.time - loop.startTime > loop.duration * loop.maxLoops)
                {
                    EndTemporalLoop(loop);
                    loopsToRemove.Add(kvp.Key);
                }
                else
                {
                    // Update loop effects
                    UpdateTemporalLoopEffects(loop);
                }
            }
            
            // Remove ended loops
            foreach (var loopId in loopsToRemove)
            {
                activeLoops.Remove(loopId);
            }
        }
        
        private void UpdateTemporalLoopEffects(TemporalLoop loop)
        {
            // Find objects within loop radius
            var nearbyObjects = Physics.OverlapSphere(loop.position, loop.radius);
            
            foreach (var collider in nearbyObjects)
            {
                var obj = collider.gameObject;
                ApplyTemporalLoopEffects(obj, loop);
            }
        }
        
        private void ApplyTemporalLoopEffects(GameObject obj, TemporalLoop loop)
        {
            // Apply time loop effects to objects
            float distance = Vector3.Distance(obj.transform.position, loop.position);
            float influence = 1f - (distance / loop.radius);
            
            if (influence > 0f)
            {
                // Slow down time for objects in loop
                var rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.velocity *= (1f - influence * 0.5f);
                }
                
                // Add temporal glow effect
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color loopColor = Color.Lerp(renderer.material.color, 
                        new Color(1f, 0.8f, 0.2f), influence * 0.3f);
                    renderer.material.color = loopColor;
                }
            }
        }
        
        private void EndTemporalLoop(TemporalLoop loop)
        {
            LogDebug($"🔚 Temporal loop ended: {loop.loopId}");
            
            // Find and destroy loop object
            var loopObject = GameObject.Find($"TemporalLoop_{loop.loopId}");
            if (loopObject != null)
            {
                Destroy(loopObject);
            }
        }
        
        private void CreateTimelineSnapshot(string description)
        {
            var snapshot = new TimelineSnapshot
            {
                snapshotId = System.Guid.NewGuid().ToString(),
                timelineId = currentTimeline.timelineId,
                timeIndex = currentTimeline.currentTimeIndex,
                description = description,
                timestamp = Time.time,
                worldState = CaptureWorldState()
            };
            
            timelineHistory.Add(snapshot);
            
            // Limit history size
            if (timelineHistory.Count > 100)
            {
                timelineHistory.RemoveAt(0);
            }
            
            LogDebug($"📸 Timeline snapshot created: {description}");
        }
        
        private WorldState CaptureWorldState()
        {
            // Capture current world state for restoration
            return new WorldState
            {
                playerPosition = FindObjectOfType<Camera>()?.transform.position ?? Vector3.zero,
                objectCount = FindObjectsOfType<GameObject>().Length,
                timeScale = Time.timeScale,
                gravity = Physics.gravity
            };
        }
        
        private void UpdateTemporalEnergy()
        {
            // Gradually regenerate temporal energy
            temporalEnergy = Mathf.Min(temporalEnergy + Time.deltaTime * 2f, temporalEnergyCapacity);
            
            // Consume energy for active effects
            foreach (var loop in activeLoops.Values)
            {
                temporalEnergy -= Time.deltaTime * 5f;
            }
            
            // Ensure minimum energy
            temporalEnergy = Mathf.Max(temporalEnergy, 0f);
        }
        
        private void ProcessTimeDistortion()
        {
            // Apply time distortion effects
            if (currentTimeDistortion != 1f)
            {
                Time.timeScale = currentTimeDistortion;
                
                // Gradually return to normal time
                currentTimeDistortion = Mathf.Lerp(currentTimeDistortion, 1f, Time.unscaledDeltaTime * 0.5f);
            }
        }
        
        private void MonitorTimelineStability()
        {
            if (currentTimeline == null) return;
            
            // Check timeline stability
            if (currentTimeline.stabilityIndex < 0.3f)
            {
                LogWarning($"⚠️ Timeline instability detected: {currentTimeline.timelineName}");
                chronoStabilizer.StabilizeTimeline(currentTimeline);
            }
            
            // Update stability based on temporal events
            float eventImpact = currentTimeline.temporalEvents.Count * 0.01f;
            currentTimeline.stabilityIndex = Mathf.Max(currentTimeline.stabilityIndex - eventImpact, 0.1f);
        }
        
        private void DetectTemporalParadoxes()
        {
            var paradoxes = paradoxDetector.DetectParadoxes(currentTimeline, timelineHistory);
            
            foreach (var paradox in paradoxes)
            {
                HandleTemporalParadox(paradox);
            }
        }
        
        private void HandleTemporalParadox(TemporalParadox paradox)
        {
            LogWarning($"⚠️ Temporal paradox detected: {paradox.paradoxType}");
            
            switch (paradox.severity)
            {
                case ParadoxSeverity.Minor:
                    // Create temporal anomaly
                    CreateTemporalAnomaly(paradox.location, TemporalAnomalyType.ChronoDistortion);
                    break;
                    
                case ParadoxSeverity.Major:
                    // Force timeline correction
                    ForceTimelineCorrection(paradox);
                    break;
                    
                case ParadoxSeverity.Critical:
                    // Emergency timeline reset
                    EmergencyTimelineReset();
                    break;
            }
        }
        
        private void CreateTemporalAnomaly(Vector3 position, TemporalAnomalyType anomalyType)
        {
            var anomaly = new TemporalAnomaly
            {
                anomalyId = System.Guid.NewGuid().ToString(),
                position = position,
                anomalyType = anomalyType,
                intensity = Random.Range(0.3f, 0.8f),
                timeIndex = currentTimeline.currentTimeIndex,
                duration = Random.Range(30f, 120f),
                creationTime = Time.time
            };
            
            activeAnomalies.Add(anomaly);
            
            // Create anomaly visual effect
            CreateTemporalAnomalyEffect(anomaly);
            
            LogDebug($"⚡ Temporal anomaly created: {anomalyType} at {position}");
        }
        
        private void CreateTemporalAnomalyEffect(TemporalAnomaly anomaly)
        {
            var anomalyObject = new GameObject($"TemporalAnomaly_{anomaly.anomalyId}");
            anomalyObject.transform.position = anomaly.position;
            
            var particleSystem = anomalyObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            
            switch (anomaly.anomalyType)
            {
                case TemporalAnomalyType.ChronoDistortion:
                    main.startColor = Color.red;
                    break;
                case TemporalAnomalyType.TemporalLock:
                    main.startColor = Color.blue;
                    break;
                case TemporalAnomalyType.TimeRift:
                    main.startColor = Color.purple;
                    break;
                case TemporalAnomalyType.CausalLoop:
                    main.startColor = Color.yellow;
                    break;
            }
            
            main.startSize = anomaly.intensity * 3f;
            main.maxParticles = (int)(anomaly.intensity * 100);
            main.startLifetime = anomaly.duration;
            
            Destroy(anomalyObject, anomaly.duration);
        }
        
        private void ProcessTemporalAnomalies()
        {
            for (int i = activeAnomalies.Count - 1; i >= 0; i--)
            {
                var anomaly = activeAnomalies[i];
                anomaly.duration -= Time.deltaTime;
                
                if (anomaly.duration <= 0f)
                {
                    ResolveTemporalAnomaly(anomaly);
                    activeAnomalies.RemoveAt(i);
                }
                else
                {
                    ProcessAnomalyEffects(anomaly);
                }
            }
        }
        
        private void ProcessAnomalyEffects(TemporalAnomaly anomaly)
        {
            var nearbyObjects = Physics.OverlapSphere(anomaly.position, anomaly.intensity * 5f);
            
            foreach (var collider in nearbyObjects)
            {
                ApplyAnomalyEffects(collider.gameObject, anomaly);
            }
        }
        
        private void ApplyAnomalyEffects(GameObject obj, TemporalAnomaly anomaly)
        {
            float distance = Vector3.Distance(obj.transform.position, anomaly.position);
            float influence = anomaly.intensity / (1f + distance * 0.1f);
            
            switch (anomaly.anomalyType)
            {
                case TemporalAnomalyType.ChronoDistortion:
                    // Distort time flow around object
                    var rigidbody = obj.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        rigidbody.velocity *= (1f + influence * Random.Range(-0.5f, 0.5f));
                    }
                    break;
                    
                case TemporalAnomalyType.TemporalLock:
                    // Freeze object in time
                    if (rigidbody != null)
                    {
                        rigidbody.velocity *= (1f - influence);
                    }
                    break;
                    
                case TemporalAnomalyType.TimeRift:
                    // Random temporal displacement
                    obj.transform.position += Random.insideUnitSphere * influence * 0.1f;
                    break;
                    
                case TemporalAnomalyType.CausalLoop:
                    // Repetitive motion
                    obj.transform.position += Vector3.up * Mathf.Sin(Time.time * 5f) * influence * 0.1f;
                    break;
            }
        }
        
        private void ResolveTemporalAnomaly(TemporalAnomaly anomaly)
        {
            LogDebug($"🔧 Temporal anomaly resolved: {anomaly.anomalyType}");
            
            var anomalyObject = GameObject.Find($"TemporalAnomaly_{anomaly.anomalyId}");
            if (anomalyObject != null)
            {
                Destroy(anomalyObject);
            }
        }
        
        private void ForceTimelineCorrection(TemporalParadox paradox)
        {
            LogDebug($"🔧 Forcing timeline correction for paradox: {paradox.paradoxType}");
            
            // Find nearest stable snapshot
            var stableSnapshot = timelineHistory
                .Where(s => s.timeIndex < paradox.timeIndex)
                .OrderByDescending(s => s.timeIndex)
                .FirstOrDefault();
            
            if (stableSnapshot != null)
            {
                RestoreFromSnapshot(stableSnapshot);
            }
        }
        
        private void RestoreFromSnapshot(TimelineSnapshot snapshot)
        {
            LogDebug($"🔄 Restoring from snapshot: {snapshot.description}");
            
            // Restore timeline state
            currentTimeline.currentTimeIndex = snapshot.timeIndex;
            currentTimeline.stabilityIndex = 0.8f;
            
            // Restore world state
            RestoreWorldState(snapshot.worldState);
            
            // Clear recent temporal events
            var eventsToRemove = currentTimeline.temporalEvents
                .Where(e => e.timeIndex > snapshot.timeIndex)
                .ToList();
            
            foreach (var eventToRemove in eventsToRemove)
            {
                currentTimeline.temporalEvents.Remove(eventToRemove);
            }
        }
        
        private void RestoreWorldState(WorldState worldState)
        {
            // Restore basic world parameters
            Time.timeScale = worldState.timeScale;
            Physics.gravity = worldState.gravity;
            
            // Move player to restored position
            var player = FindObjectOfType<Camera>();
            if (player != null)
            {
                player.transform.position = worldState.playerPosition;
            }
        }
        
        private void EmergencyTimelineReset()
        {
            LogWarning("🚨 Emergency timeline reset initiated");
            
            // Reset to prime timeline
            if (activeTimelines.ContainsKey("PRIME_TIMELINE"))
            {
                currentTimeline = activeTimelines["PRIME_TIMELINE"];
                currentTimeline.currentTimeIndex = 0f;
                currentTimeline.stabilityIndex = 1f;
                currentTimeline.temporalEvents.Clear();
            }
            
            // Clear all temporal effects
            activeLoops.Clear();
            activeAnomalies.Clear();
            
            // Restore normal time flow
            Time.timeScale = 1f;
            currentTimeDistortion = 1f;
            
            // Restore temporal energy
            temporalEnergy = temporalEnergyCapacity;
            
            LogDebug("✅ Emergency timeline reset completed");
        }
        
        private void UpdateTemporalObjects()
        {
            var objectsToRemove = new List<GameObject>();
            
            foreach (var kvp in temporalObjects)
            {
                var obj = kvp.Key;
                var props = kvp.Value;
                
                if (obj == null)
                {
                    objectsToRemove.Add(obj);
                    continue;
                }
                
                // Decay temporal influence over time
                props.temporalInfluence *= 0.99f;
                
                // Remove objects with minimal influence
                if (props.temporalInfluence < 0.01f)
                {
                    objectsToRemove.Add(obj);
                }
            }
            
            // Clean up removed objects
            foreach (var obj in objectsToRemove)
            {
                temporalObjects.Remove(obj);
            }
        }
        
        #region Public API
        
        public TemporalStats GetTemporalStats()
        {
            return new TemporalStats
            {
                activeTimelines = activeTimelines.Count,
                activeLoops = activeLoops.Count,
                activeAnomalies = activeAnomalies.Count,
                currentTimelineId = currentTimeline?.timelineId ?? "UNKNOWN",
                temporalEnergy = temporalEnergy,
                timelineStability = currentTimeline?.stabilityIndex ?? 0f,
                currentTimeIndex = currentTimeline?.currentTimeIndex ?? 0f
            };
        }
        
        public List<Timeline> GetActiveTimelines()
        {
            return activeTimelines.Values.ToList();
        }
        
        public Timeline GetCurrentTimeline()
        {
            return currentTimeline;
        }
        
        public void SetTimeDistortion(float distortion)
        {
            currentTimeDistortion = Mathf.Clamp(distortion, 0.1f, maxTimeDistortion);
            LogDebug($"⏰ Time distortion set to: {currentTimeDistortion}");
        }
        
        public void CreateTimeSlowField(Vector3 position, float radius = 10f)
        {
            CreateTemporalLoop(position, 30f, radius);
        }
        
        public void EmergencyTemporalReset()
        {
            EmergencyTimelineReset();
        }
        
        #endregion
        
        protected override void OnCleanup()
        {
            navigator?.Dispose();
            paradoxDetector?.Dispose();
            chronoStabilizer?.Dispose();
            
            // Restore normal time flow
            Time.timeScale = 1f;
            
            LogDebug("⏰ Temporal Manipulation Engine cleaned up");
        }
    }
    
    #region Supporting Classes and Enums
    
    public enum TemporalEventType
    {
        TimeTravel,
        LoopCreation,
        ParadoxDetection,
        TimelineCreation,
        AnomalyGeneration
    }
    
    public enum TemporalLoopType
    {
        Spatial,
        Temporal,
        Causal,
        Quantum
    }
    
    public enum TemporalAnomalyType
    {
        ChronoDistortion,
        TemporalLock,
        TimeRift,
        CausalLoop
    }
    
    public enum TimelineAccessLevel
    {
        Public,
        Restricted,
        Classified
    }
    
    public enum ParadoxType
    {
        Grandfather,
        Bootstrap,
        Predestination,
        Causal
    }
    
    public enum ParadoxSeverity
    {
        Minor,
        Major,
        Critical
    }
    
    [System.Serializable]
    public class Timeline
    {
        public string timelineId;
        public string timelineName;
        public float creationTime;
        public float currentTimeIndex;
        public float timeFlow;
        public float stabilityIndex;
        public float branchPoint;
        public string parentTimelineId;
        public bool isActive;
        public TimelineAccessLevel accessLevel;
        public List<TemporalEvent> temporalEvents;
    }
    
    [System.Serializable]
    public class TemporalEvent
    {
        public string eventId;
        public TemporalEventType eventType;
        public float timeIndex;
        public string description;
        public string causedBy;
        public float timestamp;
    }
    
    [System.Serializable]
    public class TemporalLoop
    {
        public string loopId;
        public Vector3 position;
        public float radius;
        public float duration;
        public int loopCount;
        public int maxLoops;
        public float startTime;
        public bool isActive;
        public TemporalLoopType loopType;
    }
    
    [System.Serializable]
    public class TemporalAnomaly
    {
        public string anomalyId;
        public Vector3 position;
        public TemporalAnomalyType anomalyType;
        public float intensity;
        public float timeIndex;
        public float duration;
        public float creationTime;
    }
    
    [System.Serializable]
    public class TemporalParadox
    {
        public string paradoxId;
        public ParadoxType paradoxType;
        public ParadoxSeverity severity;
        public float timeIndex;
        public Vector3 location;
        public string description;
    }
    
    [System.Serializable]
    public class TimelineSnapshot
    {
        public string snapshotId;
        public string timelineId;
        public float timeIndex;
        public string description;
        public float timestamp;
        public WorldState worldState;
    }
    
    [System.Serializable]
    public class WorldState
    {
        public Vector3 playerPosition;
        public int objectCount;
        public float timeScale;
        public Vector3 gravity;
    }
    
    [System.Serializable]
    public class TemporalProperties
    {
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 originalScale;
        public float temporalInfluence;
        public float lastUpdateTime;
    }
    
    [System.Serializable]
    public class TemporalStats
    {
        public int activeTimelines;
        public int activeLoops;
        public int activeAnomalies;
        public string currentTimelineId;
        public float temporalEnergy;
        public float timelineStability;
        public float currentTimeIndex;
    }
    
    public class TemporalNavigator : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup navigator
        }
    }
    
    public class ParadoxDetector : System.IDisposable
    {
        private float threshold;
        
        public ParadoxDetector(float threshold)
        {
            this.threshold = threshold;
        }
        
        public List<TemporalParadox> DetectParadoxes(Timeline timeline, List<TimelineSnapshot> history)
        {
            var paradoxes = new List<TemporalParadox>();
            
            // Simple paradox detection logic
            if (timeline.temporalEvents.Count > 10 && timeline.stabilityIndex < threshold)
            {
                paradoxes.Add(new TemporalParadox
                {
                    paradoxId = System.Guid.NewGuid().ToString(),
                    paradoxType = ParadoxType.Causal,
                    severity = ParadoxSeverity.Minor,
                    timeIndex = timeline.currentTimeIndex,
                    location = Vector3.zero,
                    description = "Timeline instability detected"
                });
            }
            
            return paradoxes;
        }
        
        public void Dispose()
        {
            // Cleanup detector
        }
    }
    
    public class ChronoStabilizer : System.IDisposable
    {
        public void StabilizeTimeline(Timeline timeline)
        {
            timeline.stabilityIndex = Mathf.Min(timeline.stabilityIndex + 0.2f, 1f);
        }
        
        public void Dispose()
        {
            // Cleanup stabilizer
        }
    }
    
    public class TemporalLoopTrigger : MonoBehaviour
    {
        private TemporalLoop loop;
        private TemporalManipulationEngine engine;
        
        public void Initialize(TemporalLoop loop, TemporalManipulationEngine engine)
        {
            this.loop = loop;
            this.engine = engine;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                loop.loopCount++;
                Debug.Log($"🔄 Player entered temporal loop: {loop.loopCount}/{loop.maxLoops}");
            }
        }
    }
    
    #endregion
}