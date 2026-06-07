using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class FlexibleGridLayout : LayoutGroup
    {
        public event Action LayoutUpdated;
        [SerializeField]
        private int _rows;

        [SerializeField]
        private int _columns;

        [SerializeField]
        private Vector2 _spacing;

        public int Rows
        {
            get => _rows;
            set
            {
                _rows = value;
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }
        
        public int Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }
        
        public Vector2 Spacing
        {
            get => _spacing;
            set
            {
                _spacing = value;
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            if (transform.childCount == 0) 
                return;

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            float totalSpacingX = _spacing.x * (_columns - 1) + padding.left + padding.right;
            float totalSpacingY = _spacing.y * (_rows - 1) + padding.top + padding.bottom;

            float cellWidth = (parentWidth - totalSpacingX) / _columns;
            float cellHeight = (parentHeight - totalSpacingY) / _rows;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int rowCount = i / _columns;
                int columnCount = i % _columns;

                var item = rectChildren[i];

                float xPos = padding.left + (cellWidth * columnCount) + (_spacing.x * columnCount);
                float yPos = padding.top + (cellHeight * rowCount) + (_spacing.y * rowCount);

                SetChildAlongAxis(item, 0, xPos, cellWidth);
                SetChildAlongAxis(item, 1, yPos, cellHeight);
            }

            LayoutUpdated?.Invoke();
        }

        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }

        public bool TryGetCellCoordinates(Vector2 localPoint, out int x, out int y)
        {
            x = -1;
            y = -1;

            if (_columns <= 0 || _rows <= 0)
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            float xFromLeft = localPoint.x + rect.width * rectTransform.pivot.x;
            float yFromTop = rect.height * (1f - rectTransform.pivot.y) - localPoint.y;

            float totalSpacingX = _spacing.x * (_columns - 1) + padding.left + padding.right;
            float totalSpacingY = _spacing.y * (_rows - 1) + padding.top + padding.bottom;
            float cellWidth = (rect.width - totalSpacingX) / _columns;
            float cellHeight = (rect.height - totalSpacingY) / _rows;
            float strideX = cellWidth + _spacing.x;
            float strideY = cellHeight + _spacing.y;

            if (strideX <= 0f || strideY <= 0f)
            {
                return false;
            }

            x = Mathf.FloorToInt((xFromLeft - padding.left) / strideX);
            y = Mathf.FloorToInt((yFromTop - padding.top) / strideY);

            if (x < 0 || y < 0 || x >= _columns || y >= _rows)
            {
                return false;
            }

            float cellLocalX = xFromLeft - padding.left - x * strideX;
            float cellLocalY = yFromTop - padding.top - y * strideY;

            if (cellLocalX < 0f || cellLocalY < 0f || cellLocalX > cellWidth || cellLocalY > cellHeight)
            {
                return false;
            }

            return true;
        }

        public bool TryGetCellCenterLocal(Vector2Int cell, out Vector2 localPoint) =>
            TryGetCellCenterLocal(cell.x, cell.y, out localPoint);

        public bool TryGetCellCenterLocal(int x, int y, out Vector2 localPoint)
        {
            localPoint = default;

            if (!TryGetCellRectLocal(x, y, out Rect cellRect))
            {
                return false;
            }

            localPoint = cellRect.center;
            return true;
        }

        public bool TryGetCellRectLocal(Vector2Int cell, out Rect localRect) =>
            TryGetCellRectLocal(cell.x, cell.y, out localRect);

        public bool TryGetCellRectLocal(int x, int y, out Rect localRect)
        {
            localRect = default;

            if (x < 0 || y < 0 || x >= _columns || y >= _rows)
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return false;
            }

            float totalSpacingX = _spacing.x * (_columns - 1) + padding.left + padding.right;
            float totalSpacingY = _spacing.y * (_rows - 1) + padding.top + padding.bottom;
            float cellWidth = (rect.width - totalSpacingX) / _columns;
            float cellHeight = (rect.height - totalSpacingY) / _rows;

            if (cellWidth <= 0f || cellHeight <= 0f)
            {
                return false;
            }

            float minFromLeft = padding.left + x * (cellWidth + _spacing.x);
            float maxFromLeft = minFromLeft + cellWidth;
            float minFromTop = padding.top + y * (cellHeight + _spacing.y);
            float maxFromTop = minFromTop + cellHeight;

            float minX = minFromLeft - rect.width * rectTransform.pivot.x;
            float maxX = maxFromLeft - rect.width * rectTransform.pivot.x;
            float maxY = rect.height * (1f - rectTransform.pivot.y) - minFromTop;
            float minY = rect.height * (1f - rectTransform.pivot.y) - maxFromTop;
            localRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return localRect.width > 0f && localRect.height > 0f;
        }
    }
}
