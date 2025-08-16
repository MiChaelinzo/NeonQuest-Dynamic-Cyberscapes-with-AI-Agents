using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using NeonQuest.Assets;

namespace Tests.Assets
{
    /// <summary>
    /// Tests for asset integrity preservation and validation
    /// </summary>
    public class AssetIntegrityTests
    {
        private GameObject complexPrefab;
        private AssetReference assetReference;

        [SetUp]
        public void SetUp()
        {
            complexPrefab = CreateComplexTestPrefab();
            assetReference = new AssetReference("test/complex_prefab", complexPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            if (complexPrefab != null)
            {
                Object.DestroyImmediate(complexPrefab);
            }
        }

        private GameObject CreateComplexTestPrefab()
        {
            // Create a complex prefab with nested hierarchy
            var root = new GameObject("ComplexPrefab");
            
            // Add various components to root
            var rootRenderer = root.AddComponent<MeshRenderer>();
            var rootFilter = root.AddComponent<MeshFilter>();
            var rootCollider = root.AddComponent<BoxCollider>();
            var rootLight = root.AddComponent<Light>();
            rootLight.intensity = 2.0f;
            rootLight.color = Color.cyan;

            // Create nested child hierarchy
            var child1 = new GameObject("Child1");
            child1.transform.SetParent(root.transform);
            child1.AddComponent<MeshRenderer>();
            child1.AddComponent<MeshFilter>();
            
            var child1_1 = new GameObject("Child1_1");
            child1_1.transform.SetParent(child1.transform);
            child1_1.AddComponent<SphereCollider>();
            
            var child1_2 = new GameObject("Child1_2");
            child1_2.transform.SetParent(child1.transform);
            var child1_2_light = child1_2.AddComponent<Light>();
            child1_2_light.intensity = 0.5f;
            child1_2_light.color = Color.red;

            var child2 = new GameObject("Child2");
            child2.transform.SetParent(root.transform);
            child2.AddComponent<Rigidbody>();
            child2.AddComponent<CapsuleCollider>();

            var child2_1 = new GameObject("Child2_1");
            child2_1.transform.SetParent(child2.transform);
            child2_1.AddComponent<AudioSource>();

            return root;
        }

        [Test]
        public void AssetReference_CapturesCompleteHierarchy()
        {
            var integrityData = assetReference.IntegrityData;
            
            Assert.AreEqual(2, integrityData.OriginalChildCount); // Child1 and Child2
            Assert.IsTrue(integrityData.ChildHierarchy.Count > 0);
            
            // Verify specific hierarchy paths are captured
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child2"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1/Child1_1"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1/Child1_2"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child2/Child2_1"));
        }

        [Test]
        public void AssetReference_CapturesAllComponentTypes()
        {
            var integrityData = assetReference.IntegrityData;
            var componentTypes = integrityData.OriginalComponentTypes;
            
            Assert.IsTrue(componentTypes.Contains("Transform"));
            Assert.IsTrue(componentTypes.Contains("MeshRenderer"));
            Assert.IsTrue(componentTypes.Contains("MeshFilter"));
            Assert.IsTrue(componentTypes.Contains("BoxCollider"));
            Assert.IsTrue(componentTypes.Contains("Light"));
        }

