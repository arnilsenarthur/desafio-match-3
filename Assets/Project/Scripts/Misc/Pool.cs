using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Misc
{
    public class Pool<K, T> : MonoBehaviour where T : Component, IPoolKey<K>
    {
        [SerializeField]
        private SerializableDictionary<K, GameObject> _prefabs = new();

        private readonly Dictionary<K, Transform> _holders = new();

        public bool HasPrefabs => _prefabs.Count > 0;

        protected bool TryGetAssignedPrefab(K key, out GameObject prefab) =>
            _prefabs.TryGetValue(key, out prefab);

        public void Register(K key, GameObject prefab, int prewarmCount = 0)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefabs.Set(key, prefab);
            Transform holder = GetOrCreateHolder(key);
            ClearHolder(holder);

            for (int i = 0; i < prewarmCount; i++)
            {
                Release(CreateInstance(key, holder, prefab));
            }
        }

        public T Get(K key)
        {
            Transform holder = GetOrCreateHolder(key);
            if (!_prefabs.TryGetValue(key, out GameObject prefab) || prefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Pool<K, T>)} on '{name}' has no prefab registered for key '{key}'.");
            }

            T instance = TryTakeAvailableInstance(holder) ?? CreateInstance(key, holder, prefab);
            OnAcquire(instance);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            K key = instance.PoolKey;
            if (!_holders.TryGetValue(key, out Transform holder) || holder == null)
            {
                return;
            }

            OnRelease(instance);
            instance.transform.SetParent(holder, false);
            instance.gameObject.SetActive(false);
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<K, Transform> pair in _holders)
            {
                Transform holder = pair.Value;
                if (holder != null)
                {
                    ClearHolder(holder);
                    DestroyHolderObject(holder);
                }
            }

            _holders.Clear();
            _prefabs.Clear();
        }

        protected virtual void OnAcquire(T instance) => instance.gameObject.SetActive(true);

        protected virtual void OnRelease(T instance) { }

        protected virtual void OnInstanceCreated(K key, T instance) => instance.PoolKey = key;

        private void OnDestroy() => ClearAll();

        private Transform GetOrCreateHolder(K key)
        {
            if (_holders.TryGetValue(key, out Transform holder) && holder != null)
            {
                return holder;
            }

            GameObject holderObject = new GameObject(BuildHolderName(key));
            holderObject.transform.SetParent(transform, false);
            holder = holderObject.transform;
            _holders[key] = holder;
            return holder;
        }

        private static string BuildHolderName(K key) => $"Key_{key}";

        protected virtual T TryTakeAvailableInstance(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    continue;
                }

                T component = child.GetComponent<T>();
                if (component != null && IsAvailableForAcquire(component))
                {
                    return component;
                }
            }

            return null;
        }

        protected virtual bool IsAvailableForAcquire(T instance) => instance != null;

        private T CreateInstance(K key, Transform holder, GameObject prefab)
        {
            GameObject instanceObject = Instantiate(prefab, holder);
            instanceObject.SetActive(false);

            T component = instanceObject.GetComponent<T>();
            if (component == null)
            {
                component = instanceObject.AddComponent<T>();
            }

            OnInstanceCreated(key, component);
            return component;
        }

        private static void ClearHolder(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DestroyHolderObject(Transform holder)
        {
            if (holder == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(holder.gameObject);
            }
            else
            {
                DestroyImmediate(holder.gameObject);
            }
        }
    }
}
