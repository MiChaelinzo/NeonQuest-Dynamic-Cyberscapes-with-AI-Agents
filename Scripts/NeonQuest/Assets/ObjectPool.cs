using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeonQuest.Assets
{
    /// <summary>
    /// Generic object pool for GameObjects with automatic cleanup and memory management
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform poolParent;
        private readonly Queue<GameObject> availableObjects;
        private readonly HashSet<GameObject> activeObjects;
        private readonly int maxPoolSize;
        private readonly string poolName;

        public int AvailableCount => availableObjects.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;
        public string PoolName => poolName;
        public GameObject Prefab => prefab;

        public ObjectPool(GameObject prefabReference, int initialSize = 5, int maxSize = 50, Transform parent = null)
        {
            prefab = prefabReference;
            maxPoolSize = maxSize;
            poolName = prefabReference.name;
            
            availableObjects = new Queue<GameObject>();
            activeObjects = new HashSet<GameObject>();

            // Create pool parent if not provided
            if (parent == null)
            {
                var poolContainer = new GameObject($"Pool_{poolName}");
                poolContainer.SetActive(false);
                poolParent = poolContainer.transform;
            }
            else
            {
                poolParent = parent;
            }

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one if none available
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject obj;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            // Configure the object
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.SetParent(parent);
            obj.SetActive(true);

            activeObjects.Add(obj);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool for reuse
        /// </summary>
        public bool Return(GameObject obj)
        {
            if (obj == null || !activeObjects.Contains(obj))
            {
                return false;
            }

            activeObjects.Remove(obj);
            
            // Reset object state
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            // Reset any poolable components
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();

            availableObjects.Enqueue(obj);
            return true;
        }

        /// <summary>
        /// Creates a new object for the pool
        /// </summary>
        private GameObject CreateNewObject()
        {
            var obj = Object.Instantiate(prefab, poolParent);
            obj.SetActive(false);
            
            // Add pool tracking component if not present
            if (obj.GetComponent<PooledObject>() == null)
            {
                var pooledComponent = obj.AddComponent<PooledObject>();
                pooledComponent.Initialize(this);
            }

            return obj;
        }

        /// <summary>
        /// Performs cleanup by removing excess objects from the pool
        /// </summary>
        public int Cleanup(int targetSize = -1)
        {
            if (targetSize < 0)
            {
                targetSize = Mathf.Max(5, maxPoolSize / 4); // Keep 25% or minimum 5
            }

            int removedCount = 0;
            while (availableObjects.Count > targetSize)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                    removedCount++;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Gets memory usage estimate for this pool in bytes
        /// </summary>
        public long GetMemoryUsage()
        {
            if (prefab == null) return 0;

            // Rough estimate based on mesh and texture data
            long memoryPerObject = 0;
            
            var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter?.sharedMesh != null)
                {
                    memoryPerObject += meshFilter.sharedMesh.GetMemorySize();
                }
            }

            return memoryPerObject * TotalCount;
        }

        /// <summary>
        /// Destroys all objects in the pool
        /// </summary>
        public void Destroy()
        {
            // Return all active objects first
            var activeList = activeObjects.ToList();
            foreach (var obj in activeList)
            {
                Return(obj);
            }

            // Destroy all available objects
            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            // Destroy pool parent if we created it
            if (poolParent != null && poolParent.name.StartsWith("Pool_"))
            {
                Object.Destroy(poolParent.gameObject);
            }
        }
    }

    /// <summary>
    /// Interface for objects that need special handling when returned to pool
    /// </summary>
    public interface IPoolable
    {
        void OnReturnToPool();
    }

    /// <summary>
    /// Component attached to pooled objects to track their pool association
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private ObjectPool parentPool;

        public ObjectPool ParentPool => parentPool;

        public void Initialize(ObjectPool pool)
        {
            parentPool = pool;
        }

        /// <summary>
        /// Convenience method to return this object to its pool
        /// </summary>
        public void ReturnToPool()
        {
            parentPool?.Return(gameObject);
        }
    }
}