using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;

namespace NeonQuest.AI
{
    /// <summary>
    /// Advanced swarm intelligence system for collective NPC behavior
    /// Implements boids algorithm with enhanced AI decision-making
    /// </summary>
    public class SwarmIntelligenceCore : System.IDisposable
    {
        private Dictionary<int, SwarmGroup> swarmGroups;
        private List<SwarmInformation> sharedInformation;
        private SwarmCommunicationNetwork communicationNetwork;
        private int nextGroupId = 0;
        
        public SwarmIntelligenceCore()
        {
            InitializeSwarmSystem();
        }
        
        private void InitializeSwarmSystem()
        {
            swarmGroups = new Dictionary<int, SwarmGroup>();
            sharedInformation = new List<SwarmInformation>();
            communicationNetwork = new SwarmCommunicationNetwork();
        }
        
        public SwarmGroup CreateSwarmGroup(Vector3 center, float radius)
        {
            var group = new SwarmGroup
            {
                id = nextGroupId++,
                center = center,
                radius = radius,
                members = new List<NPCNeuralBehavior>(),
                dominantBehavior = NPCState.Patrolling,
                cohesionStrength = 0.5f,
                separationStrength = 0.3f,
                alignmentStrength = 0.4f,
                leadershipHierarchy = new List<int>()
            };
            
            swarmGroups[group.id] = group;
            return group;
        }
        
        public void AddToSwarm(NPCNeuralBehavior npc, int groupId)
        {
            if (swarmGroups.TryGetValue(groupId, out SwarmGroup group))
            {
                group.members.Add(npc);
                UpdateSwarmDynamics(group);
            }
        }
        
        public void RemoveFromSwarm(NPCNeuralBehavior npc, int groupId)
        {
            if (swarmGroups.TryGetValue(groupId, out SwarmGroup group))
            {
                group.members.Remove(npc);
                UpdateSwarmDynamics(group);
            }
        }
        
        private void UpdateSwarmDynamics(SwarmGroup group)
        {
            if (group.members.Count == 0) return;
            
            // Update swarm center
            Vector3 centerOfMass = Vector3.zero;
            foreach (var member in group.members)
            {
                centerOfMass += member.transform.position;
            }
            group.center = centerOfMass / group.members.Count;
            
            // Update dominant behavior
            group.dominantBehavior = CalculateDominantBehavior(group);
            
            // Update leadership hierarchy
            UpdateLeadershipHierarchy(group);
            
            // Calculate swarm forces
            CalculateSwarmForces(group);
        }
        
        private NPCState CalculateDominantBehavior(SwarmGroup group)
        {
            var behaviorCounts = new Dictionary<NPCState, int>();
            
            foreach (var member in group.members)
            {
                var state = member.GetCurrentState();
                behaviorCounts[state] = behaviorCounts.GetValueOrDefault(state, 0) + 1;
            }
            
            return behaviorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        
        private void UpdateLeadershipHierarchy(SwarmGroup group)
        {
            // Sort members by leadership qualities
            var sortedMembers = group.members
                .Select((member, index) => new { member, index })
                .OrderByDescending(x => CalculateLeadershipScore(x.member))
                .ToList();
            
            group.leadershipHierarchy.Clear();
            foreach (var item in sortedMembers)
            {
                group.leadershipHierarchy.Add(item.index);
            }
        }
        
        private float CalculateLeadershipScore(NPCNeuralBehavior npc)
        {
            var personality = npc.GetPersonality();
            return personality.confidence * 0.4f + 
                   personality.intelligence * 0.3f + 
                   personality.independence * 0.2f + 
                   personality.socialness * 0.1f;
        }
        
        private void CalculateSwarmForces(SwarmGroup group)
        {
            foreach (var member in group.members)
            {
                var forces = CalculateBoidsForces(member, group);
                ApplySwarmForces(member, forces);
            }
        }
        
        private SwarmForces CalculateBoidsForces(NPCNeuralBehavior npc, SwarmGroup group)
        {
            var forces = new SwarmForces();
            var npcPosition = npc.transform.position;
            var neighbors = GetNeighbors(npc, group, 5f); // 5 unit neighbor radius
            
            if (neighbors.Count == 0) return forces;
            
            // Cohesion: steer towards average position of neighbors
            Vector3 cohesionTarget = Vector3.zero;
            foreach (var neighbor in neighbors)
            {
                cohesionTarget += neighbor.transform.position;
            }
            cohesionTarget /= neighbors.Count;
            forces.cohesion = (cohesionTarget - npcPosition).normalized * group.cohesionStrength;
            
            // Separation: steer away from nearby neighbors
            Vector3 separationForce = Vector3.zero;
            foreach (var neighbor in neighbors)
            {
                Vector3 diff = npcPosition - neighbor.transform.position;
                float distance = diff.magnitude;
                if (distance > 0f && distance < 2f) // Separation radius
                {
                    separationForce += diff.normalized / distance;
                }
            }
            forces.separation = separationForce.normalized * group.separationStrength;
            
            // Alignment: steer towards average heading of neighbors
            Vector3 averageVelocity = Vector3.zero;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.navAgent != null)
                    averageVelocity += neighbor.navAgent.velocity;
            }
            if (neighbors.Count > 0)
            {
                averageVelocity /= neighbors.Count;
                forces.alignment = averageVelocity.normalized * group.alignmentStrength;
            }
            
            // Leadership influence
            var leader = GetSwarmLeader(group);
            if (leader != null && leader != npc)
            {
                Vector3 leaderDirection = (leader.transform.position - npcPosition).normalized;
                forces.leadership = leaderDirection * 0.3f;
            }
            
            return forces;
        }
        
