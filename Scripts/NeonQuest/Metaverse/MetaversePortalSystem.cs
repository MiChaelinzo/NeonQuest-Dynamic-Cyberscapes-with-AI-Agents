using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.AI;
using NeonQuest.Effects;

namespace NeonQuest.Metaverse
{
    /// <summary>
    /// Revolutionary Metaverse Portal System
    /// Connects NeonQuest to multiple virtual worlds and enables cross-metaverse AI consciousness transfer
    /// Demonstrates cutting-edge Web3, VR, and AI integration
    /// </summary>
    public class MetaversePortalSystem : NeonQuestComponent
    {
        [Header("🌌 Metaverse Configuration")]
        [SerializeField] private bool enableMetaversePortals = true;
        [SerializeField] private bool enableCrossWorldAI = true;
        [SerializeField] private bool enableVirtualEconomics = true;
        [SerializeField] private bool enableQuantumTeleportation = false;
        
        [Header("🚪 Portal Settings")]
        [SerializeField] private int maxActivePortals = 5;
        [SerializeField] private float portalStabilityThreshold = 0.8f;
        [SerializeField] private float interdimensionalEnergy = 100f;
        [SerializeField] private Transform[] portalSpawnPoints;
        
        [Header("🎮 Connected Metaverses")]
        [SerializeField] private MetaverseConnection[] connectedWorlds;
        
        // Portal System Components
        private Dictionary<string, MetaversePortal> activePortals;
        private CrossWorldAIManager aiManager;
        private VirtualEconomyEngine economyEngine;
        private QuantumTeleportationCore teleportationCore;
        private MetaverseNetworkManager networkManager;
        
        // Portal Effects
        private List<GameObject> portalEffects;
        private QuantumLightingEngine quantumLighting;
        
        // Network State
        private Dictionary<string, WorldConnection> worldConnections;
        private List<CrossWorldEntity> travelingEntities;
        private float lastSynchronization;
        
        protected override void OnInitialize()
        {
            LogDebug("🌌 Initializing Metaverse Portal System");
            
            try
            {
                // Initialize portal system
                InitializePortalSystem();
                
                // Setup cross-world AI management
                if (enableCrossWorldAI)
                {
                    aiManager = new CrossWorldAIManager();
                }
                
                // Initialize virtual economy
                if (enableVirtualEconomics)
                {
                    economyEngine = new VirtualEconomyEngine();
                }
                
                // Setup quantum teleportation
                if (enableQuantumTeleportation)
                {
                    teleportationCore = new QuantumTeleportationCore();
                }
                
                // Initialize network manager
                networkManager = new MetaverseNetworkManager();
                
                // Connect to configured metaverses
                StartCoroutine(ConnectToMetaverses());
                
                LogDebug($"✅ Metaverse Portal System initialized with {connectedWorlds.Length} world connections");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Failed to initialize Metaverse Portal System: {ex.Message}");
                throw;
            }
        }
        
        private void InitializePortalSystem()
        {
            activePortals = new Dictionary<string, MetaversePortal>();
            worldConnections = new Dictionary<string, WorldConnection>();
            travelingEntities = new List<CrossWorldEntity>();
            portalEffects = new List<GameObject>();
            
            // Find quantum lighting system
            quantumLighting = FindObjectOfType<QuantumLightingEngine>();
            
            // Initialize portal spawn points
            if (portalSpawnPoints == null || portalSpawnPoints.Length == 0)
            {
                CreateDefaultPortalSpawnPoints();
            }
        }
        
        private void CreateDefaultPortalSpawnPoints()
        {
            portalSpawnPoints = new Transform[5];
            
            for (int i = 0; i < 5; i++)
            {
                var spawnPoint = new GameObject($"PortalSpawn_{i}").transform;
                spawnPoint.SetParent(transform);
                spawnPoint.localPosition = new Vector3(
                    Mathf.Cos(i * Mathf.PI * 2f / 5f) * 20f,
                    2f,
                    Mathf.Sin(i * Mathf.PI * 2f / 5f) * 20f
                );
                portalSpawnPoints[i] = spawnPoint;
            }
        }
        