        [Test]
        public void ValidateAssetIntegrity_WithUnmodifiedAsset_ReturnsTrue()
        {
            var instance = Object.Instantiate(complexPrefab);
            var testAssetRef = new AssetReference("test", complexPrefab);
            
            // Create a mock asset integrator to test validation
            var validator = new AssetIntegrityValidator();
            var isValid = validator.ValidateIntegrity(instance, testAssetRef.IntegrityData);
            
            Assert.IsTrue(isValid);
            
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ValidateAssetIntegrity_WithRemovedChild_ReturnsFalse()
        {
            var instance = Object.Instantiate(complexPrefab);
            var testAssetRef = new AssetReference("test", complexPrefab);
            
            // Remove a child to break integrity
            var child1 = instance.transform.Find("Child1");
            if (child1 != null)
            {
                Object.DestroyImmediate(child1.gameObject);
            }
            
            var validator = new AssetIntegrityValidator();
            var isValid = validator.ValidateIntegrity(instance, testAssetRef.IntegrityData);
            
            Assert.IsFalse(isValid);
            
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ValidateAssetIntegrity_WithRemovedComponent_ReturnsFalse()
        {
            var instance = Object.Instantiate(complexPrefab);
            var testAssetRef = new AssetReference("test", complexPrefab);
            
            // Remove a component to break integrity
            var light = instance.GetComponent<Light>();
            if (light != null)
            {
                Object.DestroyImmediate(light);
            }
            
            var validator = new AssetIntegrityValidator();
            var isValid = validator.ValidateIntegrity(instance, testAssetRef.IntegrityData);
            
            Assert.IsFalse(isValid);
            
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ValidateAssetIntegrity_WithModifiedHierarchy_ReturnsFalse()
        {
            var instance = Object.Instantiate(complexPrefab);
            var testAssetRef = new AssetReference("test", complexPrefab);
            
            // Move a child to break hierarchy
            var child1_1 = instance.transform.Find("Child1/Child1_1");
            var child2 = instance.transform.Find("Child2");
            
            if (child1_1 != null && child2 != null)
            {
                child1_1.SetParent(child2);
            }
            
            var validator = new AssetIntegrityValidator();
            var isValid = validator.ValidateIntegrity(instance, testAssetRef.IntegrityData);
            
            Assert.IsFalse(isValid);
            
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void VariationPoint_DetectsLightComponents()
        {
            var variations = assetReference.Variations;
            
            // Should have detected light intensity variations
            var lightVariations = variations.FindAll(v => v.Type == VariationType.LightIntensity);
            Assert.IsTrue(lightVariations.Count >= 1); // At least the root light
        }

        [Test]
        public void VariationPoint_DetectsRendererComponents()
        {
            var variations = assetReference.Variations;
            
            // Should have detected material color variations
            var colorVariations = variations.FindAll(v => v.Type == VariationType.MaterialColor);
            Assert.IsTrue(colorVariations.Count >= 1); // At least the root renderer
        }

        [Test]
        public void VariationPoint_HasCorrectTargetPaths()
        {
            var variations = assetReference.Variations;
            
            foreach (var variation in variations)
            {
                Assert.IsNotNull(variation.TargetPath);
                Assert.IsNotNull(variation.Name);
                
                // Verify target path format
                if (!string.IsNullOrEmpty(variation.TargetPath))
                {
                    Assert.IsFalse(variation.TargetPath.StartsWith("/"));
                    Assert.IsFalse(variation.TargetPath.EndsWith("/"));
                }
            }
        }

        [Test]
        public void AssetIntegrityData_PreservesTransformHierarchy()
        {
            var integrityData = assetReference.IntegrityData;
            
            // Verify that parent-child relationships are preserved in hierarchy data
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1/Child1_1"));
            Assert.IsTrue(integrityData.ChildHierarchy.Contains("Child1/Child1_2"));
            
            // Verify nested relationships
            var child1Children = integrityData.ChildHierarchy.FindAll(path => path.StartsWith("Child1/") && !path.Contains("/", path.IndexOf("/") + 1));
            Assert.AreEqual(2, child1Children.Count); // Child1_1 and Child1_2
        }

        [Test]
        public void AssetReference_HandlesEmptyPrefab()
        {
            var emptyPrefab = new GameObject("EmptyPrefab");
            var emptyAssetRef = new AssetReference("test/empty", emptyPrefab);
            
            Assert.AreEqual(0, emptyAssetRef.IntegrityData.OriginalChildCount);
            Assert.IsTrue(emptyAssetRef.IntegrityData.OriginalComponentTypes.Contains("Transform"));
            Assert.AreEqual(0, emptyAssetRef.IntegrityData.ChildHierarchy.Count);
            
            Object.DestroyImmediate(emptyPrefab);
        }

        [Test]
        public void AssetReference_HandlesNullPrefab()
        {
            var nullAssetRef = new AssetReference("test/null", null);
            
            Assert.IsNotNull(nullAssetRef.IntegrityData);
            Assert.AreEqual(0, nullAssetRef.IntegrityData.OriginalChildCount);
            Assert.AreEqual(0, nullAssetRef.IntegrityData.OriginalComponentTypes.Count);
        }

        [Test]
        public void VariationPoint_ParametersInitializeCorrectly()
        {
            var variation = new VariationPoint("TestVariation", VariationType.MaterialColor, "test/path");
            
            Assert.IsNotNull(variation.Parameters);
            Assert.AreEqual(0, variation.Parameters.Count);
            
            // Test parameter addition
            variation.Parameters["intensity"] = 1.5f;
            variation.Parameters["color"] = Color.blue;
            
            Assert.AreEqual(2, variation.Parameters.Count);
            Assert.AreEqual(1.5f, variation.Parameters["intensity"]);
            Assert.AreEqual(Color.blue, variation.Parameters["color"]);
        }
    }

    /// <summary>
    /// Helper class for testing asset integrity validation
    /// </summary>
    public class AssetIntegrityValidator
    {
        public bool ValidateIntegrity(GameObject instance, AssetIntegrityData integrityData)
        {
            if (instance == null || integrityData == null) return false;

            // Check child count
            if (instance.transform.childCount != integrityData.OriginalChildCount)
            {
                return false;
            }

            // Check component types
            var currentComponents = instance.GetComponents<Component>();
            var currentComponentTypes = new List<string>();
            
            foreach (var component in currentComponents)
            {
                if (component != null)
                {
                    currentComponentTypes.Add(component.GetType().Name);
                }
            }
            
            foreach (var expectedType in integrityData.OriginalComponentTypes)
            {
                if (!currentComponentTypes.Contains(expectedType))
                {
                    return false;
                }
            }

            // Check child hierarchy
            var currentHierarchy = new List<string>();
            CaptureChildHierarchy(instance.transform, "", currentHierarchy);
            
            if (currentHierarchy.Count != integrityData.ChildHierarchy.Count)
            {
                return false;
            }

            foreach (var expectedPath in integrityData.ChildHierarchy)
            {
                if (!currentHierarchy.Contains(expectedPath))
                {
                    return false;
                }
            }

            return true;
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
}