using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeonQuest.Core;
using NeonQuest.AI;

namespace NeonQuest.Evolution
{
    /// <summary>
    /// Revolutionary Genetic Algorithm Evolution Engine for NPCs
    /// Evolves NPC intelligence, behavior, and consciousness through genetic algorithms
    /// Creates truly adaptive AI that improves over generations
    /// </summary>
    public class GeneticEvolutionEngine : NeonQuestComponent
    {
        [Header("🧬 Genetic Evolution Configuration")]
        [SerializeField] private bool enableGeneticEvolution = true;
        [SerializeField] private bool enableCrossover = true;
        [SerializeField] private bool enableMutation = true;
        [SerializeField] private bool enableNaturalSelection = true;
        
        [Header("🔬 Evolution Parameters")]
        [SerializeField] private int populationSize = 50;
        [SerializeField] private float mutationRate = 0.1f;
        [SerializeField] private float crossoverRate = 0.7f;
        [SerializeField] private int generationLifespan = 300; // seconds
        [SerializeField] private SelectionMethod selectionMethod = SelectionMethod.Tournament;
        
        // Evolution Components
        private List<NPCGenome> currentGeneration;
        private List<NPCGenome> nextGeneration;
        private GeneticOperators geneticOperators;
        private FitnessEvaluator fitnessEvaluator;
        private EvolutionHistory evolutionHistory;
        
        // Evolution State
        private int currentGenerationNumber;
        private float generationStartTime;
        private Dictionary<string, float> populationStats;
        private List<NPCNeuralBehavior> activeNPCs;
        
