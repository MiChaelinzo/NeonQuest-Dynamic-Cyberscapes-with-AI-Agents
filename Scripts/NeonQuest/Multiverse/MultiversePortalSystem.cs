using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.Effects;

namespace NeonQuest.Multiverse
{
    /// <summary>
    /// Revolutionary Multiverse Portal System
    /// Enables travel between infinite parallel universes and dimensions
    /// Each universe has unique physics, rules, and reality parameters
    /// </summary>
    public class MultiversePortalSystem : NeonQuestComponent
    {
        [Header("🌌 Multiverse Configuration")]
        [SerializeField] private bool enableMultiverseTravel = true;
        [SerializeField] private bool enableParallelUniverses = true;
        [SerializeField] private bool enableDimensionalRifts = true;
        [SerializeField] private bool enableRealityMerging = false;
        
        [Header("🚪 Portal Parameters")]
        [SerializeField] private int maxActivePortals = 5;
        [SerializeField] private float portalStabilityThreshold = 0.8f;
        [SerializeField] private float dimensionalEnergyRequired = 100f;
        [SerializeField] private float universeGenerationComplexity = 0.7f;
        
        // Multiverse Components
        private Dictionary<string, Universe> discoveredUniverses;
        private Dictionary<string, DimensionalPortal> activePortals;
        private List<RealityRift> activeRifts;
        private MultiverseNavigator navigator;
        private UniverseGenerator universeGenerator;
        private DimensionalStabilizer stabilizer;
        
        // Current State
        private Universe currentUniverse;
        private float dimensionalEnergy;
        private Vector3 multiverseCenter;
        private Dictionary<GameObject, MultiverseProperties> affectedObjects;
        