        private List<NPCNeuralBehavior> GetNeighbors(NPCNeuralBehavior npc, SwarmGroup group, float radius)
        {
            var neighbors = new List<NPCNeuralBehavior>();
            var npcPosition = npc.transform.position;
            
            foreach (var member in group.members)
            {
                if (member != npc)
                {
                    float distance = Vector3.Distance(npcPosition, member.transform.position);
                    if (distance <= radius)
                    {
                        neighbors.Add(member);
                    }
                }
            }
            
            return neighbors;
        }
        
        private NPCNeuralBehavior GetSwarmLeader(SwarmGroup group)
        {
            if (group.leadershipHierarchy.Count > 0 && group.leadershipHierarchy[0] < group.members.Count)
            {
                return group.members[group.leadershipHierarchy[0]];
            }
            return null;
        }
        
        private void ApplySwarmForces(NPCNeuralBehavior npc, SwarmForces forces)
        {
            if (npc.navAgent == null) return;
            
            // Combine all forces
            Vector3 totalForce = forces.cohesion + forces.separation + forces.alignment + forces.leadership;
            
            // Apply personality modifiers
            var personality = npc.GetPersonality();
            totalForce *= personality.socialness; // Social NPCs follow swarm more
            
            // Apply force as destination offset
            if (totalForce.magnitude > 0.1f)
            {
                Vector3 targetPosition = npc.transform.position + totalForce.normalized * 3f;
                targetPosition.y = npc.transform.position.y; // Keep on ground
                
                // Only apply if NPC is in appropriate state
                var currentState = npc.GetCurrentState();
                if (currentState == NPCState.Patrolling || currentState == NPCState.Idle || currentState == NPCState.SwarmBehavior)
                {
                    npc.navAgent.SetDestination(targetPosition);
                }
            }
        }
        
        public void BroadcastInformation(SwarmInformation info)
        {
            sharedInformation.Add(info);
            communicationNetwork.PropagateInformation(info, swarmGroups.Values);
            
            // Maintain information limit
            if (sharedInformation.Count > 50)
            {
                sharedInformation.RemoveAt(0);
            }
        }
        
        public List<SwarmInformation> GetRecentInformation(int count = 10)
        {
            return sharedInformation.TakeLast(count).ToList();
        }
        
        public SwarmGroup GetSwarmGroup(int groupId)
        {
            return swarmGroups.GetValueOrDefault(groupId);
        }
        
        public List<SwarmGroup> GetAllSwarmGroups()
        {
            return swarmGroups.Values.ToList();
        }
        
        public void Dispose()
        {
            swarmGroups?.Clear();
            sharedInformation?.Clear();
            communicationNetwork?.Dispose();
        }
    }
    
    [System.Serializable]
    public class SwarmGroup
    {
        public int id;
        public Vector3 center;
        public float radius;
        public List<NPCNeuralBehavior> members;
        public NPCState dominantBehavior;
        public float cohesionStrength;
        public float separationStrength;
        public float alignmentStrength;
        public List<int> leadershipHierarchy;
        public float groupMorale = 0.5f;
        public float groupIntelligence = 0.5f;
    }
    
    [System.Serializable]
    public class SwarmForces
    {
        public Vector3 cohesion;
        public Vector3 separation;
        public Vector3 alignment;
        public Vector3 leadership;
    }
    
    [System.Serializable]
    public class SwarmInformation
    {
        public NPCNeuralBehavior sourceNPC;
        public EmotionalState emotionalState;
        public Dictionary<string, float> behaviorWeights;
        public float playerInteractionQuality;
        public float timestamp;
        public Vector3 location;
        public string informationType;
        public float reliability = 1.0f;
    }
    
    public class SwarmCommunicationNetwork : System.IDisposable
    {
        private Dictionary<int, List<SwarmMessage>> messageQueues;
        
        public SwarmCommunicationNetwork()
        {
            messageQueues = new Dictionary<int, List<SwarmMessage>>();
        }
        
        public void PropagateInformation(SwarmInformation info, IEnumerable<SwarmGroup> groups)
        {
            var message = new SwarmMessage
            {
                information = info,
                timestamp = Time.time,
                propagationRadius = 10f,
                priority = CalculateMessagePriority(info)
            };
            
            foreach (var group in groups)
            {
                if (!messageQueues.ContainsKey(group.id))
                    messageQueues[group.id] = new List<SwarmMessage>();
                
                messageQueues[group.id].Add(message);
                
                // Maintain queue size
                if (messageQueues[group.id].Count > 20)
                {
                    messageQueues[group.id].RemoveAt(0);
                }
            }
        }
        
        private float CalculateMessagePriority(SwarmInformation info)
        {
            float priority = 0.5f;
            
            // High-quality player interactions are high priority
            if (info.playerInteractionQuality > 0.7f)
                priority += 0.3f;
            
            // Emotional states affect priority
            switch (info.emotionalState)
            {
                case EmotionalState.Fearful:
                case EmotionalState.Angry:
                    priority += 0.4f;
                    break;
                case EmotionalState.Happy:
                case EmotionalState.Excited:
                    priority += 0.2f;
                    break;
            }
            
            return Mathf.Clamp01(priority);
        }
        
        public List<SwarmMessage> GetMessages(int groupId)
        {
            return messageQueues.GetValueOrDefault(groupId, new List<SwarmMessage>());
        }
        
        public void Dispose()
        {
            messageQueues?.Clear();
        }
    }
    
    [System.Serializable]
    public class SwarmMessage
    {
        public SwarmInformation information;
        public float timestamp;
        public float propagationRadius;
        public float priority;
        public bool processed = false;
    }
}