        protected override void OnInitialize()
        {
            LogDebug("🧬 Initializing Genetic Evolution Engine");
            
            try
            {
                // Initialize evolution components
                InitializeEvolutionSystem();
                
                // Create initial population
                CreateInitialPopulation();
                
                // Start evolution cycle
                StartEvolutionCycle();
                
                LogDebug($"✅ Genetic Evolution Engine initialized with population of {populationSize}");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Failed to initialize Genetic Evolution Engine: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeEvolutionSystem()
        {
            currentGeneration = new List<NPCGenome>();
            nextGeneration = new List<NPCGenome>();
            activeNPCs = new List<NPCNeuralBehavior>();
            populationStats = new Dictionary<string, float>();
            
            geneticOperators = new GeneticOperators(mutationRate, crossoverRate);
            fitnessEvaluator = new FitnessEvaluator();
            evolutionHistory = new EvolutionHistory();
            
            currentGenerationNumber = 0;
            generationStartTime = Time.time;
        }
        
        private void CreateInitialPopulation()
        {
            for (int i = 0; i < populationSize; i++)
            {
                var genome = GenerateRandomGenome();
                currentGeneration.Add(genome);
                
                // Spawn NPC from genome
                var npc = SpawnNPCFromGenome(genome);
                activeNPCs.Add(npc);
            }
            
            LogDebug($"🌱 Initial population of {populationSize} NPCs created");
        }
        
        private NPCGenome GenerateRandomGenome()
        {
            return new NPCGenome
            {
                genomeId = System.Guid.NewGuid().ToString(),
                generation = currentGenerationNumber,
                
                // Neural network genes
                neuralGenes = new NeuralGenes
                {
                    networkTopology = GenerateRandomTopology(),
                    activationFunctions = GenerateRandomActivations(),
                    learningRate = Random.Range(0.001f, 0.1f),
                    momentum = Random.Range(0.1f, 0.9f)
                },
                
                // Personality genes
                personalityGenes = new PersonalityGenes
                {
                    friendliness = Random.Range(0f, 1f),
                    curiosity = Random.Range(0f, 1f),
                    aggression = Random.Range(0f, 1f),
                    intelligence = Random.Range(0f, 1f),
                    adaptability = Random.Range(0f, 1f),
                    socialness = Random.Range(0f, 1f),
                    confidence = Random.Range(0f, 1f),
                    independence = Random.Range(0f, 1f)
                },
                
                // Behavioral genes
                behaviorGenes = new BehaviorGenes
                {
                    explorationTendency = Random.Range(0f, 1f),
                    riskTolerance = Random.Range(0f, 1f),
                    cooperationLevel = Random.Range(0f, 1f),
                    memoryCapacity = Random.Range(0.5f, 2f),
                    reactionSpeed = Random.Range(0.5f, 2f),
                    decisionMaking = Random.Range(0f, 1f)
                },
                
                // Initialize fitness
                fitness = 0f,
                age = 0f,
                isAlive = true
            };
        }
    }        

        private int[] GenerateRandomTopology()
        {
            int layers = Random.Range(3, 6); // 3-5 layers
            int[] topology = new int[layers];
            
            topology[0] = 10; // Input layer
            topology[layers - 1] = 8; // Output layer
            
            // Hidden layers
            for (int i = 1; i < layers - 1; i++)
            {
                topology[i] = Random.Range(8, 32);
            }
            
            return topology;
        }
        
        private ActivationType[] GenerateRandomActivations()
        {
            var types = System.Enum.GetValues(typeof(ActivationType)).Cast<ActivationType>().ToArray();
            int count = Random.Range(2, 5);
            var activations = new ActivationType[count];
            
            for (int i = 0; i < count; i++)
            {
                activations[i] = types[Random.Range(0, types.Length)];
            }
            
            return activations;
        }
        
        private NPCNeuralBehavior SpawnNPCFromGenome(NPCGenome genome)
        {
            var npcObject = new GameObject($"EvolvedNPC_{genome.genomeId[..8]}");
            npcObject.transform.SetParent(transform);
            npcObject.transform.position = GetRandomSpawnPosition();
            
            var npc = npcObject.AddComponent<NPCNeuralBehavior>();
            
            // Apply genome to NPC
            ApplyGenomeToNPC(npc, genome);
            
            return npc;
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            return new Vector3(
                Random.Range(-50f, 50f),
                0f,
                Random.Range(-50f, 50f)
            );
        }
        
        private void ApplyGenomeToNPC(NPCNeuralBehavior npc, NPCGenome genome)
        {
            // Apply personality genes
            var personality = new NPCPersonality
            {
                type = DeterminePersonalityType(genome.personalityGenes),
                friendliness = genome.personalityGenes.friendliness,
                curiosity = genome.personalityGenes.curiosity,
                aggression = genome.personalityGenes.aggression,
                intelligence = genome.personalityGenes.intelligence,
                adaptability = genome.personalityGenes.adaptability,
                socialness = genome.personalityGenes.socialness,
                confidence = genome.personalityGenes.confidence,
                independence = genome.personalityGenes.independence
            };
            
            npc.SetPersonality(personality);
            
            // Store genome reference
            var genomeComponent = npc.gameObject.AddComponent<NPCGenomeComponent>();
            genomeComponent.genome = genome;
        }
        
        private NPCPersonalityType DeterminePersonalityType(PersonalityGenes genes)
        {
            if (genes.friendliness > 0.7f) return NPCPersonalityType.Friendly;
            if (genes.aggression > 0.7f) return NPCPersonalityType.Aggressive;
            if (genes.curiosity > 0.7f) return NPCPersonalityType.Curious;
            if (genes.socialness < 0.3f) return NPCPersonalityType.Indifferent;
            return NPCPersonalityType.Helpful;
        }
        
        private void StartEvolutionCycle()
        {
            generationStartTime = Time.time;
            LogDebug($"🚀 Generation {currentGenerationNumber} started");
        }
        
        void Update()
        {
            if (!isInitialized || !enableGeneticEvolution) return;
            
            // Update NPC ages and fitness
            UpdatePopulationFitness();
            
            // Check if generation should evolve
            if (Time.time - generationStartTime > generationLifespan)
            {
                EvolveToNextGeneration();
            }
            
            // Update population statistics
            UpdatePopulationStats();
        }
        
        private void UpdatePopulationFitness()
        {
            for (int i = 0; i < activeNPCs.Count; i++)
            {
                var npc = activeNPCs[i];
                if (npc == null) continue;
                
                var genomeComponent = npc.GetComponent<NPCGenomeComponent>();
                if (genomeComponent?.genome != null)
                {
                    // Update age
                    genomeComponent.genome.age += Time.deltaTime;
                    
                    // Calculate fitness
                    genomeComponent.genome.fitness = fitnessEvaluator.EvaluateFitness(npc, genomeComponent.genome);
                }
            }
        }
        
        private void EvolveToNextGeneration()
        {
            LogDebug($"🧬 Evolving from generation {currentGenerationNumber} to {currentGenerationNumber + 1}");
            
            // Evaluate final fitness
            EvaluateFinalFitness();
            
            // Record generation statistics
            RecordGenerationStats();
            
            // Create next generation
            CreateNextGeneration();
            
            // Replace current population
            ReplacePopulation();
            
            // Start new generation
            currentGenerationNumber++;
            StartEvolutionCycle();
        }
        
        private void EvaluateFinalFitness()
        {
            foreach (var npc in activeNPCs)
            {
                if (npc == null) continue;
                
                var genomeComponent = npc.GetComponent<NPCGenomeComponent>();
                if (genomeComponent?.genome != null)
                {
                    genomeComponent.genome.fitness = fitnessEvaluator.EvaluateFinalFitness(npc, genomeComponent.genome);
                }
            }
        }
        
        private void RecordGenerationStats()
        {
            var validGenomes = currentGeneration.Where(g => g.isAlive).ToList();
            
            var generationStats = new GenerationStats
            {
                generationNumber = currentGenerationNumber,
                populationSize = validGenomes.Count,
                averageFitness = validGenomes.Average(g => g.fitness),
                maxFitness = validGenomes.Max(g => g.fitness),
                minFitness = validGenomes.Min(g => g.fitness),
                averageAge = validGenomes.Average(g => g.age),
                diversityIndex = CalculateDiversityIndex(validGenomes)
            };
            
            evolutionHistory.RecordGeneration(generationStats);
            
            LogDebug($"📊 Generation {currentGenerationNumber} stats - Avg Fitness: {generationStats.averageFitness:F2}, Max: {generationStats.maxFitness:F2}");
        }
        
        private float CalculateDiversityIndex(List<NPCGenome> genomes)
        {
            // Calculate genetic diversity using personality gene variance
            float totalVariance = 0f;
            int geneCount = 8; // Number of personality genes
            
            for (int i = 0; i < geneCount; i++)
            {
                var values = genomes.Select(g => GetPersonalityGeneValue(g.personalityGenes, i)).ToArray();
                totalVariance += CalculateVariance(values);
            }
            
            return totalVariance / geneCount;
        }
        
        private float GetPersonalityGeneValue(PersonalityGenes genes, int index)
        {
            return index switch
            {
                0 => genes.friendliness,
                1 => genes.curiosity,
                2 => genes.aggression,
                3 => genes.intelligence,
                4 => genes.adaptability,
                5 => genes.socialness,
                6 => genes.confidence,
                7 => genes.independence,
                _ => 0f
            };
        }
        
        private float CalculateVariance(float[] values)
        {
            if (values.Length == 0) return 0f;
            
            float mean = values.Average();
            float variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
            return variance;
        }
        
        private void CreateNextGeneration()
        {
            nextGeneration.Clear();
            
            // Sort by fitness
            var sortedGenomes = currentGeneration.Where(g => g.isAlive)
                                                .OrderByDescending(g => g.fitness)
                                                .ToList();
            
            // Elitism - keep best performers
            int eliteCount = Mathf.RoundToInt(populationSize * 0.1f); // Top 10%
            for (int i = 0; i < eliteCount && i < sortedGenomes.Count; i++)
            {
                var elite = CloneGenome(sortedGenomes[i]);
                elite.generation = currentGenerationNumber + 1;
                nextGeneration.Add(elite);
            }
            
            // Generate rest through selection, crossover, and mutation
            while (nextGeneration.Count < populationSize)
            {
                NPCGenome offspring;
                
                if (enableCrossover && Random.value < crossoverRate)
                {
                    // Crossover
                    var parent1 = SelectParent(sortedGenomes);
                    var parent2 = SelectParent(sortedGenomes);
                    offspring = geneticOperators.Crossover(parent1, parent2);
                }
                else
                {
                    // Asexual reproduction
                    var parent = SelectParent(sortedGenomes);
                    offspring = CloneGenome(parent);
                }
                
                // Mutation
                if (enableMutation && Random.value < mutationRate)
                {
                    offspring = geneticOperators.Mutate(offspring);
                }
                
                offspring.generation = currentGenerationNumber + 1;
                offspring.fitness = 0f;
                offspring.age = 0f;
                offspring.isAlive = true;
                
                nextGeneration.Add(offspring);
            }
        }
        
        private NPCGenome SelectParent(List<NPCGenome> sortedGenomes)
        {
            return selectionMethod switch
            {
                SelectionMethod.Tournament => TournamentSelection(sortedGenomes),
                SelectionMethod.Roulette => RouletteSelection(sortedGenomes),
                SelectionMethod.Rank => RankSelection(sortedGenomes),
                _ => sortedGenomes[Random.Range(0, Mathf.Min(10, sortedGenomes.Count))]
            };
        }
        
        private NPCGenome TournamentSelection(List<NPCGenome> genomes)
        {
            int tournamentSize = 5;
            var tournament = new List<NPCGenome>();
            
            for (int i = 0; i < tournamentSize && i < genomes.Count; i++)
            {
                tournament.Add(genomes[Random.Range(0, genomes.Count)]);
            }
            
            return tournament.OrderByDescending(g => g.fitness).First();
        }
        
        private NPCGenome RouletteSelection(List<NPCGenome> genomes)
        {
            float totalFitness = genomes.Sum(g => Mathf.Max(0f, g.fitness));
            if (totalFitness <= 0f) return genomes[Random.Range(0, genomes.Count)];
            
            float randomValue = Random.Range(0f, totalFitness);
            float currentSum = 0f;
            
            foreach (var genome in genomes)
            {
                currentSum += Mathf.Max(0f, genome.fitness);
                if (currentSum >= randomValue)
                    return genome;
            }
            
            return genomes.Last();
        }
        
        private NPCGenome RankSelection(List<NPCGenome> genomes)
        {
            // Select based on rank rather than raw fitness
            int totalRanks = genomes.Count * (genomes.Count + 1) / 2;
            int randomRank = Random.Range(1, totalRanks + 1);
            
            int currentRank = 0;
            for (int i = genomes.Count - 1; i >= 0; i--)
            {
                currentRank += i + 1;
                if (currentRank >= randomRank)
                    return genomes[i];
            }
            
            return genomes.First();
        }
        
        private NPCGenome CloneGenome(NPCGenome original)
        {
            return new NPCGenome
            {
                genomeId = System.Guid.NewGuid().ToString(),
                generation = original.generation,
                neuralGenes = CloneNeuralGenes(original.neuralGenes),
                personalityGenes = ClonePersonalityGenes(original.personalityGenes),
                behaviorGenes = CloneBehaviorGenes(original.behaviorGenes),
                fitness = 0f,
                age = 0f,
                isAlive = true
            };
        }
        
        private NeuralGenes CloneNeuralGenes(NeuralGenes original)
        {
            return new NeuralGenes
            {
                networkTopology = (int[])original.networkTopology.Clone(),
                activationFunctions = (ActivationType[])original.activationFunctions.Clone(),
                learningRate = original.learningRate,
                momentum = original.momentum
            };
        }
        
        private PersonalityGenes ClonePersonalityGenes(PersonalityGenes original)
        {
            return new PersonalityGenes
            {
                friendliness = original.friendliness,
                curiosity = original.curiosity,
                aggression = original.aggression,
                intelligence = original.intelligence,
                adaptability = original.adaptability,
                socialness = original.socialness,
                confidence = original.confidence,
                independence = original.independence
            };
        }
        
        private BehaviorGenes CloneBehaviorGenes(BehaviorGenes original)
        {
            return new BehaviorGenes
            {
                explorationTendency = original.explorationTendency,
                riskTolerance = original.riskTolerance,
                cooperationLevel = original.cooperationLevel,
                memoryCapacity = original.memoryCapacity,
                reactionSpeed = original.reactionSpeed,
                decisionMaking = original.decisionMaking
            };
        }
        
        private void ReplacePopulation()
        {
            // Destroy old NPCs
            foreach (var npc in activeNPCs)
            {
                if (npc != null)
                {
                    Destroy(npc.gameObject);
                }
            }
            
            activeNPCs.Clear();
            
            // Spawn new generation
            foreach (var genome in nextGeneration)
            {
                var npc = SpawnNPCFromGenome(genome);
                activeNPCs.Add(npc);
            }
            
            // Update current generation
            currentGeneration = new List<NPCGenome>(nextGeneration);
            nextGeneration.Clear();
        }
        
        private void UpdatePopulationStats()
        {
            if (currentGeneration.Count == 0) return;
            
            var aliveGenomes = currentGeneration.Where(g => g.isAlive).ToList();
            
            populationStats["population_size"] = aliveGenomes.Count;
            populationStats["average_fitness"] = aliveGenomes.Count > 0 ? aliveGenomes.Average(g => g.fitness) : 0f;
            populationStats["max_fitness"] = aliveGenomes.Count > 0 ? aliveGenomes.Max(g => g.fitness) : 0f;
            populationStats["average_age"] = aliveGenomes.Count > 0 ? aliveGenomes.Average(g => g.age) : 0f;
            populationStats["diversity_index"] = CalculateDiversityIndex(aliveGenomes);
        }
        
        #region Public API
        
        public EvolutionStats GetEvolutionStats()
        {
            return new EvolutionStats
            {
                currentGeneration = currentGenerationNumber,
                populationSize = activeNPCs.Count,
                averageFitness = populationStats.GetValueOrDefault("average_fitness", 0f),
                maxFitness = populationStats.GetValueOrDefault("max_fitness", 0f),
                diversityIndex = populationStats.GetValueOrDefault("diversity_index", 0f),
                generationProgress = (Time.time - generationStartTime) / generationLifespan,
                totalGenerations = evolutionHistory.GetTotalGenerations()
            };
        }
        
        public List<GenerationStats> GetEvolutionHistory()
        {
            return evolutionHistory.GetHistory();
        }
        
        public NPCGenome GetBestGenome()
        {
            return currentGeneration.Where(g => g.isAlive).OrderByDescending(g => g.fitness).FirstOrDefault();
        }
        
        public void ForceEvolution()
        {
            EvolveToNextGeneration();
        }
        
        #endregion
        
        protected override void OnCleanup()
        {
            foreach (var npc in activeNPCs)
            {
                if (npc != null)
                {
                    Destroy(npc.gameObject);
                }
            }
            
            geneticOperators?.Dispose();
            fitnessEvaluator?.Dispose();
            evolutionHistory?.Dispose();
            
            LogDebug("🧬 Genetic Evolution Engine cleaned up");
        }
    }
    
