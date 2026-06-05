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

        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TileObjectPool _tilePool;
        private int _width;
        private GameObject _linkTarget;
        private Func<int, float> _getTargetScale;

        public void Bind(
            GameObject[] tiles,
            TileSpotView[] tileSpots,
            TileObjectPool tilePool,
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

                GameObject tile = _tilePool.Get(addedTileInfo.Type);
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

                sequence.Join(tile.transform.DOScale(0f, SettingsService.ScaleDuration(TilePopDuration))
                    .OnComplete(() => _tilePool.Release(tile)));
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
            foreach (MovedTileInfo movedTileInfo in movedTiles)
            {
                Vector2Int from = movedTileInfo.From;
                Vector2Int to = movedTileInfo.To;

                int fromIndex = ToIndex(from.x, from.y);
                int toIndex = ToIndex(to.x, to.y);

                GameObject tile = _tiles[fromIndex];
                sequence.Join(_tileSpots[toIndex].AnimatedSetTile(tile));

                _tiles[toIndex] = tile;
                _tiles[fromIndex] = null;
            }

            return LinkSequence(sequence);
        }

        public Tween SwapTiles(Vector2Int from, Vector2Int to)
        {
            int fromIndex = ToIndex(from);
            int toIndex = ToIndex(to);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_tileSpots[fromIndex].AnimatedSetTile(_tiles[toIndex]));
            sequence.Join(_tileSpots[toIndex].AnimatedSetTile(_tiles[fromIndex]));

            (_tiles[toIndex], _tiles[fromIndex]) = (_tiles[fromIndex], _tiles[toIndex]);

            return LinkSequence(sequence);
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
