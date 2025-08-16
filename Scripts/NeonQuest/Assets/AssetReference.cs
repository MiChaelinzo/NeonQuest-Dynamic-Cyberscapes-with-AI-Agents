using UnityEngine;
using System.Collections.Generic;

namespace NeonQuest.Assets
{
    /// <summary>
    /// Represents a reference to an asset with its metadata and variation points
    /// </summary>
    [System.Serializable]
    public class AssetReference
    {
        [SerializeField] private string assetPath;
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<VariationPoint> variations;
        [SerializeField] private Dictionary<string, object> properties;
        [SerializeField] private AssetIntegrityData integrityData;

        public string AssetPath 
        { 
            get => assetPath; 
            set => assetPath = value; 
        }

        public GameObject Prefab 
        { 
            get => prefab; 
            set => prefab = value; 
        }

        public List<VariationPoint> Variations 
        { 
            get => variations ?? (variations = new List<VariationPoint>()); 
            set => variations = value; 
        }

        public Dictionary<string, object> Properties 
        { 
            get => properties ?? (properties = new Dictionary<string, object>()); 
            set => properties = value; 
        }

        public AssetIntegrityData IntegrityData 
        { 
            get => integrityData ?? (integrityData = new AssetIntegrityData()); 
            set => integrityData = value; 
        }

        public AssetReference()
        {
            variations = new List<VariationPoint>();
            properties = new Dictionary<string, object>();
            integrityData = new AssetIntegrityData();
        }

        public AssetReference(string path, GameObject prefabReference) : this()
        {
            assetPath = path;
            prefab = prefabReference;
            
            if (prefabReference != null)
            {
                CaptureIntegrityData(prefabReference);
            }
        }

        /// <summary>
        /// Captures the original structure of the prefab for integrity validation
        /// </summary>
        private void CaptureIntegrityData(GameObject prefabReference)
        {
            integrityData.OriginalChildCount = prefabReference.transform.childCount;
            integrityData.OriginalComponentTypes = new List<string>();
            
            var components = prefabReference.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    integrityData.OriginalComponentTypes.Add(component.GetType().Name);
                }
            }

            // Capture child hierarchy
            integrityData.ChildHierarchy = new List<string>();
            CaptureChildHierarchy(prefabReference.transform, "", integrityData.ChildHierarchy);
        }

        private void CaptureChildHierarchy(Transform parent, string path, List<string> hierarchy)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string childPath = string.IsNullOrEmpty(path) ? child.name : $"{path}/{child.name}";
                hierarchy.Add(childPath);
                
                if (child.childCount > 0)
                {
                    CaptureChildHierarchy(child, childPath, hierarchy);
                }
            }
        }
    }

    /// <summary>
    /// Stores integrity data for validating asset structure preservation
    /// </summary>
    [System.Serializable]
    public class AssetIntegrityData
    {
        public int OriginalChildCount;
        public List<string> OriginalComponentTypes;
        public List<string> ChildHierarchy;

        public AssetIntegrityData()
        {
            OriginalComponentTypes = new List<string>();
            ChildHierarchy = new List<string>();
        }
    }

    /// <summary>
    /// Defines a point where procedural variations can be applied to an asset
    /// </summary>
    [System.Serializable]
    public class VariationPoint
    {
        [SerializeField] private string name;
        [SerializeField] private VariationType type;
        [SerializeField] private string targetPath; // Path to the component/object to modify
        [SerializeField] private Dictionary<string, object> parameters;

        public string Name 
        { 
            get => name; 
            set => name = value; 
        }

        public VariationType Type 
        { 
            get => type; 
            set => type = value; 
        }

        public string TargetPath 
        { 
            get => targetPath; 
            set => targetPath = value; 
        }

        public Dictionary<string, object> Parameters 
        { 
            get => parameters ?? (parameters = new Dictionary<string, object>()); 
            set => parameters = value; 
        }

        public VariationPoint()
        {
            parameters = new Dictionary<string, object>();
        }

        public VariationPoint(string variationName, VariationType variationType, string path) : this()
        {
            name = variationName;
            type = variationType;
            targetPath = path;
        }
    }

    /// <summary>
    /// Types of variations that can be applied to assets
    /// </summary>
    public enum VariationType
    {
        MaterialColor,
        LightIntensity,
        Scale,
        Rotation,
        Position,
        TextureSwap,
        ComponentToggle,
        AnimationSpeed,
        ParticleEffect
    }
}