    #region Supporting Classes and Enums
    
    public enum SelectionMethod
    {
        Tournament,
        Roulette,
        Rank,
        Random
    }
    
    public enum ActivationType
    {
        Sigmoid,
        ReLU,
        Tanh,
        Softmax,
        Linear
    }
    
    [System.Serializable]
    public class NPCGenome
    {
        public string genomeId;
        public int generation;
        public NeuralGenes neuralGenes;
        public PersonalityGenes personalityGenes;
        public BehaviorGenes behaviorGenes;
        public float fitness;
        public float age;
        public bool isAlive;
    }
    
    [System.Serializable]
    public class NeuralGenes
    {
        public int[] networkTopology;
        public ActivationType[] activationFunctions;
        public float learningRate;
        public float momentum;
    }
    
    [System.Serializable]
    public class PersonalityGenes
    {
        public float friendliness;
        public float curiosity;
        public float aggression;
        public float intelligence;
        public float adaptability;
        public float socialness;
        public float confidence;
        public float independence;
    }
    
    [System.Serializable]
    public class BehaviorGenes
    {
        public float explorationTendency;
        public float riskTolerance;
        public float cooperationLevel;
        public float memoryCapacity;
        public float reactionSpeed;
        public float decisionMaking;
    }
    
    [System.Serializable]
    public class GenerationStats
    {
        public int generationNumber;
        public int populationSize;
        public float averageFitness;
        public float maxFitness;
        public float minFitness;
        public float averageAge;
        public float diversityIndex;
    }
    