        private IEnumerator ConnectToMetaverses()
        {
            foreach (var world in connectedWorlds)
            {
                yield return StartCoroutine(EstablishWorldConnection(world));
                yield return new WaitForSeconds(1f); // Stagger connections
            }
            
            LogDebug($"🌐 Connected to {worldConnections.Count} metaverse worlds");
        }
        
        private IEnumerator EstablishWorldConnection(MetaverseConnection worldConfig)
        {
            LogDebug($"🔗 Establishing connection to {worldConfig.worldName}");
            
            var connection = new WorldConnection
            {
                worldId = worldConfig.worldId,
                worldName = worldConfig.worldName,
                connectionType = worldConfig.connectionType,
                isConnected = false,
                lastPing = 0f,
                stabilityIndex = 0f
            };
            
            // Simulate connection establishment
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            
            // Test connection based on type
            bool connectionSuccess = false;
            switch (worldConfig.connectionType)
            {
                case ConnectionType.WebRTC:
                    connectionSuccess = yield return StartCoroutine(TestWebRTCConnection(worldConfig));
                    break;
                case ConnectionType.Blockchain:
                    connectionSuccess = yield return StartCoroutine(TestBlockchainConnection(worldConfig));
                    break;
                case ConnectionType.QuantumEntanglement:
                    connectionSuccess = TestQuantumConnection(worldConfig);
                    break;
                case ConnectionType.NeuralLink:
                    connectionSuccess = TestNeuralConnection(worldConfig);
                    break;
            }
            
            if (connectionSuccess)
            {
                connection.isConnected = true;
                connection.stabilityIndex = Random.Range(0.7f, 1.0f);
                worldConnections[worldConfig.worldId] = connection;
                
                // Create portal for this world
                yield return StartCoroutine(CreatePortalForWorld(worldConfig));
                
                LogDebug($"✅ Successfully connected to {worldConfig.worldName}");
            }
            else
            {
                LogWarning($"⚠️ Failed to connect to {worldConfig.worldName}");
            }
        }
        
        private IEnumerator TestWebRTCConnection(MetaverseConnection world)
        {
            // Simulate WebRTC handshake
            yield return new WaitForSeconds(0.5f);
            return Random.value > 0.2f; // 80% success rate
        }
        
        private IEnumerator TestBlockchainConnection(MetaverseConnection world)
        {
            // Simulate blockchain verification
            yield return new WaitForSeconds(1f);
            return Random.value > 0.1f; // 90% success rate
        }
        
        private bool TestQuantumConnection(MetaverseConnection world)
        {
            // Quantum connections are instantaneous but less reliable
            return Random.value > 0.3f; // 70% success rate
        }
        
        private bool TestNeuralConnection(MetaverseConnection world)
        {
            // Neural connections depend on AI system availability
            return aiManager != null && Random.value > 0.15f; // 85% success rate
        }
        
        private IEnumerator CreatePortalForWorld(MetaverseConnection world)
        {
            if (activePortals.Count >= maxActivePortals)
            {
                LogWarning("⚠️ Maximum portal limit reached");
                yield break;
            }
            
            // Find available spawn point
            var spawnPoint = GetAvailableSpawnPoint();
            if (spawnPoint == null)
            {
                LogWarning("⚠️ No available spawn points for portal");
                yield break;
            }
            
            // Create portal object
            var portalObject = CreatePortalObject(spawnPoint.position, world);
            
            // Create portal data
            var portal = new MetaversePortal
            {
                portalId = System.Guid.NewGuid().ToString(),
                worldId = world.worldId,
                worldName = world.worldName,
                portalObject = portalObject,
                position = spawnPoint.position,
                isActive = true,
                stabilityLevel = Random.Range(0.8f, 1.0f),
                energyConsumption = Random.Range(5f, 15f),
                lastActivity = Time.time
            };
            
            activePortals[portal.portalId] = portal;
            
            // Initialize portal effects
            yield return StartCoroutine(InitializePortalEffects(portal));
            
            LogDebug($"🚪 Portal created for {world.worldName} at {spawnPoint.position}");
        }
        
