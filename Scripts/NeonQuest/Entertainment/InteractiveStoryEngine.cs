using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeonQuest.Entertainment
{
    /// <summary>
    /// AI-Powered Interactive Storytelling Engine with dynamic narrative generation
    /// Perfect for Code with Kiro Hackathon - Games & Entertainment category
    /// </summary>
    public class InteractiveStoryEngine : MonoBehaviour
    {
        [Header("Interactive Story Configuration")]
        public bool enableDynamicNarrative = true;
        public bool enableEmotionalAdaptation = true;
        public int maxStoryBranches = 20;
        public float narrativeUpdateInterval = 2.0f;
        
        private NarrativeAI narrativeAI;
        private EmotionalStorytellingEngine emotionalEngine;
        private CharacterPersonalitySystem personalitySystem;
        private StoryBranchingManager branchingManager;
        private PlayerChoiceAnalyzer choiceAnalyzer;
        
        // Story state management
        private StoryState currentStoryState;
        private Queue<StoryEvent> pendingEvents;
        private Dictionary<string, CharacterArc> characterArcs;
        private List<PlayerChoice> playerChoiceHistory;
        
        // Dynamic narrative elements
        private float playerEngagementLevel;
        private EmotionalTone currentTone;
        private List<StoryTheme> activeThemes;
        
        void Start()
        {
            InitializeStoryEngine();
            LoadInitialNarrative();
            StartDynamicStorytelling();
        }
        
        void Update()
        {
            if (enableDynamicNarrative)
            {
                UpdateNarrativeFlow();
                AnalyzePlayerEngagement();
                AdaptStoryToPlayerBehavior();
            }
        }
        
        private void InitializeStoryEngine()
        {
            narrativeAI = new NarrativeAI();
            emotionalEngine = new EmotionalStorytellingEngine();
            personalitySystem = new CharacterPersonalitySystem();
            branchingManager = new StoryBranchingManager();
            choiceAnalyzer = new PlayerChoiceAnalyzer();
            
            currentStoryState = new StoryState();
            pendingEvents = new Queue<StoryEvent>();
            characterArcs = new Dictionary<string, CharacterArc>();
            playerChoiceHistory = new List<PlayerChoice>();
            activeThemes = new List<StoryTheme>();
            
            playerEngagementLevel = 0.5f;
            currentTone = EmotionalTone.Neutral;
            
            Debug.Log("📚 Interactive Story Engine initialized - Ready for dynamic storytelling!");
        }
        
        private void LoadInitialNarrative()
        {
            // Generate initial cyberpunk narrative
            var initialStory = narrativeAI.GenerateOpeningNarrative(new NarrativeParameters
            {
                Genre = StoryGenre.Cyberpunk,
                Tone = EmotionalTone.Mysterious,
                Setting = "Neo-Tokyo 2087",
                MainThemes = new[] { "Identity", "Technology", "Rebellion" }
            });
            
            currentStoryState = initialStory.InitialState;
            
            // Create main characters with AI-generated personalities
            CreateMainCharacters();
            
            // Set up initial story branches
            var initialBranches = branchingManager.GenerateInitialBranches(currentStoryState);
            foreach (var branch in initialBranches)
            {
                currentStoryState.AvailableBranches.Add(branch);
            }
            
            Debug.Log($"🎭 Loaded initial narrative: '{initialStory.Title}'");
            Debug.Log($"🌟 Generated {initialBranches.Count} story branches");
        }
        
        private void CreateMainCharacters()
        {
            var characters = new[]
            {
                new CharacterDefinition
                {
                    Id = "protagonist",
                    Name = "Alex Chen",
                    Role = CharacterRole.Protagonist,
                    Personality = personalitySystem.GeneratePersonality(new[] { "Curious", "Rebellious", "Tech-Savvy" }),
                    BackgroundTraits = new[] { "Former corporate hacker", "Seeks truth", "Distrusts authority" }
                },
                new CharacterDefinition
                {
                    Id = "mentor",
                    Name = "Dr. Yuki Tanaka",
                    Role = CharacterRole.Mentor,
                    Personality = personalitySystem.GeneratePersonality(new[] { "Wise", "Cautious", "Protective" }),
                    BackgroundTraits = new[] { "AI researcher", "Lost everything to the corporations", "Guides from shadows" }
                },
                new CharacterDefinition
                {
                    Id = "antagonist",
                    Name = "Director Kane",
                    Role = CharacterRole.Antagonist,
                    Personality = personalitySystem.GeneratePersonality(new[] { "Ruthless", "Intelligent", "Manipulative" }),
                    BackgroundTraits = new[] { "Corporate executive", "Controls information flow", "Believes in order through control" }
                }
            };
            
            foreach (var character in characters)
            {
                var arc = narrativeAI.GenerateCharacterArc(character, currentStoryState);
                characterArcs[character.Id] = arc;
                
                Debug.Log($"👤 Created character: {character.Name} ({character.Role})");
            }
        }
        
        private void StartDynamicStorytelling()
        {
            InvokeRepeating(nameof(GenerateDynamicStoryEvents), 1f, narrativeUpdateInterval);
        }
        
        private void GenerateDynamicStoryEvents()
        {
            // AI-generated story events based on current state
            var contextualEvents = narrativeAI.GenerateContextualEvents(
                currentStoryState, 
                playerChoiceHistory, 
                playerEngagementLevel
            );
            
            foreach (var storyEvent in contextualEvents)
            {
                pendingEvents.Enqueue(storyEvent);
            }
            
            // Process pending events
            ProcessPendingStoryEvents();
        }
        
        private void ProcessPendingStoryEvents()
        {
            while (pendingEvents.Count > 0)
            {
                var storyEvent = pendingEvents.Dequeue();
                ExecuteStoryEvent(storyEvent);
            }
        }
        
        private void ExecuteStoryEvent(StoryEvent storyEvent)
        {
            switch (storyEvent.Type)
            {
                case StoryEventType.CharacterInteraction:
                    ExecuteCharacterInteraction(storyEvent);
                    break;
                case StoryEventType.PlotTwist:
                    ExecutePlotTwist(storyEvent);
                    break;
                case StoryEventType.EnvironmentalChange:
                    ExecuteEnvironmentalChange(storyEvent);
                    break;
                case StoryEventType.PlayerChoice:
                    PresentPlayerChoice(storyEvent);
                    break;
            }
            
            // Update story state
            currentStoryState.CurrentChapter = storyEvent.ChapterNumber;
            currentStoryState.LastEventTime = Time.time;
            
            Debug.Log($"📖 Story Event: {storyEvent.Title}");
            Debug.Log($"💭 Description: {storyEvent.Description}");
        }
        
        private void ExecuteCharacterInteraction(StoryEvent storyEvent)
        {
            var characterId = storyEvent.Parameters.GetValueOrDefault("character_id", "");
            if (characterArcs.TryGetValue(characterId, out var arc))
            {
                var dialogue = narrativeAI.GenerateDialogue(arc.Character, currentStoryState);
                DisplayDialogue(arc.Character.Name, dialogue);
                
                // Update character relationship based on interaction
                arc.RelationshipWithPlayer += storyEvent.EmotionalImpact * 0.1f;
                arc.RelationshipWithPlayer = Mathf.Clamp(arc.RelationshipWithPlayer, -1f, 1f);
            }
        }
        
        private void ExecutePlotTwist(StoryEvent storyEvent)
        {
            var twist = narrativeAI.GeneratePlotTwist(currentStoryState, playerChoiceHistory);
            
            DisplayNarrative($"🌟 PLOT TWIST: {twist.Title}", twist.Description);
            
            // Dramatically alter available story branches
            currentStoryState.AvailableBranches.Clear();
            var newBranches = branchingManager.GenerateTwistBranches(twist, currentStoryState);
            currentStoryState.AvailableBranches.AddRange(newBranches);
            
            // Update emotional tone
            currentTone = twist.ResultingTone;
            emotionalEngine.TransitionToTone(currentTone);
        }
        
        private void ExecuteEnvironmentalChange(StoryEvent storyEvent)
        {
            var environmentChange = narrativeAI.GenerateEnvironmentalNarrative(storyEvent);
            DisplayNarrative("🌆 Environment", environmentChange);
            
            // Trigger visual/audio changes in the game world
            TriggerEnvironmentalEffects(storyEvent);
        }
        
        private void PresentPlayerChoice(StoryEvent storyEvent)
        {
            var choices = narrativeAI.GeneratePlayerChoices(currentStoryState, 3);
            DisplayPlayerChoices(storyEvent.Title, storyEvent.Description, choices);
        }
        
        private void UpdateNarrativeFlow()
        {
            // AI-driven narrative pacing
            var pacingAnalysis = narrativeAI.AnalyzeNarrativePacing(currentStoryState, playerChoiceHistory);
            
            if (pacingAnalysis.ShouldAccelerate)
            {
                narrativeUpdateInterval = Mathf.Max(1f, narrativeUpdateInterval * 0.8f);
            }
            else if (pacingAnalysis.ShouldDecelerate)
            {
                narrativeUpdateInterval = Mathf.Min(5f, narrativeUpdateInterval * 1.2f);
            }
        }
        
        private void AnalyzePlayerEngagement()
        {
            var engagement = choiceAnalyzer.AnalyzeEngagement(playerChoiceHistory);
            playerEngagementLevel = Mathf.Lerp(playerEngagementLevel, engagement, Time.deltaTime * 0.1f);
            
            // Adapt story based on engagement
            if (playerEngagementLevel < 0.3f)
            {
                // Generate more exciting events
                var excitingEvent = narrativeAI.GenerateHighImpactEvent(currentStoryState);
                pendingEvents.Enqueue(excitingEvent);
            }
        }
        
        private void AdaptStoryToPlayerBehavior()
        {
            if (!enableEmotionalAdaptation) return;
            
            var behaviorPattern = choiceAnalyzer.AnalyzeBehaviorPattern(playerChoiceHistory);
            var adaptedNarrative = emotionalEngine.AdaptNarrativeToPlayer(behaviorPattern, currentStoryState);
            
            if (adaptedNarrative != null)
            {
                currentStoryState.NarrativeStyle = adaptedNarrative.Style;
                currentTone = adaptedNarrative.PreferredTone;
            }
        }
        
        private void TriggerEnvironmentalEffects(StoryEvent storyEvent)
        {
            // Integration with other NeonQuest systems
            var weatherSystem = FindObjectOfType<AIWeatherSystem>();
            var lightingEngine = FindObjectOfType<QuantumLightingEngine>();
            var audioEngine = FindObjectOfType<AudioEngine>();
            
            if (weatherSystem != null)
            {
                weatherSystem.AdaptToStoryMood(currentTone);
            }
            
            if (lightingEngine != null)
            {
                lightingEngine.SetNarrativeLighting(storyEvent.EmotionalImpact);
            }
            
            if (audioEngine != null)
            {
                audioEngine.PlayNarrativeAmbience(currentTone);
            }
        }
        
        private void DisplayNarrative(string title, string content)
        {
            Debug.Log($"📚 {title}");
            Debug.Log($"📖 {content}");
            
            // Integration with holographic UI
            var holoUI = FindObjectOfType<HolographicUISystem>();
            if (holoUI != null)
            {
                holoUI.DisplayNarrative(title, content);
            }
        }
        
        private void DisplayDialogue(string characterName, string dialogue)
        {
            Debug.Log($"💬 {characterName}: \"{dialogue}\"");
            
            // Integration with holographic UI for dialogue display
            var holoUI = FindObjectOfType<HolographicUISystem>();
            if (holoUI != null)
            {
                holoUI.DisplayDialogue(characterName, dialogue);
            }
        }
        
        private void DisplayPlayerChoices(string title, string context, List<PlayerChoice> choices)
        {
            Debug.Log($"🎯 {title}");
            Debug.Log($"📝 {context}");
            Debug.Log("Choose your path:");
            
            for (int i = 0; i < choices.Count; i++)
            {
                Debug.Log($"{i + 1}. {choices[i].Text} (Impact: {choices[i].EmotionalImpact:F1})");
            }
            
            // In a real implementation, this would present UI choices to the player
            // For demo purposes, we'll simulate a random choice
            SimulatePlayerChoice(choices);
        }
        
        private void SimulatePlayerChoice(List<PlayerChoice> choices)
        {
            if (choices.Count == 0) return;
            
            var selectedChoice = choices[Random.Range(0, choices.Count)];
            HandlePlayerChoice(selectedChoice);
        }
        
        public void HandlePlayerChoice(PlayerChoice choice)
        {
            playerChoiceHistory.Add(choice);
            
            // Apply choice consequences
            currentStoryState.PlayerMoralAlignment += choice.MoralImpact;
            currentStoryState.PlayerMoralAlignment = Mathf.Clamp(currentStoryState.PlayerMoralAlignment, -1f, 1f);
            
            // Update character relationships
            foreach (var relationship in choice.CharacterImpacts)
            {
                if (characterArcs.TryGetValue(relationship.Key, out var arc))
                {
                    arc.RelationshipWithPlayer += relationship.Value;
                    arc.RelationshipWithPlayer = Mathf.Clamp(arc.RelationshipWithPlayer, -1f, 1f);
                }
            }
            
            // Generate consequences
            var consequences = narrativeAI.GenerateChoiceConsequences(choice, currentStoryState);
            foreach (var consequence in consequences)
            {
                pendingEvents.Enqueue(consequence);
            }
            
            Debug.Log($"✅ Player chose: {choice.Text}");
            Debug.Log($"📊 Moral alignment: {currentStoryState.PlayerMoralAlignment:F2}");
        }
        
        // Public API for external systems
        public StoryState GetCurrentStoryState()
        {
            return currentStoryState;
        }
        
        public List<PlayerChoice> GetChoiceHistory()
        {
            return playerChoiceHistory.ToList();
        }
        
        public float GetPlayerEngagement()
        {
            return playerEngagementLevel;
        }
        
        public EmotionalTone GetCurrentTone()
        {
            return currentTone;
        }
    }
    
    // Supporting data structures for interactive storytelling
    [System.Serializable]
    public class StoryState
    {
        public int CurrentChapter = 1;
        public float PlayerMoralAlignment = 0f;
        public string NarrativeStyle = "Cyberpunk";
        public List<StoryBranch> AvailableBranches = new List<StoryBranch>();
        public float LastEventTime;
    }
    
    [System.Serializable]
    public class StoryEvent
    {
        public string Id;
        public string Title;
        public string Description;
        public StoryEventType Type;
        public int ChapterNumber;
        public float EmotionalImpact;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
    }
    
    public enum StoryEventType
    {
        CharacterInteraction,
        PlotTwist,
        EnvironmentalChange,
        PlayerChoice
    }
    
    [System.Serializable]
    public class PlayerChoice
    {
        public string Id;
        public string Text;
        public float EmotionalImpact;
        public float MoralImpact;
        public Dictionary<string, float> CharacterImpacts = new Dictionary<string, float>();
    }
    
    [System.Serializable]
    public class CharacterArc
    {
        public CharacterDefinition Character;
        public float RelationshipWithPlayer;
        public List<string> CompletedStoryBeats = new List<string>();
    }
    
    [System.Serializable]
    public class CharacterDefinition
    {
        public string Id;
        public string Name;
        public CharacterRole Role;
        public Dictionary<string, float> Personality;
        public string[] BackgroundTraits;
    }
    
    public enum CharacterRole
    {
        Protagonist,
        Mentor,
        Antagonist,
        Ally,
        Neutral
    }
    
    public enum EmotionalTone
    {
        Hopeful,
        Mysterious,
        Tense,
        Melancholic,
        Triumphant,
        Neutral
    }
    
    public enum StoryGenre
    {
        Cyberpunk,
        SciFi,
        Fantasy,
        Mystery,
        Adventure
    }
    
    [System.Serializable]
    public class StoryBranch
    {
        public string Id;
        public string Title;
        public string Description;
        public List<string> Prerequisites = new List<string>();
    }
    
    [System.Serializable]
    public class StoryTheme
    {
        public string Name;
        public float Intensity;
        public string Description;
    }
}