    [System.Serializable]
    public class EvolutionStats
    {
        public int currentGeneration;
        public int populationSize;
        public float averageFitness;
        public float maxFitness;
        public float diversityIndex;
        public float generationProgress;
        public int totalGenerations;
    }
    
    public class NPCGenomeComponent : MonoBehaviour
    {
        public NPCGenome genome;
    }
    
    public class GeneticOperators : System.IDisposable
    {
        private float mutationRate;
        private float crossoverRate;
        
        public GeneticOperators(float mutRate, float crossRate)
        {
            mutationRate = mutRate;
            crossoverRate = crossRate;
        }
        
        public NPCGenome Crossover(NPCGenome parent1, NPCGenome parent2)
        {
            var offspring = new NPCGenome
            {
                genomeId = System.Guid.NewGuid().ToString(),
                generation = Mathf.Max(parent1.generation, parent2.generation),
                neuralGenes = CrossoverNeuralGenes(parent1.neuralGenes, parent2.neuralGenes),
                personalityGenes = CrossoverPersonalityGenes(parent1.personalityGenes, parent2.personalityGenes),
                behaviorGenes = CrossoverBehaviorGenes(parent1.behaviorGenes, parent2.behaviorGenes),
                fitness = 0f,
                age = 0f,
                isAlive = true
            };
            
            return offspring;
        }
        