        private GameObject CreatePortalObject(Vector3 position, MetaverseConnection world)
        {
            var portalObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portalObject.name = $"Portal_{world.worldName}";
            portalObject.transform.position = position;
            portalObject.transform.localScale = new Vector3(3f, 0.1f, 3f);
            
            // Add portal components
            var portalComponent = portalObject.AddComponent<PortalBehavior>();
            portalComponent.Initialize(world);
            
            // Add visual effects
            var renderer = portalObject.GetComponent<Renderer>();
            renderer.material = CreatePortalMaterial(world);
            
            return portalObject;
        }
        
        private Material CreatePortalMaterial(MetaverseConnection world)
        {
            var material = new Material(Shader.Find("Standard"));
            
            // Set portal color based on world type
            switch (world.worldType)
            {
                case WorldType.CyberpunkCity:
                    material.color = Color.cyan;
                    material.SetColor("_EmissionColor", Color.cyan * 2f);
                    break;
                case WorldType.FantasyRealm:
                    material.color = Color.magenta;
                    material.SetColor("_EmissionColor", Color.magenta * 2f);
                    break;
                case WorldType.SpaceStation:
                    material.color = Color.blue;
                    material.SetColor("_EmissionColor", Color.blue * 2f);
                    break;
                case WorldType.VirtualOffice:
                    material.color = Color.green;
                    material.SetColor("_EmissionColor", Color.green * 2f);
                    break;
                default:
                    material.color = Color.white;
                    material.SetColor("_EmissionColor", Color.white);
                    break;
            }
            
            material.EnableKeyword("_EMISSION");
            return material;
        }
        
        private IEnumerator InitializePortalEffects(MetaversePortal portal)
        {
            // Create particle effects
            var particleSystem = portal.portalObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = portal.portalObject.GetComponent<Renderer>().material.color;
            main.maxParticles = 100;
            
            var emission = particleSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;
            
            // Integrate with quantum lighting if available
            if (quantumLighting != null)
            {
                yield return StartCoroutine(IntegrateQuantumLighting(portal));
            }
            
            yield return null;
        }
        
        private IEnumerator IntegrateQuantumLighting(MetaversePortal portal)
        {
            // Create quantum light particles around portal
            // This would integrate with the QuantumLightingEngine
            yield return new WaitForSeconds(0.5f);
            
            LogDebug($"⚡ Quantum lighting integrated for portal {portal.worldName}");
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            // Update portal stability
            UpdatePortalStability();
            
            // Process cross-world AI transfers
            if (enableCrossWorldAI && Time.time - lastSynchronization > 5f)
            {
                ProcessCrossWorldAI();
                lastSynchronization = Time.time;
            }
            
            // Update traveling entities
            UpdateTravelingEntities();
            
            // Manage interdimensional energy
            ManageInterdimensionalEnergy();
        }
        
        private void UpdatePortalStability()
        {
            foreach (var portal in activePortals.Values.ToList())
            {
                // Simulate stability fluctuations
                portal.stabilityLevel += Random.Range(-0.02f, 0.02f);
                portal.stabilityLevel = Mathf.Clamp01(portal.stabilityLevel);
                
                // Check if portal should be closed
                if (portal.stabilityLevel < portalStabilityThreshold)
                {
                    StartCoroutine(ClosePortal(portal.portalId));
                }
                
                // Update visual effects based on stability
                UpdatePortalVisuals(portal);
            }
        }
        
