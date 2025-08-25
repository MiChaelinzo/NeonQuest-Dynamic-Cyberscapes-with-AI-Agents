using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeonQuest.Education
{
    /// <summary>
    /// AI-Powered Tutorial System that adapts to player learning patterns
    /// Perfect for Code with Kiro Hackathon - Educational Apps category
    /// </summary>
    public class AITutorSystem : MonoBehaviour
    {
        [Header("AI Learning Configuration")]
        public float adaptiveDifficultyRate = 0.1f;
        public int maxTutorialSteps = 50;
        public bool enablePersonalizedLearning = true;
        
        private LearningAnalytics analytics;
        private PersonalizedCurriculum curriculum;
        private InteractiveTutorialEngine tutorialEngine;
        private KnowledgeGraph knowledgeGraph;
        
        // AI-driven learning metrics
        private Dictionary<string, float> playerSkillLevels;
        private List<LearningObjective> completedObjectives;
        private Queue<TutorialStep> adaptiveStepQueue;
        
        void Start()
        {
            InitializeAITutorSystem();
            GeneratePersonalizedLearningPath();
        }
        
        void Update()
        {
            if (enablePersonalizedLearning)
            {
                AnalyzePlayerProgress();
                AdaptTutorialDifficulty();
                UpdateLearningRecommendations();
            }
        }
        
        private void InitializeAITutorSystem()
        {
            analytics = new LearningAnalytics();
            curriculum = new PersonalizedCurriculum();
            tutorialEngine = new InteractiveTutorialEngine();
            knowledgeGraph = new KnowledgeGraph();
            
            playerSkillLevels = new Dictionary<string, float>();
            completedObjectives = new List<LearningObjective>();
            adaptiveStepQueue = new Queue<TutorialStep>();
            
            Debug.Log("🎓 AI Tutor System initialized - Ready for personalized learning!");
        }
        
        public void GeneratePersonalizedLearningPath()
        {
            var playerProfile = analytics.GetPlayerLearningProfile();
            var recommendedPath = curriculum.GenerateAdaptivePath(playerProfile);
            
            foreach (var step in recommendedPath)
            {
                adaptiveStepQueue.Enqueue(step);
            }
            
            Debug.Log($"📚 Generated personalized learning path with {recommendedPath.Count} steps");
        }
        
        private void AnalyzePlayerProgress()
        {
            var currentPerformance = analytics.AnalyzeRealtimePerformance();
            var learningVelocity = analytics.CalculateLearningVelocity();
            var comprehensionLevel = analytics.AssessComprehension();
            
            // Update skill levels based on AI analysis
            foreach (var skill in playerSkillLevels.Keys.ToList())
            {
                var improvement = currentPerformance.GetSkillImprovement(skill);
                playerSkillLevels[skill] = Mathf.Clamp01(playerSkillLevels[skill] + improvement);
            }
        }
        
        private void AdaptTutorialDifficulty()
        {
            var averageSkillLevel = playerSkillLevels.Values.Average();
            var targetDifficulty = curriculum.CalculateOptimalDifficulty(averageSkillLevel);
            
            tutorialEngine.AdjustDifficulty(targetDifficulty);
            
            // Generate new tutorial steps if needed
            if (adaptiveStepQueue.Count < 5)
            {
                var newSteps = curriculum.GenerateAdaptiveSteps(playerSkillLevels, 10);
                foreach (var step in newSteps)
                {
                    adaptiveStepQueue.Enqueue(step);
                }
            }
        }
        
        private void UpdateLearningRecommendations()
        {
            var weakAreas = analytics.IdentifyWeakAreas(playerSkillLevels);
            var recommendations = curriculum.GenerateRecommendations(weakAreas);
            
            // Display AI-generated learning suggestions
            foreach (var recommendation in recommendations)
            {
                DisplayLearningRecommendation(recommendation);
            }
        }
        
        public void StartInteractiveTutorial(string topicId)
        {
            var tutorial = tutorialEngine.CreateInteractiveTutorial(topicId);
            tutorial.OnStepCompleted += HandleTutorialStepCompleted;
            tutorial.OnTutorialCompleted += HandleTutorialCompleted;
            
            tutorial.Begin();
            Debug.Log($"🚀 Started interactive tutorial: {topicId}");
        }
        
        private void HandleTutorialStepCompleted(TutorialStep step)
        {
            analytics.RecordStepCompletion(step);
            
            // AI-powered feedback generation
            var feedback = GenerateAIFeedback(step);
            DisplayFeedback(feedback);
            
            // Update knowledge graph
            knowledgeGraph.UpdateKnowledgeNode(step.ConceptId, step.CompletionScore);
        }
        
        private void HandleTutorialCompleted(InteractiveTutorial tutorial)
        {
            var objective = new LearningObjective
            {
                Id = tutorial.Id,
                CompletionTime = Time.time,
                MasteryLevel = tutorial.CalculateMasteryLevel()
            };
            
            completedObjectives.Add(objective);
            
            // Generate next learning recommendations
            GeneratePersonalizedLearningPath();
            
            Debug.Log($"✅ Tutorial completed: {tutorial.Id} - Mastery: {objective.MasteryLevel:F2}");
        }
        
        private AIFeedback GenerateAIFeedback(TutorialStep step)
        {
            return new AIFeedback
            {
                PerformanceScore = step.CompletionScore,
                StrengthAreas = analytics.IdentifyStrengths(step),
                ImprovementSuggestions = analytics.GenerateImprovementSuggestions(step),
                NextRecommendedTopics = curriculum.GetNextTopics(step.ConceptId),
                MotivationalMessage = GenerateMotivationalMessage(step.CompletionScore)
            };
        }
        
        private string GenerateMotivationalMessage(float score)
        {
            if (score >= 0.9f) return "🌟 Outstanding work! You're mastering this concept brilliantly!";
            if (score >= 0.7f) return "💪 Great progress! You're building solid understanding!";
            if (score >= 0.5f) return "📈 Good effort! Keep practicing to strengthen your skills!";
            return "🎯 Every expert was once a beginner. Keep going - you've got this!";
        }
        
        private void DisplayLearningRecommendation(LearningRecommendation recommendation)
        {
            // This would integrate with the holographic UI system
            var holoPanel = FindObjectOfType<HolographicPanel>();
            if (holoPanel != null)
            {
                holoPanel.DisplayRecommendation(recommendation);
            }
        }
        
        private void DisplayFeedback(AIFeedback feedback)
        {
            Debug.Log($"🎓 AI Feedback - Score: {feedback.PerformanceScore:F2}");
            Debug.Log($"💡 Suggestion: {feedback.ImprovementSuggestions.FirstOrDefault()}");
            Debug.Log($"🎯 {feedback.MotivationalMessage}");
        }
        
        // Public API for integration with other systems
        public float GetPlayerSkillLevel(string skillId)
        {
            return playerSkillLevels.GetValueOrDefault(skillId, 0f);
        }
        
        public List<string> GetRecommendedTopics()
        {
            return curriculum.GetTopRecommendations(playerSkillLevels, 5);
        }
        
        public LearningAnalytics GetAnalytics()
        {
            return analytics;
        }
    }
    
    // Supporting data structures
    [System.Serializable]
    public class LearningObjective
    {
        public string Id;
        public float CompletionTime;
        public float MasteryLevel;
    }
    
    [System.Serializable]
    public class TutorialStep
    {
        public string ConceptId;
        public float CompletionScore;
        public float DifficultyLevel;
        public List<string> Prerequisites;
    }
    
    [System.Serializable]
    public class AIFeedback
    {
        public float PerformanceScore;
        public List<string> StrengthAreas;
        public List<string> ImprovementSuggestions;
        public List<string> NextRecommendedTopics;
        public string MotivationalMessage;
    }
    
    [System.Serializable]
    public class LearningRecommendation
    {
        public string TopicId;
        public string Title;
        public string Description;
        public float RelevanceScore;
        public string Reason;
    }
}