        private NeuralGenes CrossoverNeuralGenes(NeuralGenes parent1, NeuralGenes parent2)
        {
            return new NeuralGenes
            {
                networkTopology = Random.value < 0.5f ? parent1.networkTopology : parent2.networkTopology,
                activationFunctions = Random.value < 0.5f ? parent1.activationFunctions : parent2.activationFunctions,
                learningRate = (parent1.learningRate + parent2.learningRate) * 0.5f,
                momentum = (parent1.momentum + parent2.momentum) * 0.5f
            };
        }
        
        private PersonalityGenes CrossoverPersonalityGenes(PersonalityGenes parent1, PersonalityGenes parent2)
        {
            return new PersonalityGenes
            {
                friendliness = Random.value < 0.5f ? parent1.friendliness : parent2.friendliness,
                curiosity = Random.value < 0.5f ? parent1.curiosity : parent2.curiosity,
                aggression = Random.value < 0.5f ? parent1.aggression : parent2.aggression,
                intelligence = Random.value < 0.5f ? parent1.intelligence : parent2.intelligence,
                adaptability = Random.value < 0.5f ? parent1.adaptability : parent2.adaptability,
                socialness = Random.value < 0.5f ? parent1.socialness : parent2.socialness,
                confidence = Random.value < 0.5f ? parent1.confidence : parent2.confidence,
                independence = Random.value < 0.5f ? parent1.independence : parent2.independence
            };
        }
        
