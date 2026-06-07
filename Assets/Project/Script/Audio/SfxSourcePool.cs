using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Misc;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Audio
{
    public sealed class SfxSourcePool : Pool<int, SfxSourceHandle>
    {
        private const int PoolKey = 0;

        private readonly List<SfxSourceHandle> _activeHandles = new();

        public int ActiveCount => _activeHandles.Count;

        public int TotalCount
        {
            get
            {
                Transform holder = transform.Find(BuildHolderName(PoolKey));
                return holder != null ? holder.childCount : 0;
            }
        }

        public void Configure(GameObject prefab, int prewarmCount)
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(SfxSourcePool)} requires an Sfx source prefab.", this);
                return;
            }

            _activeHandles.Clear();
            ClearAll();
            Register(PoolKey, prefab, Mathf.Max(0, prewarmCount));
        }

        public SfxSourceHandle Rent()
        {
            SfxSourceHandle handle = Get(PoolKey);
            if (!_activeHandles.Contains(handle))
            {
                _activeHandles.Add(handle);
            }

            return handle;
        }

        public void Return(SfxSourceHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            _activeHandles.Remove(handle);
            Release(handle);
        }

        public void ForEachActive(Action<SfxSourceHandle> action)
        {
            if (action == null)
            {
                return;
            }

            for (int i = _activeHandles.Count - 1; i >= 0; i--)
            {
                SfxSourceHandle handle = _activeHandles[i];
                if (handle == null)
                {
                    _activeHandles.RemoveAt(i);
                    continue;
                }

                if (handle.InUse)
                {
                    action(handle);
                }
            }
        }

        protected override void OnAcquire(SfxSourceHandle instance)
        {
            base.OnAcquire(instance);
            instance.ResetForPool();
        }

        protected override void OnRelease(SfxSourceHandle instance)
        {
            instance.ResetForPool();
            base.OnRelease(instance);
        }

        private static string BuildHolderName(int key) => $"Key_{key}";
    }
}
