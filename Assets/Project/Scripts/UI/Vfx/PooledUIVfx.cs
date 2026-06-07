using DG.Tweening;
using Gazeus.DesafioMatch3.Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Vfx
{
    [DisallowMultipleComponent]
    public sealed class PooledUIVfx : MonoBehaviour, IPoolKey<BoardVfxKind>
    {
        private const string SweepProgressProperty = "_SweepProgress";
        private const string DissolveProperty = "_Dissolve";
        private const string SweepAxisLengthProperty = "_SweepAxisLength";
        private const string CrossAxisLengthProperty = "_CrossAxisLength";
        private const string SweepVerticalProperty = "_SweepVertical";

        private static readonly int SweepProgressId = Shader.PropertyToID(SweepProgressProperty);
        private static readonly int DissolveId = Shader.PropertyToID(DissolveProperty);
        private static readonly int SweepAxisLengthId = Shader.PropertyToID(SweepAxisLengthProperty);
        private static readonly int CrossAxisLengthId = Shader.PropertyToID(CrossAxisLengthProperty);
        private static readonly int SweepVerticalId = Shader.PropertyToID(SweepVerticalProperty);

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Material _lineSweepSharedMaterial;

        [SerializeField]
        private ParticleSystem _particleSystem;

        private RectTransform _rectTransform;
        private Material _lineSweepInstance;
        private Sprite _defaultSprite;
        private Color _defaultImageColor = Color.white;
        private ParticleSystem.MinMaxGradient _defaultStartColor;
        private bool _defaultsCached;
        private bool _hasLineSweep;
        private bool _inUse;

        public BoardVfxKind PoolKey { get; set; }

        public bool InUse => _inUse;

        public bool HasImage
        {
            get
            {
                ResolveComponents();
                return _image != null;
            }
        }

        public bool HasParticles
        {
            get
            {
                ResolveComponents();
                return _particleSystem != null;
            }
        }

        public bool HasLineSweep
        {
            get
            {
                CachePrefabDefaults();
                return _hasLineSweep;
            }
        }

        public Image Image
        {
            get
            {
                ResolveComponents();
                return _image;
            }
        }

        public ParticleSystem ParticleSystem
        {
            get
            {
                ResolveComponents();
                return _particleSystem;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }

                return _rectTransform;
            }
        }

        public float SweepProgress
        {
            get => GetLineSweepFloat(SweepProgressId);
            set => SetLineSweepFloat(SweepProgressId, value);
        }

        public float SweepDissolve
        {
            get => GetLineSweepFloat(DissolveId);
            set => SetLineSweepFloat(DissolveId, value);
        }

        public void ResetLineSweepMaterial()
        {
            if (_lineSweepInstance == null)
            {
                return;
            }

            _lineSweepInstance.SetFloat(SweepProgressId, 0f);
            _lineSweepInstance.SetFloat(DissolveId, 0f);
            MarkLineSweepDirty();
        }

        public void ConfigureLineSweep(
            float axisLength,
            float crossLength,
            Color tint,
            bool vertical)
        {
            if (!_hasLineSweep || _image == null)
            {
                return;
            }

            EnsureLineSweepInstance();
            if (_lineSweepInstance == null)
            {
                return;
            }

            _image.color = tint;
            _lineSweepInstance.SetFloat(SweepProgressId, 0f);
            _lineSweepInstance.SetFloat(DissolveId, 0f);
            _lineSweepInstance.SetFloat(SweepVerticalId, vertical ? 1f : 0f);
            _lineSweepInstance.SetFloat(SweepAxisLengthId, axisLength);
            _lineSweepInstance.SetFloat(CrossAxisLengthId, crossLength);
            MarkLineSweepDirty();
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            RectTransform rect = RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = anchoredPosition;
        }

        public void SetParticleTint(Color tint)
        {
            foreach (ParticleSystem particleSystem in GetParticleSystems())
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = tint;
            }
        }

        public void PlayParticles()
        {
            ParticleSystem[] particleSystems = GetParticleSystems();
            if (particleSystems.Length == 0)
            {
                return;
            }

            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Clear(true);
                particleSystems[i].Play(true);
            }
        }

        public void ResetBurstImages()
        {
            Transform flash = transform.Find("Flash");
            Transform ring = transform.Find("Ring");
            ResetBurstImage(flash);
            ResetBurstImage(ring);
        }

        public float GetEstimatedDuration()
        {
            ParticleSystem[] particleSystems = GetParticleSystems();
            if (particleSystems.Length == 0)
            {
                return 0.24f;
            }

            float maxDuration = 0f;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                float lifetime = main.startLifetime.mode switch
                {
                    ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
                    ParticleSystemCurveMode.TwoCurves => main.startLifetime.curveMax.Evaluate(1f),
                    ParticleSystemCurveMode.Curve => main.startLifetime.curve.Evaluate(1f),
                    _ => main.startLifetime.constant
                };

                maxDuration = Mathf.Max(
                    maxDuration,
                    main.duration + lifetime + main.startDelay.constantMax);
            }

            return maxDuration;
        }

        public void ActivateForUse()
        {
            CachePrefabDefaults();
            _inUse = true;
            DOTween.Kill(gameObject, complete: false);

            if (HasParticles)
            {
                StopAllParticleSystems();
            }

            ResetBurstImages();
            gameObject.SetActive(true);
        }

        public void PrepareForPool()
        {
            CachePrefabDefaults();
            _inUse = false;
            DOTween.Kill(gameObject, complete: false);

            if (_hasLineSweep)
            {
                ResetLineSweepMaterial();
            }

            StopAllParticleSystems();
            ResetParticleColors();
            ResetBurstImages();

            RectTransform rect = RectTransform;
            if (rect != null)
            {
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            if (HasImage)
            {
                Image image = Image;
                image.color = _defaultImageColor;
                image.sprite = _defaultSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.raycastTarget = false;
                image.maskable = false;
            }
        }

        private float GetLineSweepFloat(int propertyId)
        {
            return _lineSweepInstance != null ? _lineSweepInstance.GetFloat(propertyId) : 0f;
        }

        private void SetLineSweepFloat(int propertyId, float value)
        {
            EnsureLineSweepInstance();
            if (_lineSweepInstance == null)
            {
                return;
            }

            _lineSweepInstance.SetFloat(propertyId, value);
            MarkLineSweepDirty();
        }

        private void EnsureLineSweepInstance()
        {
            if (_lineSweepInstance != null || !_hasLineSweep || _image == null)
            {
                return;
            }

            _lineSweepInstance = new Material(_lineSweepSharedMaterial);
            _image.material = _lineSweepInstance;
        }

        private void MarkLineSweepDirty()
        {
            if (_image != null)
            {
                _image.SetAllDirty();
            }
        }

        private ParticleSystem[] GetParticleSystems()
        {
            ResolveComponents();
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems.Length > 0)
            {
                return particleSystems;
            }

            return _particleSystem != null ? new[] { _particleSystem } : System.Array.Empty<ParticleSystem>();
        }

        private void StopAllParticleSystems()
        {
            ParticleSystem[] particleSystems = GetParticleSystems();
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void ResetParticleColors()
        {
            if (_particleSystem == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _particleSystem.main;
            main.startColor = _defaultStartColor;
        }

        private static void ResetBurstImage(Transform target)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rect = target as RectTransform;
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            Material material = image.material;
            if (material != null && material.HasProperty(DissolveProperty))
            {
                material.SetFloat(DissolveProperty, 0f);
            }
        }

        private void CachePrefabDefaults()
        {
            if (_defaultsCached)
            {
                return;
            }

            ResolveComponents();

            if (_image != null)
            {
                _hasLineSweep = _lineSweepSharedMaterial != null &&
                                _lineSweepSharedMaterial.HasProperty(SweepProgressProperty);

                _defaultSprite = _image.sprite;
                _defaultImageColor = _image.color;
            }

            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems.Length > 0)
            {
                ConfigureParticleForUi();
                _defaultStartColor = particleSystems[0].main.startColor;
            }
            else if (_particleSystem != null)
            {
                ConfigureParticleForUi();
                _defaultStartColor = _particleSystem.main.startColor;
            }

            _defaultsCached = true;
        }

        private void ConfigureParticleForUi()
        {
            ParticleSystem[] particleSystems = GetParticleSystems();
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                ParticleSystem.MainModule main = particleSystem.main;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Local;

                ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                renderer.sortingOrder = 10;
            }
        }

        private void ResolveComponents()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }

        private void OnDestroy()
        {
            if (_lineSweepInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_lineSweepInstance);
            }
            else
            {
                DestroyImmediate(_lineSweepInstance);
            }

            _lineSweepInstance = null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _rectTransform = transform as RectTransform;
            _image = GetComponent<Image>();
            _particleSystem = GetComponent<ParticleSystem>();

            if (_image != null)
            {
                _image.raycastTarget = false;
                _image.maskable = false;
            }
        }
#endif
    }
}
