using UnityEngine;

namespace NeonQuest.Configuration
{
    [CreateAssetMenu(fileName = "EnvironmentConfiguration", menuName = "NeonQuest/Environment Configuration", order = 1)]
    public class EnvironmentConfigurationAsset : ScriptableObject
    {
        [SerializeField]
        private EnvironmentConfiguration configuration;
        
        public EnvironmentConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new EnvironmentConfiguration();
                }
                return configuration;
            }
            set
            {
                configuration = value;
            }
        }
        
        private void OnValidate()
        {
            if (configuration != null)
            {
                var validationResult = ConfigurationValidator.ValidateConfiguration(configuration);
                if (!validationResult.IsValid)
                {
                    Debug.LogWarning($"Configuration validation failed for {name}:\n{ConfigurationValidator.FormatValidationResult(validationResult)}");
                }
            }
        }
        
        public void LoadFromYAML(string yamlPath)
        {
            var loader = new YAMLConfigLoader();
            configuration = loader.LoadConfiguration(yamlPath);
        }
        
        public bool IsValid()
        {
            return configuration != null && configuration.IsValid();
        }
    }
}