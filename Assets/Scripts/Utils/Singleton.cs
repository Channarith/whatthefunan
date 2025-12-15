using UnityEngine;

namespace WhatTheFunan.Utils
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Ensures only one instance exists and persists across scene loads.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// Gets the singleton instance. Creates one if it doesn't exist.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on application quit.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[Singleton] {typeof(T)}";
                            
                            DontDestroyOnLoad(singletonObject);
                            
                            Debug.Log($"[Singleton] Created instance of {typeof(T)}");
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Returns true if an instance exists.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} destroyed.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when the singleton is first created. Override for initialization.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Singleton that doesn't persist across scenes.
    /// </summary>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[SceneSingleton] Duplicate instance of {typeof(T)} destroyed.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