        protected override void OnInitialize()
        {
            LogDebug("🌌 Initializing Multiverse Portal System");
            
            try
            {
                InitializeMultiverse();
                
                navigator = new MultiverseNavigator();
                universeGenerator = new UniverseGenerator(universeGenerationComplexity);
                stabilizer = new DimensionalStabilizer();
                
                // Create the base universe (current reality)
                CreateBaseUniverse();
                
                LogDebug("✅ Multiverse Portal System initialized");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Failed to initialize Multiverse Portal System: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeMultiverse()
        {
            discoveredUniverses = new Dictionary<string, Universe>();
            activePortals = new Dictionary<string, DimensionalPortal>();
            activeRifts = new List<RealityRift>();
            affectedObjects = new Dictionary<GameObject, MultiverseProperties>();
            
            dimensionalEnergy = dimensionalEnergyRequired;
            multiverseCenter = transform.position;
        }
        
        private void CreateBaseUniverse()
        {
            currentUniverse = new Universe
            {
                universeId = "BASE_REALITY",
                universeName = "Prime Universe",
                physicsLaws = new PhysicsLaws
                {
                    gravity = 9.81f,
                    timeFlow = 1.0f,
                    lightSpeed = 299792458f,
                    quantumUncertainty = 0.1f
                },
                realityParameters = new RealityParameters
                {
                    stabilityIndex = 1.0f,
                    entropyLevel = 0.2f,
                    dimensionalFrequency = 440f,
                    consciousnessResonance = 0.8f
                },
                environmentalConditions = new EnvironmentalConditions
                {
                    atmosphereComposition = "Standard Earth",
                    temperature = 20f,
                    pressure = 1013.25f,
                    radiationLevel = 0.1f
                },
                creationTime = Time.time,
                isStable = true,
                accessLevel = UniverseAccessLevel.Public
            };
            
            discoveredUniverses[currentUniverse.universeId] = currentUniverse;
            LogDebug($"🌍 Base universe created: {currentUniverse.universeName}");
        }
        
        void Update()
        {
            if (!isInitialized || !enableMultiverseTravel) return;
            
            // Update dimensional energy
            UpdateDimensionalEnergy();
            
            // Process active portals
            ProcessActivePortals();
            
            // Update reality rifts
            if (enableDimensionalRifts)
            {
                UpdateRealityRifts();
            }
            
            // Monitor universe stability
            MonitorUniverseStability();
            
            // Process multiverse effects
            ProcessMultiverseEffects();
        }
        
        public void CreateDimensionalPortal(Vector3 position, string targetUniverseId = null)
        {
            if (activePortals.Count >= maxActivePortals)
            {
                LogWarning("⚠️ Maximum portal limit reached");
                return;
            }
            
            if (dimensionalEnergy < dimensionalEnergyRequired)
            {
                LogWarning("⚠️ Insufficient dimensional energy for portal creation");
                return;
            }
            
            // Generate target universe if not specified
            if (string.IsNullOrEmpty(targetUniverseId))
            {
                targetUniverseId = GenerateRandomUniverse();
            }
            
            var portal = new DimensionalPortal
            {
                portalId = System.Guid.NewGuid().ToString(),
                position = position,
                sourceUniverseId = currentUniverse.universeId,
                targetUniverseId = targetUniverseId,
                stabilityLevel = Random.Range(0.6f, 1.0f),
                energyConsumption = Random.Range(10f, 30f),
                portalSize = Random.Range(2f, 5f),
                creationTime = Time.time,
                isActive = true,
                portalType = DimensionalPortalType.Bidirectional
            };
            
            activePortals[portal.portalId] = portal;
            
            // Consume dimensional energy
            dimensionalEnergy -= dimensionalEnergyRequired;
            
            // Create visual portal effect
            CreatePortalEffect(portal);
            
            LogDebug($"🚪 Dimensional portal created to universe: {targetUniverseId}");
        }
        
        private string GenerateRandomUniverse()
        {
            var universe = universeGenerator.GenerateUniverse();
            discoveredUniverses[universe.universeId] = universe;
            
            LogDebug($"🌟 New universe generated: {universe.universeName}");
            return universe.universeId;
        }
        
        private void CreatePortalEffect(DimensionalPortal portal)
        {
            var portalObject = new GameObject($"Portal_{portal.portalId}");
            portalObject.transform.position = portal.position;
            
            // Create swirling portal effect
            var particleSystem = portalObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startColor = new Color(0.5f, 0.8f, 1f, 0.8f);
            main.startSize = portal.portalSize;
            main.maxParticles = 200;
            main.startLifetime = 5f;
            main.startSpeed = 2f;
            
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = portal.portalSize * 0.5f;
            
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(-1f, 1f);
            
            // Add portal collider for interaction
            var collider = portalObject.AddComponent<SphereCollider>();
            collider.radius = portal.portalSize * 0.6f;
            collider.isTrigger = true;
            
            var portalTrigger = portalObject.AddComponent<PortalTrigger>();
            portalTrigger.Initialize(portal, this);
            
            LogDebug($"🌀 Portal effect created at {portal.position}");
        }
        
        public void TravelToUniverse(string targetUniverseId, GameObject traveler = null)
        {
            if (!discoveredUniverses.ContainsKey(targetUniverseId))
            {
                LogError($"❌ Universe not found: {targetUniverseId}");
                return;
            }
            
            var targetUniverse = discoveredUniverses[targetUniverseId];
            
            if (!CanTravelToUniverse(targetUniverse))
            {
                LogWarning($"⚠️ Cannot travel to universe: {targetUniverse.universeName}");
                return;
            }
            
            LogDebug($"🚀 Initiating travel to universe: {targetUniverse.universeName}");
            
            // Store current universe state
            StoreUniverseState(currentUniverse);
            
            // Apply transition effects
            ApplyUniverseTransition(currentUniverse, targetUniverse, traveler);
            
            // Switch to target universe
            currentUniverse = targetUniverse;
            
            // Apply new universe physics and rules
            ApplyUniversePhysics(targetUniverse);
            
            LogDebug($"✅ Successfully traveled to: {targetUniverse.universeName}");
        }
        
        private bool CanTravelToUniverse(Universe universe)
        {
            // Check access level
            if (universe.accessLevel == UniverseAccessLevel.Restricted)
            {
                return false;
            }
            
            // Check stability
            if (universe.realityParameters.stabilityIndex < portalStabilityThreshold)
            {
                return false;
            }
            
            // Check dimensional energy
            if (dimensionalEnergy < 50f)
            {
                return false;
            }
            
            return true;
        }
        
        private void StoreUniverseState(Universe universe)
        {
            // Store current physics state
            universe.physicsLaws.gravity = Physics.gravity.magnitude;
            universe.physicsLaws.timeFlow = Time.timeScale;
            
            // Store environmental conditions
            universe.environmentalConditions.temperature = Random.Range(15f, 25f); // Simulated
            
            LogDebug($"💾 Universe state stored: {universe.universeName}");
        }
        
        private void ApplyUniverseTransition(Universe from, Universe to, GameObject traveler)
        {
            // Create transition effect
            var transitionEffect = new GameObject("UniverseTransition");
            transitionEffect.transform.position = traveler?.transform.position ?? transform.position;
            
            var particleSystem = transitionEffect.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startColor = Color.white;
            main.startSize = 5f;
            main.maxParticles = 500;
            main.startLifetime = 2f;
            
            var emission = particleSystem.emission;
            emission.rateOverTime = 250f;
            
            Destroy(transitionEffect, 3f);
            
            LogDebug($"✨ Universe transition effect applied");
        }
        
        private void ApplyUniversePhysics(Universe universe)
        {
            // Apply gravity
            Physics.gravity = Vector3.down * universe.physicsLaws.gravity;
            
            // Apply time flow
            Time.timeScale = universe.physicsLaws.timeFlow;
            
            // Apply environmental effects to all objects
            ApplyEnvironmentalEffects(universe);
            
            LogDebug($"⚙️ Physics applied for universe: {universe.universeName}");
        }
        
        private void ApplyEnvironmentalEffects(Universe universe)
        {
            var allObjects = FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                if (!affectedObjects.ContainsKey(obj))
                {
                    affectedObjects[obj] = new MultiverseProperties
                    {
                        originalScale = obj.transform.localScale,
                        originalColor = GetObjectColor(obj),
                        universalInfluence = 0f
                    };
                }
                
                var props = affectedObjects[obj];
                
                // Apply universe-specific transformations
                ApplyUniverseTransformations(obj, props, universe);
            }
        }
        
        private Color GetObjectColor(GameObject obj)
        {
            var renderer = obj.GetComponent<Renderer>();
            return renderer != null ? renderer.material.color : Color.white;
        }
        
        private void ApplyUniverseTransformations(GameObject obj, MultiverseProperties props, Universe universe)
        {
            // Scale based on dimensional frequency
            float scaleModifier = 1f + Mathf.Sin(universe.realityParameters.dimensionalFrequency * 0.01f) * 0.1f;
            obj.transform.localScale = Vector3.Lerp(obj.transform.localScale, props.originalScale * scaleModifier, Time.deltaTime);
            
            // Color based on consciousness resonance
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color newColor = Color.Lerp(props.originalColor, 
                    new Color(universe.realityParameters.consciousnessResonance, 0.5f, 1f - universe.realityParameters.consciousnessResonance), 
                    0.3f);
                renderer.material.color = newColor;
            }
            
            // Apply entropy effects
            if (universe.realityParameters.entropyLevel > 0.5f)
            {
                // High entropy - add randomness
                Vector3 randomOffset = Random.insideUnitSphere * universe.realityParameters.entropyLevel * 0.1f;
                obj.transform.position += randomOffset * Time.deltaTime;
            }
        }
        
