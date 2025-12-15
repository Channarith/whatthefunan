using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Utils
{
    /// <summary>
    /// Generic object pool for reusing GameObjects.
    /// Improves performance by avoiding repeated instantiation/destruction.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        #region Singleton
        private static ObjectPool _instance;
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectPool");
                    _instance = go.AddComponent<ObjectPool>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        #endregion

        #region Pool Data
        [Serializable]
        public class PoolConfig
        {
            public string poolId;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool expandable = true;
        }

        [SerializeField] private List<PoolConfig> _poolConfigs = new List<PoolConfig>();

        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, PoolConfig> _configs = new Dictionary<string, PoolConfig>();
        private Dictionary<string, Transform> _poolParents = new Dictionary<string, Transform>();
        private Dictionary<GameObject, string> _instanceToPool = new Dictionary<GameObject, string>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var config in _poolConfigs)
            {
                CreatePool(config);
            }
        }
        #endregion

        #region Pool Management
        /// <summary>
        /// Create a new pool with the given configuration.
        /// </summary>
        public void CreatePool(PoolConfig config)
        {
            if (config.prefab == null)
            {
                Debug.LogError($"[ObjectPool] Cannot create pool '{config.poolId}': prefab is null");
                return;
            }

            if (_pools.ContainsKey(config.poolId))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{config.poolId}' already exists");
                return;
            }

            // Create pool parent for organization
            GameObject poolParent = new GameObject($"Pool_{config.poolId}");
            poolParent.transform.SetParent(transform);
            _poolParents[config.poolId] = poolParent.transform;

            // Create the pool
            Queue<GameObject> pool = new Queue<GameObject>();
            _pools[config.poolId] = pool;
            _configs[config.poolId] = config;

            // Pre-instantiate objects
            for (int i = 0; i < config.initialSize; i++)
            {
                CreatePooledObject(config.poolId);
            }

            Debug.Log($"[ObjectPool] Created pool '{config.poolId}' with {config.initialSize} objects");
        }

        /// <summary>
        /// Create a pool at runtime with just a prefab.
        /// </summary>
        public void CreatePool(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            CreatePool(new PoolConfig
            {
                poolId = poolId,
                prefab = prefab,
                initialSize = initialSize,
                maxSize = maxSize,
                expandable = true
            });
        }

        private GameObject CreatePooledObject(string poolId)
        {
            if (!_configs.TryGetValue(poolId, out PoolConfig config))
            {
                Debug.LogError($"[ObjectPool] Config not found for pool '{poolId}'");
                return null;
            }

            GameObject obj = Instantiate(config.prefab, _poolParents[poolId]);
            obj.SetActive(false);
            _pools[poolId].Enqueue(obj);
            _instanceToPool[obj] = poolId;

            // Add pooled object component for callbacks
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            pooledObj.PoolId = poolId;

            return obj;
        }
        #endregion

        #region Get/Return Objects
        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public GameObject Get(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out Queue<GameObject> pool))
            {
                Debug.LogError($"[ObjectPool] Pool '{poolId}' does not exist");
                return null;
            }

            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                var config = _configs[poolId];
                if (config.expandable)
                {
                    obj = CreatePooledObject(poolId);
                    pool.Dequeue(); // Remove the one we just added
                }
                else
                {
                    Debug.LogWarning($"[ObjectPool] Pool '{poolId}' is empty and not expandable");
                    return null;
                }
            }

            obj.SetActive(true);
            
            // Notify the object it's been spawned
            var pooledObj = obj.GetComponent<PooledObject>();
            pooledObj?.OnSpawned();

            return obj;
        }

        /// <summary>
        /// Get an object from the pool and set its position/rotation.
        /// </summary>
        public GameObject Get(string poolId, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get(poolId);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Get an object from the pool and set its parent.
        /// </summary>
        public GameObject Get(string poolId, Transform parent)
        {
            GameObject obj = Get(poolId);
            if (obj != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
            }
            return obj;
        }

        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!_instanceToPool.TryGetValue(obj, out string poolId))
            {
                Debug.LogWarning($"[ObjectPool] Object '{obj.name}' is not from any pool. Destroying.");
                Destroy(obj);
                return;
            }

            // Notify the object it's being despawned
            var pooledObj = obj.GetComponent<PooledObject>();
            pooledObj?.OnDespawned();

            obj.SetActive(false);
            obj.transform.SetParent(_poolParents[poolId]);
            
            _pools[poolId].Enqueue(obj);
        }

        /// <summary>
        /// Return an object to its pool after a delay.
        /// </summary>
        public void ReturnAfterDelay(GameObject obj, float delay)
        {
            if (obj == null) return;
            StartCoroutine(ReturnAfterDelayCoroutine(obj, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelayCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(obj);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Get the count of available objects in a pool.
        /// </summary>
        public int GetAvailableCount(string poolId)
        {
            if (_pools.TryGetValue(poolId, out Queue<GameObject> pool))
            {
                return pool.Count;
            }
            return 0;
        }

        /// <summary>
        /// Clear all objects from a pool.
        /// </summary>
        public void ClearPool(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out Queue<GameObject> pool))
            {
                return;
            }

            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                _instanceToPool.Remove(obj);
                Destroy(obj);
            }
        }

        /// <summary>
        /// Destroy a pool completely.
        /// </summary>
        public void DestroyPool(string poolId)
        {
            ClearPool(poolId);
            _pools.Remove(poolId);
            _configs.Remove(poolId);

            if (_poolParents.TryGetValue(poolId, out Transform parent))
            {
                Destroy(parent.gameObject);
                _poolParents.Remove(poolId);
            }
        }

        /// <summary>
        /// Pre-warm a pool by expanding it to a certain size.
        /// </summary>
        public void PreWarm(string poolId, int count)
        {
            if (!_pools.TryGetValue(poolId, out Queue<GameObject> pool))
            {
                return;
            }

            int toCreate = count - pool.Count;
            for (int i = 0; i < toCreate; i++)
            {
                CreatePooledObject(poolId);
            }
        }
        #endregion
    }

    /// <summary>
    /// Component attached to pooled objects for lifecycle callbacks.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolId { get; set; }

        public event Action OnSpawnedEvent;
        public event Action OnDespawnedEvent;

        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        public virtual void OnSpawned()
        {
            OnSpawnedEvent?.Invoke();
        }

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public virtual void OnDespawned()
        {
            OnDespawnedEvent?.Invoke();
        }

        /// <summary>
        /// Return this object to its pool.
        /// </summary>
        public void ReturnToPool()
        {
            ObjectPool.Instance.Return(gameObject);
        }

        /// <summary>
        /// Return this object to its pool after a delay.
        /// </summary>
        public void ReturnToPoolAfterDelay(float delay)
        {
            ObjectPool.Instance.ReturnAfterDelay(gameObject, delay);
        }
    }
}

