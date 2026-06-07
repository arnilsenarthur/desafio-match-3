using Gazeus.DesafioMatch3.Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Vfx
{
    public sealed class BoardVfxPool : Pool<BoardVfxKind, PooledUiVfx>
    {
        [SerializeField]
        private int _swapPrewarm = 6;

        [SerializeField]
        private int _specialSwapPrewarm = 4;

        [SerializeField]
        private int _tilePopPrewarm = 12;

        [SerializeField]
        private int _specialTilePopPrewarm = 8;

        [SerializeField]
        private int _bombExplosionPrewarm = 4;

        private bool _initialized;

        public bool IsInitialized => _initialized;

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (!HasPrefabs)
            {
                Debug.LogError(
                    $"{nameof(BoardVfxPool)} on '{name}' has no VFX prefabs assigned.",
                    this);
                return;
            }

            TryRegister(BoardVfxKind.Swap, _swapPrewarm);
            TryRegister(BoardVfxKind.SpecialSwap, _specialSwapPrewarm);
            TryRegister(BoardVfxKind.TilePop, _tilePopPrewarm);
            TryRegister(BoardVfxKind.SpecialTilePop, _specialTilePopPrewarm);
            TryRegister(BoardVfxKind.BombExplosion, _bombExplosionPrewarm);
            _initialized = true;
        }

        public PooledUiVfx Rent(BoardVfxKind kind, Transform parent)
        {
            Initialize();
            PooledUiVfx instance = Get(kind);
            instance.transform.SetParent(parent, false);
            instance.gameObject.layer = parent.gameObject.layer;
            instance.transform.SetAsLastSibling();
            return instance;
        }

        public void Return(PooledUiVfx instance)
        {
            if (instance == null)
            {
                return;
            }

            Release(instance);
        }

        protected override void OnAcquire(PooledUiVfx instance)
        {
            base.OnAcquire(instance);
            instance.ActivateForUse();
        }

        protected override void OnRelease(PooledUiVfx instance)
        {
            instance.PrepareForPool();
            base.OnRelease(instance);
        }

        protected override bool IsAvailableForAcquire(PooledUiVfx instance) =>
            instance != null && !instance.InUse;

        private void TryRegister(BoardVfxKind kind, int prewarmCount)
        {
            if (!TryGetAssignedPrefab(kind, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"{nameof(BoardVfxPool)} is missing prefab for {kind}.", this);
                return;
            }

            ValidatePrefab(kind, prefab);
            Register(kind, prefab, Mathf.Max(0, prewarmCount));
        }

        private static void ValidatePrefab(BoardVfxKind kind, GameObject prefab)
        {
            if (prefab.GetComponent<PooledUiVfx>() == null)
            {
                Debug.LogError($"{prefab.name} is missing {nameof(PooledUiVfx)}.", prefab);
                return;
            }

            if (prefab.GetComponent<RectTransform>() == null)
            {
                Debug.LogError($"{prefab.name} must use a RectTransform root.", prefab);
            }

            switch (kind)
            {
                case BoardVfxKind.Swap:
                case BoardVfxKind.SpecialSwap:
                    PooledUiVfx pooled = prefab.GetComponent<PooledUiVfx>();
                    Image image = pooled != null ? pooled.Image : prefab.GetComponent<Image>();
                    if (image == null || pooled == null || !pooled.HasLineSweep)
                    {
                        Debug.LogError(
                            $"{prefab.name} requires an Image and a shared sweep material assigned on {nameof(PooledUiVfx)}.",
                            prefab);
                    }

                    break;

                case BoardVfxKind.TilePop:
                case BoardVfxKind.SpecialTilePop:
                case BoardVfxKind.BombExplosion:
                    ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
                    if (particleSystems.Length == 0)
                    {
                        Debug.LogError($"{prefab.name} requires at least one ParticleSystem.", prefab);
                    }

                    for (int i = 0; i < particleSystems.Length; i++)
                    {
                        ParticleSystemRenderer renderer = particleSystems[i].GetComponent<ParticleSystemRenderer>();
                        if (renderer == null || renderer.sharedMaterial == null)
                        {
                            Debug.LogError($"{prefab.name} requires particle render materials.", prefab);
                        }
                    }

                    break;
            }
        }
    }
}