        private void UpdateDimensionalEnergy()
        {
            // Gradually regenerate dimensional energy
            dimensionalEnergy = Mathf.Min(dimensionalEnergy + Time.deltaTime * 5f, dimensionalEnergyRequired * 2f);
            
            // Consume energy for active portals
            foreach (var portal in activePortals.Values)
            {
                dimensionalEnergy -= portal.energyConsumption * Time.deltaTime;
            }
            
            // Ensure minimum energy
            dimensionalEnergy = Mathf.Max(dimensionalEnergy, 0f);
        }
        
        private void ProcessActivePortals()
        {
            var portalsToRemove = new List<string>();
            
            foreach (var kvp in activePortals)
            {
                var portal = kvp.Value;
                
                // Update portal stability
                portal.stabilityLevel -= Time.deltaTime * 0.02f;
                
                // Check if portal should collapse
                if (portal.stabilityLevel <= 0f || dimensionalEnergy <= 0f)
                {
                    CollapsePortal(portal);
                    portalsToRemove.Add(kvp.Key);
                }
                else
                {
                    // Update portal effects
                    UpdatePortalEffects(portal);
                }
            }
            
            // Remove collapsed portals
            foreach (var portalId in portalsToRemove)
            {
                activePortals.Remove(portalId);
            }
        }
        
        private void CollapsePortal(DimensionalPortal portal)
        {
            LogDebug($"💥 Portal collapsed: {portal.portalId}");
            
            // Create collapse effect
            var collapseEffect = new GameObject("PortalCollapse");
            collapseEffect.transform.position = portal.position;
            
            var particleSystem = collapseEffect.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startColor = Color.red;
            main.startSize = portal.portalSize * 1.5f;
            main.maxParticles = 100;
            main.startLifetime = 1f;
            
            Destroy(collapseEffect, 2f);
            
            // Find and destroy portal object
            var portalObject = GameObject.Find($"Portal_{portal.portalId}");
            if (portalObject != null)
            {
                Destroy(portalObject);
            }
        }
        