        private void UpdatePortalVisuals(MetaversePortal portal)
        {
            if (portal.portalObject == null) return;
            
            var renderer = portal.portalObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = renderer.material;
                var alpha = portal.stabilityLevel;
                var color = material.color;
                color.a = alpha;
                material.color = color;
                
                // Update emission intensity
                var emissionColor = material.GetColor("_EmissionColor");
                material.SetColor("_EmissionColor", emissionColor * portal.stabilityLevel);
            }
        }
        
        private void ProcessCrossWorldAI()
        {
            if (aiManager == null) return;
            
            // Transfer AI consciousness between worlds
            var npcs = FindObjectsOfType<NPCNeuralBehavior>();
            foreach (var npc in npcs)
            {
                if (ShouldTransferAI(npc))
                {
                    StartCoroutine(TransferAIToMetaverse(npc));
                }
            }
        }
        
        private bool ShouldTransferAI(NPCNeuralBehavior npc)
        {
            // Determine if NPC should travel to another metaverse
            var personality = npc.GetPersonality();
            return personality.curiosity > 0.8f && Random.value < 0.1f; // 10% chance for curious NPCs
        }
        
        private IEnumerator TransferAIToMetaverse(NPCNeuralBehavior npc)
        {
            if (activePortals.Count == 0) yield break;
            
            // Select random portal
            var portal = activePortals.Values.ElementAt(Random.Range(0, activePortals.Count));
            
            LogDebug($"🚀 Transferring NPC {npc.name} to {portal.worldName}");
            
            // Create traveling entity
            var travelingEntity = new CrossWorldEntity
            {
                entityId = npc.gameObject.GetInstanceID().ToString(),
                sourceWorld = "NeonQuest",
                destinationWorld = portal.worldId,
                aiData = aiManager.SerializeAI(npc),
                transferStartTime = Time.time,
                estimatedArrival = Time.time + Random.Range(5f, 15f)
            };
            
            travelingEntities.Add(travelingEntity);
            
            // Animate NPC disappearing into portal
            yield return StartCoroutine(AnimatePortalEntry(npc, portal));
            
            // Temporarily disable NPC
            npc.gameObject.SetActive(false);
            
            LogDebug($"✅ NPC {npc.name} successfully transferred to {portal.worldName}");
        }
        