        private BehaviorGenes CrossoverBehaviorGenes(BehaviorGenes parent1, BehaviorGenes parent2)
        {
            return new BehaviorGenes
            {
                explorationTendency = Random.value < 0.5f ? parent1.explorationTendency : parent2.explorationTendency,
                riskTolerance = Random.value < 0.5f ? parent1.riskTolerance : parent2.riskTolerance,
                cooperationLevel = Random.value < 0.5f ? parent1.cooperationLevel : parent2.cooperationLevel,
                memoryCapacity = Random.value < 0.5f ? parent1.memoryCapacity : parent2.memoryCapacity,
                reactionSpeed = Random.value < 0.5f ? parent1.reactionSpeed : parent2.reactionSpeed,
                decisionMaking = Random.value < 0.5f ? parent1.decisionMaking : parent2.decisionMaking
            };
        }
        
        public NPCGenome Mutate(NPCGenome genome)
        {
            // Mutate personality genes
            if (Random.value < mutationRate)
                genome.personalityGenes.friendliness = Mathf.Clamp01(genome.personalityGenes.friendliness + Random.Range(-0.1f, 0.1f));
            if (Random.value < mutationRate)
                genome.personalityGenes.curiosity = Mathf.Clamp01(genome.personalityGenes.curiosity + Random.Range(-0.1f, 0.1f));
            if (Random.value < mutationRate)
                genome.personalityGenes.aggression = Mathf.Clamp01(genome.personalityGenes.aggression + Random.Range(-0.1f, 0.1f));
            
            // Mutate behavior genes
            if (Random.value < mutationRate)
                genome.behaviorGenes.explorationTendency = Mathf.Clamp01(genome.behaviorGenes.explorationTendency + Random.Range(-0.1f, 0.1f));
            if (Random.value < mutationRate)
                genome.behaviorGenes.riskTolerance = Mathf.Clamp01(genome.behaviorGenes.riskTolerance + Random.Range(-0.1f, 0.1f));
            
            // Mutate neural genes
            if (Random.value < mutationRate)
                genome.neuralGenes.learningRate = Mathf.Clamp(genome.neuralGenes.learningRate + Random.Range(-0.01f, 0.01f), 0.001f, 0.1f);
            
            return genome;
        }
        