        private void UpdatePortalEffects(DimensionalPortal portal)
        {
            // Update portal visual effects based on stability
            var portalObject = GameObject.Find($"Portal_{portal.portalId}");
            if (portalObject != null)
            {
                var particleSystem = portalObject.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.startColor = Color.Lerp(Color.red, Color.cyan, portal.stabilityLevel);
                }
            }
        }
        
        private void UpdateRealityRifts()
        {
            // Randomly create reality rifts
            if (Random.value < 0.001f && activeRifts.Count < 3)
            {
                CreateRealityRift();
            }
            
            // Update existing rifts
            for (int i = activeRifts.Count - 1; i >= 0; i--)
            {
                var rift = activeRifts[i];
                rift.duration -= Time.deltaTime;
                
                if (rift.duration <= 0f)
                {
                    CloseRealityRift(rift);
                    activeRifts.RemoveAt(i);
                }
                else
                {
                    ProcessRiftEffects(rift);
                }
            }
        }
        
        private void CreateRealityRift()
        {
            var rift = new RealityRift
            {
                riftId = System.Guid.NewGuid().ToString(),
                position = transform.position + Random.insideUnitSphere * 20f,
                size = Random.Range(1f, 3f),
                intensity = Random.Range(0.3f, 0.8f),
                duration = Random.Range(10f, 30f),
                riftType = (RealityRiftType)Random.Range(0, System.Enum.GetValues(typeof(RealityRiftType)).Length),
                creationTime = Time.time
            };
            
            activeRifts.Add(rift);
            
            // Create rift visual effect
            CreateRiftEffect(rift);
            
            LogDebug($"⚡ Reality rift created: {rift.riftType} at {rift.position}");
        }
        
        private void CreateRiftEffect(RealityRift rift)
        {
            var riftObject = new GameObject($"RealityRift_{rift.riftId}");
            riftObject.transform.position = rift.position;
            
            var particleSystem = riftObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            
            switch (rift.riftType)
            {
                case RealityRiftType.Temporal:
                    main.startColor = Color.yellow;
                    break;
                case RealityRiftType.Spatial:
                    main.startColor = Color.blue;
                    break;
                case RealityRiftType.Dimensional:
                    main.startColor = Color.purple;
                    break;
                case RealityRiftType.Quantum:
                    main.startColor = Color.green;
                    break;
            }
            
            main.startSize = rift.size;
            main.maxParticles = (int)(rift.intensity * 100);
            main.startLifetime = rift.duration;
            
            Destroy(riftObject, rift.duration);
        }
        
        private void ProcessRiftEffects(RealityRift rift)
        {
            var nearbyObjects = Physics.OverlapSphere(rift.position, rift.size * 2f);
            
            foreach (var collider in nearbyObjects)
            {
                var obj = collider.gameObject;
                ApplyRiftEffects(obj, rift);
            }
        }
        
        private void ApplyRiftEffects(GameObject obj, RealityRift rift)
        {
            float distance = Vector3.Distance(obj.transform.position, rift.position);
            float influence = rift.intensity / (1f + distance * 0.1f);
            
            switch (rift.riftType)
            {
                case RealityRiftType.Temporal:
                    // Time dilation effects
                    var rigidbody = obj.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        rigidbody.velocity *= (1f - influence * 0.5f);
                    }
                    break;
                    
                case RealityRiftType.Spatial:
                    // Space warping effects
                    Vector3 direction = (obj.transform.position - rift.position).normalized;
                    obj.transform.position += direction * influence * Time.deltaTime;
                    break;
                    
                case RealityRiftType.Dimensional:
                    // Dimensional phasing
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var color = renderer.material.color;
                        color.a = 1f - influence * 0.5f;
                        renderer.material.color = color;
                    }
                    break;
                    
