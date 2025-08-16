using System;
using System.Collections.Generic;

namespace NeonQuest.Configuration
{
    [Serializable]
    public class GenerationAction
    {
        public enum ActionType
        {
            GenerateLayout,
            AdjustLighting,
            ChangeFogDensity,
            ModifyAudio,
            TransitionAudioZone,
            TriggerEffect,
            CreateSpatialAudio
        }
        
        public ActionType Type { get; set; }
        public string Target { get; set; }
        public float Intensity { get; set; } = 1.0f;
        public float Duration { get; set; } = 0.0f;
        public Dictionary<string, object> Parameters { get; set; }
        
        public GenerationAction()
        {
            Parameters = new Dictionary<string, object>();
        }
        
        public GenerationAction(ActionType type, string target = null) : this()
        {
            Type = type;
            Target = target;
        }
        
        public bool IsValid()
        {
            return Parameters != null;
        }
        
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (Parameters.ContainsKey(key) && Parameters[key] is T)
            {
                return (T)Parameters[key];
            }
            return defaultValue;
        }
        
        public void SetParameter(string key, object value)
        {
            Parameters[key] = value;
        }
        
        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }
    }
}