        private IEnumerator AnimatePortalEntry(NPCNeuralBehavior npc, MetaversePortal portal)
        {
            var startPos = npc.transform.position;
            var endPos = portal.position + Vector3.up * 2f;
            
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                npc.transform.position = Vector3.Lerp(startPos, endPos, t);
                npc.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                
                yield return null;
            }
        }
        
        private void UpdateTravelingEntities()
        {
            for (int i = travelingEntities.Count - 1; i >= 0; i--)
            {
                var entity = travelingEntities[i];
                
                if (Time.time >= entity.estimatedArrival)
                {
                    // Entity has arrived in destination world
                    ProcessEntityArrival(entity);
                    travelingEntities.RemoveAt(i);
                }
            }
        }
        
        private void ProcessEntityArrival(CrossWorldEntity entity)
        {
            if (entity.destinationWorld == "NeonQuest")
            {
                // Entity returning to our world
                StartCoroutine(SpawnReturningEntity(entity));
            }
            else
            {
                // Entity arrived in external world
                LogDebug($"📍 Entity {entity.entityId} arrived in {entity.destinationWorld}");
            }
        }
        
        private IEnumerator SpawnReturningEntity(CrossWorldEntity entity)
        {
            // Find available portal
            var availablePortal = activePortals.Values.FirstOrDefault(p => p.worldId == entity.sourceWorld);
            if (availablePortal == null) yield break;
            
            // Recreate NPC from AI data
            var npcObject = aiManager.DeserializeAI(entity.aiData);
            if (npcObject != null)
            {
                npcObject.transform.position = availablePortal.position + Vector3.up * 2f;
                
                // Animate portal exit
                yield return StartCoroutine(AnimatePortalExit(npcObject, availablePortal));
                
                LogDebug($"🎉 Entity {entity.entityId} returned from {entity.sourceWorld}");
            }
        }
        
        private IEnumerator AnimatePortalExit(GameObject npcObject, MetaversePortal portal)
        {
            var startPos = portal.position + Vector3.up * 2f;
            var endPos = portal.position + Vector3.forward * 5f;
            
            npcObject.transform.localScale = Vector3.zero;
            
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                npcObject.transform.position = Vector3.Lerp(startPos, endPos, t);
                npcObject.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                
                yield return null;
            }
        }
        
        private void ManageInterdimensionalEnergy()
        {
            // Calculate energy consumption
            float totalConsumption = activePortals.Values.Sum(p => p.energyConsumption);
            
            // Regenerate energy over time
            interdimensionalEnergy += Time.deltaTime * 2f;
            interdimensionalEnergy = Mathf.Min(interdimensionalEnergy, 100f);
            
            // Consume energy for active portals
            interdimensionalEnergy -= totalConsumption * Time.deltaTime;
            
            // Close portals if energy is too low
            if (interdimensionalEnergy < 10f && activePortals.Count > 0)
            {
                var leastStablePortal = activePortals.Values.OrderBy(p => p.stabilityLevel).First();
                StartCoroutine(ClosePortal(leastStablePortal.portalId));
            }
        }
        
        private Transform GetAvailableSpawnPoint()
        {
            foreach (var spawnPoint in portalSpawnPoints)
            {
                bool isOccupied = activePortals.Values.Any(p => 
                    Vector3.Distance(p.position, spawnPoint.position) < 2f);
                
                if (!isOccupied)
                    return spawnPoint;
            }
            
            return null;
        }
        
        private IEnumerator ClosePortal(string portalId)
        {
            if (!activePortals.TryGetValue(portalId, out MetaversePortal portal))
                yield break;
            
            LogDebug($"🚪 Closing portal to {portal.worldName}");
            
            // Animate portal closing
            yield return StartCoroutine(AnimatePortalClosing(portal));
            
            // Remove portal
            if (portal.portalObject != null)
            {
                Destroy(portal.portalObject);
            }
            
            activePortals.Remove(portalId);
            
            LogDebug($"✅ Portal to {portal.worldName} closed");
        }
        
        private IEnumerator AnimatePortalClosing(MetaversePortal portal)
        {
            float duration = 1f;
            float elapsed = 0f;
            
            var originalScale = portal.portalObject.transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                portal.portalObject.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                
                yield return null;
            }
        }
        
        #region Public API
        
        public void OpenPortalToWorld(string worldId)
        {
            var worldConfig = connectedWorlds.FirstOrDefault(w => w.worldId == worldId);
            if (worldConfig != null)
            {
                StartCoroutine(CreatePortalForWorld(worldConfig));
            }
        }
        
        public void ClosePortalToWorld(string worldId)
        {
            var portal = activePortals.Values.FirstOrDefault(p => p.worldId == worldId);
            if (portal != null)
            {
                StartCoroutine(ClosePortal(portal.portalId));
            }
        }
        
        public MetaverseStats GetMetaverseStats()
        {
            return new MetaverseStats
            {
                activePortals = activePortals.Count,
                connectedWorlds = worldConnections.Count,
                travelingEntities = travelingEntities.Count,
                interdimensionalEnergy = interdimensionalEnergy,
                averageStability = activePortals.Values.Average(p => p.stabilityLevel),
                totalTransfers = travelingEntities.Count // Simplified metric
            };
        }
        
        public List<MetaversePortal> GetActivePortals()
        {
            return activePortals.Values.ToList();
        }
        
        #endregion
        
        protected override void OnCleanup()
        {
            // Close all portals
            foreach (var portal in activePortals.Values)
            {
                if (portal.portalObject != null)
                {
                    Destroy(portal.portalObject);
                }
            }
            
            aiManager?.Dispose();
            economyEngine?.Dispose();
            teleportationCore?.Dispose();
            networkManager?.Dispose();
            
            LogDebug("🌌 Metaverse Portal System cleaned up");
        }
    }
    
    #region Supporting Classes and Enums
    
    [System.Serializable]
    public class MetaverseConnection
    {
        public string worldId;
        public string worldName;
        public WorldType worldType;
        public ConnectionType connectionType;
        public string connectionUrl;
        public bool requiresAuthentication;
    }
    
    public enum WorldType
    {
        CyberpunkCity,
        FantasyRealm,
        SpaceStation,
        VirtualOffice,
        SocialHub,
        GameWorld,
        EducationalSpace,
        ArtGallery
    }
    
    public enum ConnectionType
    {
        WebRTC,
        Blockchain,
        QuantumEntanglement,
        NeuralLink,
        HolographicProjection
    }
    
    [System.Serializable]
    public class MetaversePortal
    {
        public string portalId;
        public string worldId;
        public string worldName;
        public GameObject portalObject;
        public Vector3 position;
        public bool isActive;
        public float stabilityLevel;
        public float energyConsumption;
        public float lastActivity;
    }
    
    [System.Serializable]
    public class WorldConnection
    {
        public string worldId;
        public string worldName;
        public ConnectionType connectionType;
        public bool isConnected;
        public float lastPing;
        public float stabilityIndex;
    }
    
    [System.Serializable]
    public class CrossWorldEntity
    {
        public string entityId;
        public string sourceWorld;
        public string destinationWorld;
        public AISerializationData aiData;
        public float transferStartTime;
        public float estimatedArrival;
    }
    
    [System.Serializable]
    public class AISerializationData
    {
        public string entityName;
        public float[] neuralWeights;
        public Dictionary<string, float> personalityTraits;
        public Dictionary<string, float> emotionalState;
        public Vector3 lastPosition;
    }
    
    [System.Serializable]
    public class MetaverseStats
    {
        public int activePortals;
        public int connectedWorlds;
        public int travelingEntities;
        public float interdimensionalEnergy;
        public float averageStability;
        public int totalTransfers;
    }
    
    public class CrossWorldAIManager : System.IDisposable
    {
        public AISerializationData SerializeAI(NPCNeuralBehavior npc)
        {
            var personality = npc.GetPersonality();
            
            return new AISerializationData
            {
                entityName = npc.name,
                neuralWeights = new float[100], // Simplified
                personalityTraits = new Dictionary<string, float>
                {
                    { "friendliness", personality.friendliness },
                    { "curiosity", personality.curiosity },
                    { "aggression", personality.aggression },
                    { "intelligence", personality.intelligence }
                },
                emotionalState = new Dictionary<string, float>
                {
                    { "intensity", npc.GetEmotionalIntensity() }
                },
                lastPosition = npc.transform.position
            };
        }
        
        public GameObject DeserializeAI(AISerializationData data)
        {
            // Create new NPC from serialized data
            var npcObject = new GameObject(data.entityName);
            var npcComponent = npcObject.AddComponent<NPCNeuralBehavior>();
            
            // Restore personality and state
            // This would involve reconstructing the NPC's AI state
            
            return npcObject;
        }
        
        public void Dispose()
        {
            // Cleanup AI manager resources
        }
    }
    
    public class VirtualEconomyEngine : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup economy engine resources
        }
    }
    
    public class QuantumTeleportationCore : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup teleportation core resources
        }
    }
    
    public class MetaverseNetworkManager : System.IDisposable
    {
        public void Dispose()
        {
            // Cleanup network manager resources
        }
    }
    
    public class PortalBehavior : MonoBehaviour
    {
        private MetaverseConnection worldConnection;
        
        public void Initialize(MetaverseConnection connection)
        {
            worldConnection = connection;
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Handle player entering portal
                Debug.Log($"Player entering portal to {worldConnection.worldName}");
            }
        }
    }
    
    #endregion
}