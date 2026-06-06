using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    internal sealed class BoardViewAnimations
    {
        private const float TilePopDuration = 0.2f;
        private const float SpotEnterDuration = 0.6f;
        private const float TileEnterDuration = 0.5f;
        private const float EnterStagger = 0.05f;
        private const float TileEnterDelay = 0.1f;

        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TilePool _tilePool;
        private int _width;
        private GameObject _linkTarget;
        private Func<int, float> _getTargetScale;

        public void Bind(
            GameObject[] tiles,
            TileSpotView[] tileSpots,
            TilePool tilePool,
            int width,
            GameObject linkTarget,
            Func<int, float> getTargetScale)
        {
            _tiles = tiles;
            _tileSpots = tileSpots;
            _tilePool = tilePool;
            _width = width;
            _linkTarget = linkTarget;
            _getTargetScale = getTargetScale;
        }

        public void ClearBindings()
        {
            _tiles = null;
            _tileSpots = null;
            _tilePool = null;
            _width = 0;
            _linkTarget = null;
            _getTargetScale = null;
        }

        public Tween CreateTile(List<AddedTileInfo> addedTiles)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (AddedTileInfo addedTileInfo in addedTiles)
            {
                Vector2Int position = addedTileInfo.Position;
                int index = ToIndex(position.x, position.y);

                ReleaseTileAt(index);

                GameObject tile = _tilePool.GetObject(addedTileInfo.TypeId);
                _tileSpots[index].SetTile(tile);
                _tiles[index] = tile;

                tile.transform.localScale = Vector3.zero;
                sequence.Join(tile.transform.DOScale(
                    _getTargetScale(index),
                    SettingsService.ScaleDuration(TilePopDuration)));
            }

            return LinkSequence(sequence);
        }

        public Tween DestroyTiles(List<Vector2Int> matchedPosition)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (Vector2Int position in matchedPosition)
            {
                int index = ToIndex(position.x, position.y);
                GameObject tile = _tiles[index];
                _tiles[index] = null;

                if (tile == null)
                {
                    continue;
                }

                tile.transform.DOKill(true);
                sequence.Join(tile.transform.DOScale(0f, SettingsService.ScaleDuration(TilePopDuration))
                    .OnComplete(() => _tilePool.ReleaseObject(tile)));
            }

            if (matchedPosition.Count == 0)
            {
                sequence.AppendInterval(0.01f);
            }

            return LinkSequence(sequence);
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
                sequence.Join(_tileSpots[toIndex].AnimatedSetTile(tile));
            }

            return LinkSequence(sequence);
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
                sequence.Insert(
                    delay + tileDelay,
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