        public void Dispose()
        {
            // Cleanup genetic operators
        }
    }
    
    public class FitnessEvaluator : System.IDisposable
    {
        public float EvaluateFitness(NPCNeuralBehavior npc, NPCGenome genome)
        {
            float fitness = 0f;
            
            // Survival fitness (age bonus)
            fitness += genome.age * 0.1f;
            
            // Interaction quality
            var interactions = npc.GetInteractionHistory();
            if (interactions.Length > 0)
            {
                fitness += interactions.Average(i => i.playerEngagement) * 10f;
                fitness += interactions.Average(i => i.emotionalResonance) * 5f;
            }
            
            // Behavioral diversity
            var personality = npc.GetPersonality();
            fitness += CalculateBehavioralDiversity(personality) * 3f;
            
            // Learning efficiency
            fitness += genome.neuralGenes.learningRate * 20f;
            
            return Mathf.Max(0f, fitness);
        }
        
        public float EvaluateFinalFitness(NPCNeuralBehavior npc, NPCGenome genome)
        {
            float baseFitness = EvaluateFitness(npc, genome);
            
            // Add longevity bonus
            float longevityBonus = genome.age / 300f * 5f; // Max 5 points for surviving full generation
            
            // Add social success bonus
            var status = npc.GetNPCStatus();
            float socialBonus = status.swarmSize * 0.5f; // Bonus for being part of larger swarms
            
            return baseFitness + longevityBonus + socialBonus;
        }
        
        private float CalculateBehavioralDiversity(NPCPersonality personality)
        {
            // Reward balanced personalities
            float[] traits = {
                personality.friendliness,
                personality.curiosity,
                personality.aggression,
                personality.intelligence,
                personality.adaptability,
                personality.socialness,
                personality.confidence,
                personality.independence
            };
            
            float mean = traits.Average();
            float variance = traits.Sum(t => (t - mean) * (t - mean)) / traits.Length;
            
            // Higher variance = more diverse personality = higher fitness
            return variance * 10f;
        }
        
        public void Dispose()
        {
            // Cleanup fitness evaluator
        }
    }
    
    public class EvolutionHistory : System.IDisposable
    {
        private List<GenerationStats> history;
        
        public EvolutionHistory()
        {
            history = new List<GenerationStats>();
        }
        
        public void RecordGeneration(GenerationStats stats)
        {
            history.Add(stats);
            
            // Keep only last 100 generations
            if (history.Count > 100)
            {
                history.RemoveAt(0);
            }
        }
        
        public List<GenerationStats> GetHistory()
        {
            return new List<GenerationStats>(history);
        }
        
        public int GetTotalGenerations()
        {
            return history.Count;
        }
        
        public void Dispose()
        {
            history?.Clear();
        }
    }
    
    #endregion
}