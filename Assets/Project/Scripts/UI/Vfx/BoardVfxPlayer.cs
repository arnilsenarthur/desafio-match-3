using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Vfx
{
    public sealed class BoardVfxPlayer : MonoBehaviour
    {
        private const float SwapSecondsPerTile = 0.08f;
        private const float SwapThicknessScale = 1.15f;
        private const float SpecialSwapThicknessScale = 1.35f;
        private const float TilePopDuration = 0.24f;
        private const float TilePopParticleSpawnDelayRatio = 0.75f;
        private const float BombPopDuration = 0.28f;
        private const float BombExplosionVisualScale = 1.2f;
        private const string ExplosionDissolveProperty = "_Dissolve";

        [SerializeField]
        private Color _matchLineColor = new(1f, 0.92f, 0.35f, 1f);

        [SerializeField]
        private Color _removeBurstColor = new(1f, 0.95f, 0.75f, 1f);

        [SerializeField]
        private RectTransform _vfxRoot;

        [SerializeField]
        private BoardVfxPool _vfxPool;

        private FlexibleGridLayout _grid;
        private RectTransform _boardRect;
        private Canvas _vfxCanvas;
        private readonly List<Tween> _sweepsScratch = new();

        public void Configure(RectTransform boardRect, FlexibleGridLayout grid)
        {
            _boardRect = boardRect;
            _grid = grid;
            EnsureRoot();
        }

        public float GetMatchStepDuration(BoardSequence sequence)
        {
            return SettingsService.ScaleDuration(SwapSecondsPerTile * GetMaxLineSpan(sequence));
        }

        public Tween PlayMatchStep(BoardSequence sequence)
        {
            EnsureRoot();

            if (sequence == null || _grid == null)
            {
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            List<Tween> sweeps = _sweepsScratch;
            sweeps.Clear();
            float maxDuration = 0f;

            for (int i = 0; i < sequence.ClearedRows.Count; i++)
            {
                int spanCount = _grid.Columns;
                float duration = GetLineSweepDuration(spanCount);
                maxDuration = Mathf.Max(maxDuration, duration);
                sweeps.Add(PlayLineSweep(
                    horizontal: true,
                    fixedIndex: sequence.ClearedRows[i],
                    startIndex: 0,
                    endIndex: _grid.Columns - 1,
                    duration: duration,
                    tint: _matchLineColor,
                    isSpecial: true));
            }

            for (int i = 0; i < sequence.ClearedColumns.Count; i++)
            {
                int spanCount = _grid.Rows;
                float duration = GetLineSweepDuration(spanCount);
                maxDuration = Mathf.Max(maxDuration, duration);
                sweeps.Add(PlayLineSweep(
                    horizontal: false,
                    fixedIndex: sequence.ClearedColumns[i],
                    startIndex: 0,
                    endIndex: _grid.Rows - 1,
                    duration: duration,
                    tint: _matchLineColor,
                    isSpecial: true));
            }

            if (sequence.MatchedPosition != null)
            {
                foreach (MatchRun run in MatchRunAnalysis.FindRuns(sequence.MatchedPosition))
                {
                    if (run.Count < 3 ||
                        IsRunOnClearedLine(run, sequence.ClearedRows, sequence.ClearedColumns))
                    {
                        continue;
                    }

                    bool isSpecial = run.Count >= MatchRunAnalysis.SpecialLinearMatchMinimum;
                    float duration = GetLineSweepDuration(run.Count);
                    maxDuration = Mathf.Max(maxDuration, duration);
                    sweeps.Add(PlayLineSweep(
                        horizontal: run.Horizontal,
                        fixedIndex: run.FixedIndex,
                        startIndex: run.StartIndex,
                        endIndex: run.EndIndex,
                        duration: duration,
                        tint: _matchLineColor,
                        isSpecial: isSpecial));
                }
            }

            if (maxDuration <= 0f)
            {
                maxDuration = GetLineSweepDuration(3);
            }

            return BuildParallelSequence(sweeps, maxDuration);
        }

        public Tween PlayTileRemove(
            Vector2Int cell,
            Color? tint = null,
            bool isSpecialMatch = true,
            float destroyDuration = 0f)
        {
            if (!TryGetCellCenterInVfxSpace(cell, out Vector2 localCenter))
            {
                return DOTween.Sequence().AppendInterval(SettingsService.ScaleDuration(TilePopDuration));
            }

            BoardVfxKind kind = isSpecialMatch ? BoardVfxKind.SpecialTilePop : BoardVfxKind.TilePop;
            float duration = destroyDuration > 0f
                ? destroyDuration
                : SettingsService.ScaleDuration(TilePopDuration);
            float spawnDelay = duration * TilePopParticleSpawnDelayRatio;
            return PlayParticleEffect(kind, localCenter, ResolvePopColor(tint), duration, spawnDelay);
        }

        public Tween PlayBombExplosion(
            RectTransform tileRect,
            float restingScale,
            Action onRelease)
        {
            if (tileRect == null)
            {
                onRelease?.Invoke();
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            float popDuration = SettingsService.ScaleDuration(BombPopDuration);
            Vector3 baseScale = Vector3.one * restingScale;
            Vector2 localCenter = _vfxRoot.InverseTransformPoint(tileRect.TransformPoint(tileRect.rect.center));

            tileRect.localScale = baseScale;

            Sequence tileBurst = DOTween.Sequence();
            tileBurst.Append(
                tileRect.DOScale(baseScale * 1.35f, popDuration * 0.12f)
                    .SetEase(Ease.OutQuad));
            tileBurst.Append(
                tileRect.DOScale(Vector3.zero, popDuration * 0.38f)
                    .SetEase(Ease.InBack));

            PooledUiVfx vfx = Rent(BoardVfxKind.BombExplosion);
            vfx.SetAnchoredPosition(localCenter);
            vfx.RectTransform.localScale = Vector3.one * BombExplosionVisualScale;
            vfx.ResetBurstImages();
            vfx.PlayParticles();

            float burstDuration = popDuration * 0.92f;
            float particleDuration = Mathf.Max(
                burstDuration,
                SettingsService.ScaleDuration(vfx.GetEstimatedDuration()));

            Sequence explosion = DOTween.Sequence();
            explosion.Join(tileBurst);
            explosion.Join(PlayBombBurstImages(vfx, burstDuration));
            explosion.Join(DOTween.Sequence().AppendInterval(particleDuration));
            BindVfxTween(explosion, vfx);

            return explosion
                .OnComplete(() =>
                {
                    Return(vfx);
                    onRelease?.Invoke();
                });
        }

        private Sequence PlayBombBurstImages(PooledUiVfx vfx, float duration)
        {
            Sequence sequence = DOTween.Sequence();
            Transform root = vfx.transform;
            sequence.Join(AnimateBurstImage(
                root.Find("Flash"),
                startScale: 0.2f,
                peakScale: 1.35f,
                endScale: 0.75f,
                duration: duration * 0.42f,
                dissolveDelay: duration * 0.18f,
                dissolveDuration: duration * 0.35f));
            sequence.Join(AnimateBurstImage(
                root.Find("Ring"),
                startScale: 0.35f,
                peakScale: 2.15f,
                endScale: 2.35f,
                duration: duration,
                dissolveDelay: duration * 0.28f,
                dissolveDuration: duration * 0.55f));
            BindVfxTween(sequence, vfx);
            return sequence;
        }

        private static Sequence AnimateBurstImage(
            Transform target,
            float startScale,
            float peakScale,
            float endScale,
            float duration,
            float dissolveDelay,
            float dissolveDuration)
        {
            Sequence sequence = DOTween.Sequence();
            if (target == null)
            {
                sequence.AppendInterval(Mathf.Max(0.01f, duration));
                return sequence;
            }

            RectTransform rect = target as RectTransform;
            Image image = target.GetComponent<Image>();
            if (rect == null || image == null)
            {
                sequence.AppendInterval(Mathf.Max(0.01f, duration));
                return sequence;
            }

            Material material = image.material;
            rect.localScale = Vector3.one * startScale;
            if (material != null && material.HasProperty(ExplosionDissolveProperty))
            {
                material.SetFloat(ExplosionDissolveProperty, 0f);
            }

            float riseDuration = duration * 0.34f;
            float fallDuration = Mathf.Max(0.01f, duration - riseDuration);
            sequence.Append(rect.DOScale(Vector3.one * peakScale, riseDuration).SetEase(Ease.OutQuad));
            sequence.Append(rect.DOScale(Vector3.one * endScale, fallDuration).SetEase(Ease.OutQuad));

            if (material != null && material.HasProperty(ExplosionDissolveProperty))
            {
                sequence.Insert(
                    dissolveDelay,
                    DOTween.To(
                            () => material.GetFloat(ExplosionDissolveProperty),
                            value => material.SetFloat(ExplosionDissolveProperty, value),
                            1f,
                            dissolveDuration)
                        .SetEase(Ease.InQuad)
                        .SetTarget(material));
            }

            return sequence;
        }

        private Tween PlayParticleEffect(
            BoardVfxKind kind,
            Vector2 localCenter,
            Color tint,
            float fallbackDuration,
            float spawnDelay = 0f)
        {
            PooledUiVfx vfx = Rent(kind);
            vfx.SetAnchoredPosition(localCenter);
            vfx.SetParticleTint(tint);

            float particleDuration = SettingsService.ScaleDuration(vfx.GetEstimatedDuration());

            Sequence sequence = DOTween.Sequence();
            if (spawnDelay > 0f)
            {
                sequence.AppendInterval(spawnDelay);
            }

            sequence.AppendCallback(() => vfx.PlayParticles());
            sequence.AppendInterval(Mathf.Max(particleDuration, fallbackDuration - spawnDelay));
            BindVfxTween(sequence, vfx);
            return sequence.OnComplete(() => Return(vfx));
        }

        private readonly struct LineSweepLayout
        {
            public LineSweepLayout(Vector2 center, float length, float thickness, bool horizontal)
            {
                Center = center;
                Length = length;
                Thickness = thickness;
                Horizontal = horizontal;
            }

            public Vector2 Center { get; }
            public float Length { get; }
            public float Thickness { get; }
            public bool Horizontal { get; }
        }

        private Tween PlayLineSweep(
            bool horizontal,
            int fixedIndex,
            int startIndex,
            int endIndex,
            float duration,
            Color tint,
            bool isSpecial)
        {
            if (_grid == null ||
                !TryGetLineSweepLayout(
                    horizontal,
                    fixedIndex,
                    startIndex,
                    endIndex,
                    out LineSweepLayout layout))
            {
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            float thicknessScale = isSpecial ? SpecialSwapThicknessScale : SwapThicknessScale;
            float thickness = Mathf.Max(layout.Thickness * thicknessScale, 10f);
            BoardVfxKind kind = isSpecial ? BoardVfxKind.SpecialSwap : BoardVfxKind.Swap;
            return PlayMaterialLineSweep(layout, thickness, duration, tint, kind);
        }

        private Tween PlayMaterialLineSweep(
            LineSweepLayout layout,
            float thickness,
            float duration,
            Color tint,
            BoardVfxKind kind)
        {
            PooledUiVfx vfx = Rent(kind);
            vfx.ResetLineSweepMaterial();

            Image line = vfx.Image;
            if (line == null || !vfx.HasLineSweep)
            {
                Return(vfx);
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            vfx.ConfigureLineSweep(layout.Length, thickness, tint, vertical: !layout.Horizontal);

            RectTransform rect = line.rectTransform;
            PlaceLineSweepRect(rect, layout, thickness);

            float sweepDuration = duration * 0.82f;
            float dissolveDuration = duration * 0.22f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(
                DOTween.To(
                        () => vfx.SweepProgress,
                        value => vfx.SweepProgress = value,
                        1f,
                        sweepDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(vfx));
            sequence.Append(
                DOTween.To(
                        () => vfx.SweepDissolve,
                        value => vfx.SweepDissolve = value,
                        1f,
                        dissolveDuration)
                    .SetEase(Ease.InQuad)
                    .SetTarget(vfx));
            sequence.OnComplete(() => Return(vfx));
            BindVfxTween(sequence, vfx);
            return sequence;
        }

        private static bool IsRunOnClearedLine(
            MatchRun run,
            IReadOnlyList<int> clearedRows,
            IReadOnlyList<int> clearedColumns)
        {
            if (run.Horizontal && clearedRows != null)
            {
                for (int i = 0; i < clearedRows.Count; i++)
                {
                    if (clearedRows[i] == run.FixedIndex)
                    {
                        return true;
                    }
                }
            }

            if (!run.Horizontal && clearedColumns != null)
            {
                for (int i = 0; i < clearedColumns.Count; i++)
                {
                    if (clearedColumns[i] == run.FixedIndex)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Color ResolvePopColor(Color? tint)
        {
            Color fallback = _removeBurstColor;
            fallback.a = 1f;

            if (!tint.HasValue)
            {
                return fallback;
            }

            Color sampled = tint.Value;
            if (sampled.r > 0.85f && sampled.g > 0.85f && sampled.b > 0.85f)
            {
                return fallback;
            }

            Color resolved = Color.Lerp(fallback, sampled, 0.35f);
            resolved.a = 1f;
            return resolved;
        }

        private static Tween BuildParallelSequence(IReadOnlyList<Tween> tweens, float fallbackDuration)
        {
            Sequence sequence = DOTween.Sequence();
            bool started = false;

            for (int i = 0; i < tweens.Count; i++)
            {
                Tween tween = tweens[i];
                if (tween == null)
                {
                    continue;
                }

                if (!started)
                {
                    sequence.Append(tween);
                    started = true;
                    continue;
                }

                sequence.Join(tween);
            }

            if (!started)
            {
                sequence.AppendInterval(Mathf.Max(0.01f, fallbackDuration));
            }

            return sequence;
        }

        private static void BindVfxTween(Tween tween, PooledUiVfx vfx)
        {
            if (tween == null || vfx == null)
            {
                return;
            }

            tween.SetTarget(vfx.gameObject);
        }

        private PooledUiVfx Rent(BoardVfxKind kind)
        {
            EnsurePool();
            return _vfxPool.Rent(kind, _vfxRoot);
        }

        private void Return(PooledUiVfx vfx)
        {
            if (_vfxPool == null || vfx == null)
            {
                return;
            }

            _vfxPool.Return(vfx);
        }

        private void EnsurePool()
        {
            EnsureRoot();

            if (_vfxPool == null)
            {
                Debug.LogError(
                    $"{nameof(BoardVfxPlayer)} on '{name}' is missing {nameof(BoardVfxPool)} reference.",
                    this);
            }
            else
            {
                _vfxPool.Initialize();
            }
        }

        private static float GetLineSweepDuration(int spanCount) =>
            SettingsService.ScaleDuration(SwapSecondsPerTile * Mathf.Max(1, spanCount));

        private int GetMaxLineSpan(BoardSequence sequence)
        {
            int maxSpan = 3;

            if (_grid != null)
            {
                for (int i = 0; i < sequence.ClearedRows.Count; i++)
                {
                    maxSpan = Mathf.Max(maxSpan, _grid.Columns);
                }

                for (int i = 0; i < sequence.ClearedColumns.Count; i++)
                {
                    maxSpan = Mathf.Max(maxSpan, _grid.Rows);
                }
            }

            if (sequence.MatchedPosition != null)
            {
                foreach (MatchRun run in MatchRunAnalysis.FindRuns(sequence.MatchedPosition))
                {
                    if (run.Count < 3 ||
                        IsRunOnClearedLine(run, sequence.ClearedRows, sequence.ClearedColumns))
                    {
                        continue;
                    }

                    maxSpan = Mathf.Max(maxSpan, run.Count);
                }
            }

            return maxSpan;
        }

        private static void PlaceLineSweepRect(RectTransform rect, LineSweepLayout layout, float thickness)
        {
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = layout.Center;
            rect.localRotation = Quaternion.identity;

            if (layout.Horizontal)
            {
                rect.sizeDelta = new Vector2(layout.Length, thickness);
                return;
            }

            rect.sizeDelta = new Vector2(thickness, layout.Length);
        }

        private bool TryGetLineSweepLayout(
            bool horizontal,
            int fixedIndex,
            int startIndex,
            int endIndex,
            out LineSweepLayout layout)
        {
            layout = default;

            if (_grid == null)
            {
                return false;
            }

            int min = Mathf.Min(startIndex, endIndex);
            int max = Mathf.Max(startIndex, endIndex);

            Vector2Int startCell = horizontal ? new Vector2Int(min, fixedIndex) : new Vector2Int(fixedIndex, min);
            Vector2Int endCell = horizontal ? new Vector2Int(max, fixedIndex) : new Vector2Int(fixedIndex, max);

            if (!TryGetCellRectInVfxSpace(startCell, out Rect startRect) ||
                !TryGetCellRectInVfxSpace(endCell, out Rect endRect))
            {
                return false;
            }

            float minX = Mathf.Min(startRect.xMin, endRect.xMin);
            float maxX = Mathf.Max(startRect.xMax, endRect.xMax);
            float minY = Mathf.Min(startRect.yMin, endRect.yMin);
            float maxY = Mathf.Max(startRect.yMax, endRect.yMax);

            float length = horizontal ? maxX - minX : maxY - minY;
            float thickness = horizontal ? startRect.height : startRect.width;
            if (length <= 0f || thickness <= 0f)
            {
                return false;
            }

            layout = new LineSweepLayout(
                new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f),
                length,
                thickness,
                horizontal);
            return true;
        }

        private bool TryGetCellCenterInVfxSpace(Vector2Int cell, out Vector2 localCenter)
        {
            localCenter = default;
            EnsureRoot();

            if (_grid == null || _boardRect == null || !_grid.TryGetCellCenterLocal(cell, out Vector2 boardLocal))
            {
                return false;
            }

            Vector3 worldCenter = _boardRect.TransformPoint(boardLocal);
            localCenter = _vfxRoot.InverseTransformPoint(worldCenter);
            return true;
        }

        private bool TryGetCellRectInVfxSpace(Vector2Int cell, out Rect rect)
        {
            rect = default;

            if (_grid == null || _boardRect == null || !_grid.TryGetCellRectLocal(cell, out Rect boardRect))
            {
                return false;
            }

            Vector3 minWorld = _boardRect.TransformPoint(new Vector3(boardRect.xMin, boardRect.yMin, 0f));
            Vector3 maxWorld = _boardRect.TransformPoint(new Vector3(boardRect.xMax, boardRect.yMax, 0f));
            Vector2 minLocal = _vfxRoot.InverseTransformPoint(minWorld);
            Vector2 maxLocal = _vfxRoot.InverseTransformPoint(maxWorld);
            rect = Rect.MinMaxRect(
                Mathf.Min(minLocal.x, maxLocal.x),
                Mathf.Min(minLocal.y, maxLocal.y),
                Mathf.Max(minLocal.x, maxLocal.x),
                Mathf.Max(minLocal.y, maxLocal.y));
            return rect.width > 0f && rect.height > 0f;
        }

        private void EnsureRoot()
        {
            if (_vfxRoot == null)
            {
                Debug.LogError(
                    $"{nameof(BoardVfxPlayer)} on '{name}' is missing {nameof(_vfxRoot)} reference.",
                    this);
                return;
            }

            EnsureVfxCanvas();
            _vfxRoot.SetAsLastSibling();
        }

        private void EnsureVfxCanvas()
        {
            if (_vfxRoot == null)
            {
                return;
            }

            if (_vfxCanvas == null)
            {
                _vfxCanvas = _vfxRoot.GetComponent<Canvas>();
                if (_vfxCanvas == null)
                {
                    _vfxCanvas = _vfxRoot.gameObject.AddComponent<Canvas>();
                }
            }

            Camera camera = ResolveUiCamera();
            if (camera == null)
            {
                Debug.LogWarning(
                    $"{nameof(BoardVfxPlayer)} could not resolve a UI camera; VFX may not render.",
                    this);
                return;
            }

            _vfxCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _vfxCanvas.worldCamera = camera;
            _vfxCanvas.planeDistance = 100f;
            _vfxCanvas.overrideSorting = true;
            _vfxCanvas.sortingOrder = 5;
        }

        private Camera ResolveUiCamera()
        {
            Canvas rootCanvas = _vfxRoot.GetComponentInParent<Canvas>();
            if (rootCanvas != null &&
                rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                rootCanvas.worldCamera != null)
            {
                return rootCanvas.worldCamera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            Camera[] cameras = Camera.allCameras;
            return cameras.Length > 0 ? cameras[0] : null;
        }
    }
}