                case RealityRiftType.Quantum:
                    // Quantum uncertainty
                    obj.transform.position += Random.insideUnitSphere * influence * 0.1f;
                    break;
            }
        }
        
        private void CloseRealityRift(RealityRift rift)
        {
            LogDebug($"🔒 Reality rift closed: {rift.riftType}");
            
            var riftObject = GameObject.Find($"RealityRift_{rift.riftId}");
            if (riftObject != null)
            {
                Destroy(riftObject);
            }
        }
        
        private void MonitorUniverseStability()
        {
            if (currentUniverse == null) return;
            
            // Check for instabilities
            if (currentUniverse.realityParameters.stabilityIndex < 0.3f)
            {
                LogWarning($"⚠️ Universe instability detected: {currentUniverse.universeName}");
                stabilizer.StabilizeUniverse(currentUniverse);
            }
            
            // Update entropy
            currentUniverse.realityParameters.entropyLevel += Time.deltaTime * 0.001f;
            currentUniverse.realityParameters.entropyLevel = Mathf.Clamp01(currentUniverse.realityParameters.entropyLevel);
        }
        
        private void ProcessMultiverseEffects()
        {
            // Apply multiverse-wide effects
            foreach (var universe in discoveredUniverses.Values)
            {
                if (universe != currentUniverse)
                {
                    // Simulate universe evolution
                    universe.realityParameters.entropyLevel += Time.deltaTime * 0.0001f;
                    universe.realityParameters.stabilityIndex -= Time.deltaTime * 0.00001f;
                    
                    // Clamp values
                    universe.realityParameters.entropyLevel = Mathf.Clamp01(universe.realityParameters.entropyLevel);
                    universe.realityParameters.stabilityIndex = Mathf.Clamp01(universe.realityParameters.stabilityIndex);
                }
            }
        }
        
        #region Public API
        
        public MultiverseStats GetMultiverseStats()
        {
            return new MultiverseStats
            {
                discoveredUniverses = discoveredUniverses.Count,
                activePortals = activePortals.Count,
                activeRifts = activeRifts.Count,
                currentUniverseId = currentUniverse?.universeId ?? "UNKNOWN",
                dimensionalEnergy = dimensionalEnergy,
                averageUniverseStability = discoveredUniverses.Values.Count > 0 ? 
                    discoveredUniverses.Values.Average(u => u.realityParameters.stabilityIndex) : 0f
            };
        }
        
        public List<Universe> GetDiscoveredUniverses()
        {
            return discoveredUniverses.Values.ToList();
        }
        
        public Universe GetCurrentUniverse()
        {
            return currentUniverse;
        }
        
        public void CreatePortalToRandomUniverse(Vector3 position)
        {
            CreateDimensionalPortal(position);
        }
        
        public void EmergencyReturnToBaseReality()
        {
            if (discoveredUniverses.ContainsKey("BASE_REALITY"))
            {
                TravelToUniverse("BASE_REALITY");
                LogDebug("🏠 Emergency return to base reality completed");
            }
        }
        
        #endregion
        
        protected override void OnCleanup()
        {
            navigator?.Dispose();
            universeGenerator?.Dispose();
            stabilizer?.Dispose();
            
            // Restore base physics
            Physics.gravity = Vector3.down * 9.81f;
            Time.timeScale = 1f;
            
            LogDebug("🌌 Multiverse Portal System cleaned up");
        }
    }
    
    #region Supporting Classes and Enums
    
    public enum DimensionalPortalType
    {
        Unidirectional,
        Bidirectional,
        Temporal,
        Quantum
    }
    
    public enum UniverseAccessLevel
    {
        Public,
        Restricted,
        Classified,
        Forbidden
    }
    
    public enum RealityRiftType
    {
        Temporal,
        Spatial,
        Dimensional,
        Quantum
    }
    
    [System.Serializable]
    public class Universe
    {
        public string universeId;
        public string universeName;
        public PhysicsLaws physicsLaws;
        public RealityParameters realityParameters;
        public EnvironmentalConditions environmentalConditions;
        public float creationTime;
        public bool isStable;
        public UniverseAccessLevel accessLevel;
    }
    
    [System.Serializable]
    public class PhysicsLaws
    {
        public float gravity;
        public float timeFlow;
        public float lightSpeed;
        public float quantumUncertainty;
    }
    
    [System.Serializable]
    public class RealityParameters
    {
        public float stabilityIndex;
        public float entropyLevel;
        public float dimensionalFrequency;
        public float consciousnessResonance;
    }
    
    [System.Serializable]
    public class EnvironmentalConditions
    {
        public string atmosphereComposition;
        public float temperature;
        public float pressure;
        public float radiationLevel;
    }
    
    [System.Serializable]
    public class DimensionalPortal
    {
        public string portalId;
        public Vector3 position;
        public string sourceUniverseId;
        public string targetUniverseId;
        public float stabilityLevel;
        public float energyConsumption;
        public float portalSize;
        public float creationTime;
        public bool isActive;
        public DimensionalPortalType portalType;
    }
    
    [System.Serializable]
    public class RealityRift
    {
        public string riftId;
        public Vector3 position;
        public float size;
        public float intensity;
        public float duration;
        public RealityRiftType riftType;
        public float creationTime;
    }
    
    [System.Serializable]
    public class MultiverseProperties
    {
        public Vector3 originalScale;
        public Color originalColor;
        public float universalInfluence;
    }
    
    [System.Serializable]
    public class MultiverseStats
    {
        public int discoveredUniverses;
        public int activePortals;
        public int activeRifts;
        public string currentUniverseId;
        public float dimensionalEnergy;
        public float averageUniverseStability;
    }
    
    public class MultiverseNavigator : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup navigator
        }
    }
    
    public class UniverseGenerator : System.IDisposable
    {
        private float complexity;
        
        public UniverseGenerator(float complexity)
        {
            this.complexity = complexity;
        }
        
        public Universe GenerateUniverse()
        {
            var universeId = System.Guid.NewGuid().ToString();
            var universeName = GenerateUniverseName();
            
            return new Universe
            {
                universeId = universeId,
                universeName = universeName,
                physicsLaws = GeneratePhysicsLaws(),
                realityParameters = GenerateRealityParameters(),
                environmentalConditions = GenerateEnvironmentalConditions(),
                creationTime = Time.time,
                isStable = Random.value > 0.2f,
                accessLevel = (UniverseAccessLevel)Random.Range(0, System.Enum.GetValues(typeof(UniverseAccessLevel)).Length)
            };
        }
        
        private string GenerateUniverseName()
        {
            string[] prefixes = { "Neo", "Quantum", "Parallel", "Mirror", "Shadow", "Crystal", "Void", "Infinite" };
            string[] suffixes = { "Realm", "Dimension", "Universe", "Reality", "Cosmos", "Plane", "Sphere", "Domain" };
            
            return $"{prefixes[Random.Range(0, prefixes.Length)]} {suffixes[Random.Range(0, suffixes.Length)]}";
        }
        
        private PhysicsLaws GeneratePhysicsLaws()
        {
            return new PhysicsLaws
            {
                gravity = Random.Range(1f, 20f),
                timeFlow = Random.Range(0.1f, 3f),
                lightSpeed = Random.Range(100000000f, 500000000f),
                quantumUncertainty = Random.Range(0.01f, 0.5f)
            };
        }
        
        private RealityParameters GenerateRealityParameters()
        {
            return new RealityParameters
            {
                stabilityIndex = Random.Range(0.3f, 1f),
                entropyLevel = Random.Range(0f, 0.8f),
                dimensionalFrequency = Random.Range(100f, 1000f),
                consciousnessResonance = Random.Range(0.1f, 1f)
            };
        }
        
        private EnvironmentalConditions GenerateEnvironmentalConditions()
        {
            string[] atmospheres = { "Oxygen-Rich", "Methane-Based", "Crystalline", "Plasma", "Void", "Quantum Foam" };
            
            return new EnvironmentalConditions
            {
                atmosphereComposition = atmospheres[Random.Range(0, atmospheres.Length)],
                temperature = Random.Range(-100f, 200f),
                pressure = Random.Range(0.1f, 10f),
                radiationLevel = Random.Range(0f, 2f)
            };
        }
        
        public void Dispose()
        {
            // Cleanup generator
        }
    }
    
    public class DimensionalStabilizer : System.IDisposable
    {
        public void StabilizeUniverse(Universe universe)
        {
            universe.realityParameters.stabilityIndex = Mathf.Min(universe.realityParameters.stabilityIndex + 0.1f, 1f);
            universe.realityParameters.entropyLevel = Mathf.Max(universe.realityParameters.entropyLevel - 0.05f, 0f);
        }
        
        public void Dispose()
        {
            // Cleanup stabilizer
        }
    }
    
    public class PortalTrigger : MonoBehaviour
    {
        private DimensionalPortal portal;
        private MultiversePortalSystem portalSystem;
        
        public void Initialize(DimensionalPortal portal, MultiversePortalSystem system)
        {
            this.portal = portal;
            this.portalSystem = system;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                portalSystem.TravelToUniverse(portal.targetUniverseId, other.gameObject);
            }
        }
    }
    
    #endregion
}