using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.UI.Vfx;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    internal sealed class BoardViewAnimations
    {
        private const float TilePopDuration = 0.2f;
        private const float SwapSecondsPerTile = 0.08f;
        private const float BombPopDuration = 0.28f;
        private const float TileMoveDuration = 0.3f;
        private const float SpotEnterDuration = 0.6f;
        private const float TileEnterDuration = 0.5f;
        private const float EnterStagger = 0.05f;
        private const float TileEnterDelay = 0.1f;
        private const float BoardEnterGemSlideVolume = 0.35f;
        private const float BoardEnterGemSlideMinInterval = 0.12f;

        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TilePool _tilePool;
        private int _width;
        private GameObject _linkTarget;
        private Func<int, float> _getTargetScale;
        private BoardVfxPlayer _vfxPlayer;

        public void Bind(
            GameObject[] tiles,
            TileSpotView[] tileSpots,
            TilePool tilePool,
            int width,
            GameObject linkTarget,
            Func<int, float> getTargetScale,
            BoardVfxPlayer vfxPlayer)
        {
            _tiles = tiles;
            _tileSpots = tileSpots;
            _tilePool = tilePool;
            _width = width;
            _linkTarget = linkTarget;
            _getTargetScale = getTargetScale;
            _vfxPlayer = vfxPlayer;
        }

        public void ClearBindings()
        {
            _tiles = null;
            _tileSpots = null;
            _tilePool = null;
            _width = 0;
            _linkTarget = null;
            _getTargetScale = null;
            _vfxPlayer = null;
        }

        public Tween CreateTile(List<AddedTileInfo> addedTiles)
        {
            if (addedTiles == null || addedTiles.Count == 0)
            {
                return LinkSequence(DOTween.Sequence().AppendInterval(0.01f));
            }

            float popDuration = SettingsService.ScaleDuration(TilePopDuration);
            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() => RunCreateTiles(addedTiles, popDuration));
            sequence.AppendInterval(popDuration);
            return LinkSequence(sequence);
        }

        public Tween PlayMatchAndDestroyPhases(BoardSequence boardSequence, Action playMatchSound = null)
        {
            if (boardSequence?.MatchedPosition == null || boardSequence.MatchedPosition.Count == 0)
            {
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            bool isSpecialMatch = MatchRunAnalysis.HasSpecialBonusMatch(boardSequence);
            float popDuration = SettingsService.ScaleDuration(TilePopDuration);
            float matchDuration = _vfxPlayer != null
                ? _vfxPlayer.GetMatchStepDuration(boardSequence)
                : SettingsService.ScaleDuration(SwapSecondsPerTile * 3f);
            bool hasMatchedBombs = boardSequence.MatchedBombs != null && boardSequence.MatchedBombs.Count > 0;
            float bombDuration = hasMatchedBombs ? SettingsService.ScaleDuration(BombPopDuration) : 0f;
            float phaseDuration = Mathf.Max(popDuration, matchDuration, bombDuration);

            Sequence step = DOTween.Sequence();
            step.AppendCallback(() => playMatchSound?.Invoke());

            Sequence popPhase = DOTween.Sequence();
            popPhase.AppendCallback(() =>
            {
                RunDestroyMatchedCells(
                    boardSequence.MatchedPosition,
                    boardSequence.MatchedBombs,
                    isSpecialMatch,
                    popDuration,
                    bombDuration);
                _vfxPlayer?.PlayMatchStep(boardSequence);
            });
            popPhase.AppendInterval(phaseDuration);
            step.Append(popPhase);
            return LinkSequence(step);
        }

        public Tween DestroyTiles(BoardSequence boardSequence)
        {
            if (boardSequence?.MatchedPosition == null || boardSequence.MatchedPosition.Count == 0)
            {
                return LinkSequence(DOTween.Sequence().AppendInterval(0.01f));
            }

            bool isSpecialMatch = MatchRunAnalysis.HasSpecialBonusMatch(boardSequence);
            float popDuration = SettingsService.ScaleDuration(TilePopDuration);
            bool hasMatchedBombs = boardSequence.MatchedBombs != null && boardSequence.MatchedBombs.Count > 0;
            float bombDuration = hasMatchedBombs ? SettingsService.ScaleDuration(BombPopDuration) : 0f;
            float phaseDuration = Mathf.Max(popDuration, bombDuration);

            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() =>
            {
                RunDestroyMatchedCells(
                    boardSequence.MatchedPosition,
                    boardSequence.MatchedBombs,
                    isSpecialMatch,
                    popDuration,
                    bombDuration);
            });
            sequence.AppendInterval(phaseDuration);
            return LinkSequence(sequence);
        }

        private void RunDestroyMatchedCells(
            IReadOnlyList<Vector2Int> matchedCells,
            IReadOnlyList<Vector2Int> matchedBombs,
            bool isSpecialMatch,
            float popDuration,
            float bombDuration)
        {
            if (matchedCells == null || matchedCells.Count == 0)
            {
                return;
            }

            var processedCells = new HashSet<Vector2Int>();
            var bombCells = BuildBombCellSet(matchedBombs);

            foreach (Vector2Int position in matchedCells)
            {
                if (!processedCells.Add(position))
                {
                    continue;
                }

                int index = ToIndex(position.x, position.y);
                GameObject tile = _tiles[index];

                if (tile == null)
                {
                    continue;
                }

                _tiles[index] = null;
                tile.transform.DOKill(true);

                if (bombCells.Contains(position))
                {
                    PlayBombDestroyAt(tile, index, bombDuration > 0f ? bombDuration : popDuration);
                }
                else
                {
                    PlayRegularDestroyAt(position, tile, isSpecialMatch, popDuration);
                }
            }
        }

        private static HashSet<Vector2Int> BuildBombCellSet(IReadOnlyList<Vector2Int> matchedBombs)
        {
            if (matchedBombs == null || matchedBombs.Count == 0)
            {
                return new HashSet<Vector2Int>();
            }

            return new HashSet<Vector2Int>(matchedBombs);
        }

        private void PlayBombDestroyAt(GameObject tile, int index, float duration)
        {
            AudioService.PlaySfx(AudioKeys.GameplayExplosion);

            float targetScale = _getTargetScale != null ? _getTargetScale(index) : tile.transform.localScale.x;
            if (targetScale <= 0.01f)
            {
                targetScale = 1f;
            }

            RectTransform tileRect = tile.transform as RectTransform;

            if (_vfxPlayer == null || tileRect == null)
            {
                tile.transform
                    .DOScale(0f, duration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => _tilePool.ReleaseObject(tile));
                return;
            }

            _vfxPlayer.PlayBombExplosion(
                tileRect,
                targetScale,
                () => _tilePool.ReleaseObject(tile));
        }

        private void PlayRegularDestroyAt(
            Vector2Int position,
            GameObject tile,
            bool isSpecialMatch,
            float popDuration)
        {
            Sequence destroySequence = DOTween.Sequence();
            destroySequence.Join(
                tile.transform
                    .DOScale(0f, popDuration)
                    .SetEase(Ease.InBack));

            if (_vfxPlayer != null)
            {
                Color? tint = TryGetTileTint(tile);
                destroySequence.Join(_vfxPlayer.PlayTileRemove(position, tint, isSpecialMatch, popDuration));
            }

            destroySequence.OnComplete(() => _tilePool.ReleaseObject(tile));
        }

        private void RunCreateTiles(IReadOnlyList<AddedTileInfo> addedTiles, float popDuration)
        {
            foreach (AddedTileInfo addedTileInfo in addedTiles)
            {
                Vector2Int position = addedTileInfo.Position;
                int index = ToIndex(position.x, position.y);

                ReleaseTileAt(index);

                GameObject tile = _tilePool.GetObject(addedTileInfo.TypeId);
                _tileSpots[index].SetTile(tile);
                _tiles[index] = tile;

                tile.transform.localScale = Vector3.zero;
                tile.transform.DOScale(_getTargetScale(index), popDuration);
            }
        }

        public Tween MoveTiles(List<MovedTileInfo> movedTiles)
        {
            Sequence sequence = DOTween.Sequence();

            if (movedTiles == null || movedTiles.Count == 0)
            {
                sequence.AppendInterval(0.01f);
                return LinkSequence(sequence);
            }

            List<MovedTileInfo> orderedMoves = movedTiles.Count > 1
                ? SortMoves(movedTiles)
                : movedTiles;

            float moveDuration = SettingsService.ScaleDuration(TileMoveDuration);
            sequence.AppendCallback(() => RunMoveTiles(orderedMoves));
            sequence.AppendInterval(moveDuration);
            return LinkSequence(sequence);
        }

        private void RunMoveTiles(IReadOnlyList<MovedTileInfo> orderedMoves)
        {
            foreach (MovedTileInfo move in orderedMoves)
            {
                int fromIndex = ToIndex(move.From.x, move.From.y);
                int toIndex = ToIndex(move.To.x, move.To.y);

                GameObject tile = _tiles[fromIndex];
                if (tile == null)
                {
                    continue;
                }

                _tiles[fromIndex] = null;
                _tiles[toIndex] = tile;
                _tileSpots[toIndex].AnimatedSetTile(tile);
            }
        }

        public Tween PlayEnterAnimation()
        {
            if (_tileSpots == null || _tileSpots.Length == 0)
            {
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            Sequence sequence = DOTween.Sequence();
            float stagger = SettingsService.ScaleDuration(EnterStagger);
            float spotDuration = SettingsService.ScaleDuration(SpotEnterDuration);
            float tileDuration = SettingsService.ScaleDuration(TileEnterDuration);
            float tileDelay = SettingsService.ScaleDuration(TileEnterDelay);
            int centerX = (_width - 1) / 2;
            int centerY = (_tileSpots.Length / _width - 1) / 2;

            for (int i = 0; i < _tileSpots.Length; i++)
            {
                TileSpotView spot = _tileSpots[i];
                if (spot == null)
                {
                    continue;
                }

                Transform spotTransform = spot.transform;
                spotTransform.localScale = Vector3.zero;

                int x = i % _width;
                int y = i / _width;
                float delay = (Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY)) * stagger;

                sequence.Insert(
                    delay,
                    spotTransform.DOScale(1f, spotDuration).SetEase(Ease.OutBack));

                GameObject tile = _tiles[i];
                if (tile == null)
                {
                    continue;
                }

                float targetScale = _getTargetScale != null ? _getTargetScale(i) : 1f;
                tile.transform.localScale = Vector3.zero;
                float tileStartDelay = delay + tileDelay;
                sequence.InsertCallback(tileStartDelay, PlayBoardEnterGemSlideSound);
                sequence.Insert(
                    tileStartDelay,
                    tile.transform.DOScale(targetScale, tileDuration).SetEase(Ease.OutBack));
            }

            return LinkSequence(sequence);
        }

        public Tween SwapTiles(Vector2Int from, Vector2Int to)
        {
            int fromIndex = ToIndex(from);
            int toIndex = ToIndex(to);

            GameObject fromTile = _tiles[fromIndex];
            GameObject toTile = _tiles[toIndex];

            Sequence sequence = DOTween.Sequence();

            if (fromTile == null || toTile == null)
            {
                sequence.AppendInterval(0.01f);
                return LinkSequence(sequence);
            }

            sequence.Append(_tileSpots[fromIndex].AnimatedSetTile(toTile));
            sequence.Join(_tileSpots[toIndex].AnimatedSetTile(fromTile));

            (_tiles[toIndex], _tiles[fromIndex]) = (_tiles[fromIndex], _tiles[toIndex]);

            return LinkSequence(sequence);
        }

        private static Color? TryGetTileTint(GameObject tile)
        {
            if (tile != null && tile.TryGetComponent(out Image image))
            {
                return image.color;
            }

            return null;
        }

        private static void PlayBoardEnterGemSlideSound() =>
            AudioService.PlaySfxRateLimited(
                AudioKeys.GameplayGemSlide,
                BoardEnterGemSlideMinInterval,
                BoardEnterGemSlideVolume);

        private void ReleaseTileAt(int index)
        {
            GameObject tile = _tiles[index];
            if (tile == null)
            {
                return;
            }

            tile.transform.DOKill(true);
            _tilePool.ReleaseObject(tile);
            _tiles[index] = null;
        }

        private static List<MovedTileInfo> SortMoves(List<MovedTileInfo> moves)
        {
            var sorted = new List<MovedTileInfo>(moves);
            sorted.Sort(static (a, b) =>
            {
                int compareY = a.From.y.CompareTo(b.From.y);
                return compareY != 0 ? compareY : a.From.x.CompareTo(b.From.x);
            });
            return sorted;
        }

        private Sequence LinkSequence(Sequence sequence)
        {
            if (sequence != null && _linkTarget != null)
            {
                sequence.SetLink(_linkTarget, LinkBehaviour.KillOnDestroy);
            }

            return sequence;
        }

        private int ToIndex(Vector2Int cell) => cell.y * _width + cell.x;

        private int ToIndex(int x, int y) => y * _width + x;
    }
}
