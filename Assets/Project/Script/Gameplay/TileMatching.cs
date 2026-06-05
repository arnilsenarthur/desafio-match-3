using System.Collections.Generic;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class TileMatching
    {
        public static bool FindAndMarkMatches(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            int cellCount = board.Width * board.Height;
            for (int i = 0; i < cellCount; i++)
            {
                matchedFlags[i] = false;
            }

            clearedRows.Clear();
            clearedColumns.Clear();

            ScanShapeRuns(board, tiles, matchedFlags, clearedRows, clearedColumns, horizontal: true);
            ScanShapeRuns(board, tiles, matchedFlags, clearedRows, clearedColumns, horizontal: false);
            ScanSkullRuns(board, tiles, matchedFlags, horizontal: true);
            ScanSkullRuns(board, tiles, matchedFlags, horizontal: false);

            ApplyLineClears(board, tiles, matchedFlags, clearedRows, clearedColumns);

            for (int i = 0; i < cellCount; i++)
            {
                if (matchedFlags[i])
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountSkullsInMatches(BoardState board, TileDefinitions tiles, bool[] matchedFlags)
        {
            int count = 0;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!matchedFlags[board.ToIndex(x, y)])
                    {
                        continue;
                    }

                    if (tiles.IsSkull(board.GetType(x, y)))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static bool CreatesMatchAt(BoardState board, TileDefinitions tiles, int x, int y)
        {
            if (tiles.IsEmpty(board.GetType(x, y)))
            {
                return false;
            }

            return MeasureShapeRunThroughCell(board, tiles, x, y, 1, 0) >= 3 ||
                   MeasureShapeRunThroughCell(board, tiles, x, y, 0, 1) >= 3 ||
                   GetSkullRunLengthAt(board, tiles, x, y, horizontal: true) >= 3 ||
                   GetSkullRunLengthAt(board, tiles, x, y, horizontal: false) >= 3;
        }

        public static void PropagateBombClears(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        if (!matchedFlags[board.ToIndex(x, y)])
                        {
                            continue;
                        }

                        if (!tiles.IsBomb(board.GetType(x, y)))
                        {
                            continue;
                        }

                        if (MarkEntireRow(board, tiles, matchedFlags, y))
                        {
                            changed = true;
                            AddUnique(clearedRows, y);
                        }

                        if (MarkEntireColumn(board, tiles, matchedFlags, x))
                        {
                            changed = true;
                            AddUnique(clearedColumns, x);
                        }
                    }
                }
            }
        }

        public static bool FitsInShapeRun(string cellType, string anchorShape, TileDefinitions tiles)
        {
            if (tiles.IsEmpty(cellType) || tiles.IsSkull(cellType))
            {
                return false;
            }

            if (IsWildcard(cellType, tiles))
            {
                return true;
            }

            if (!tiles.IsShape(cellType))
            {
                return false;
            }

            return string.IsNullOrEmpty(anchorShape) || cellType == anchorShape;
        }

        private static bool IsWildcard(string typeId, TileDefinitions tiles) =>
            tiles.IsJoker(typeId) || tiles.IsBomb(typeId);

        private static bool CanStartShapeRun(string typeId, TileDefinitions tiles) =>
            !tiles.IsEmpty(typeId) && !tiles.IsSkull(typeId) &&
            (tiles.IsShape(typeId) || IsWildcard(typeId, tiles));

        private static void ScanShapeRuns(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns,
            bool horizontal)
        {
            if (horizontal)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    ScanShapeLine(board, tiles, matchedFlags, clearedRows, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanShapeLine(board, tiles, matchedFlags, clearedColumns, x, axisX: false);
                }
            }
        }

        private static void ScanShapeLine(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            List<int> lineClears,
            int fixedCoord,
            bool axisX)
        {
            int lineLength = axisX ? board.Width : board.Height;
            int index = 0;

            while (index < lineLength)
            {
                int x = axisX ? index : fixedCoord;
                int y = axisX ? fixedCoord : index;
                string type = board.GetType(x, y);

                if (!CanStartShapeRun(type, tiles))
                {
                    index++;
                    continue;
                }

                int start = index;
                string anchorShape = null;
                int stepX = axisX ? 1 : 0;
                int stepY = axisX ? 0 : 1;

                if (!TryIncludeInShapeRun(type, ref anchorShape, tiles))
                {
                    index++;
                    continue;
                }

                index++;

                while (index < lineLength)
                {
                    int nx = axisX ? index : fixedCoord;
                    int ny = axisX ? fixedCoord : index;
                    string nextType = board.GetType(nx, ny);

                    if (!TryIncludeInShapeRun(nextType, ref anchorShape, tiles))
                    {
                        break;
                    }

                    index++;
                }

                int runLength = index - start;
                int startX = axisX ? start : fixedCoord;
                int startY = axisX ? fixedCoord : start;
                int endX = axisX ? index - 1 : fixedCoord;
                int endY = axisX ? fixedCoord : index - 1;

                if (runLength < 3 ||
                    !IsValidShapeRunSegment(board, tiles, startX, startY, endX, endY, stepX, stepY))
                {
                    index = start + 1;
                    continue;
                }

                for (int i = start; i < index; i++)
                {
                    int mx = axisX ? i : fixedCoord;
                    int my = axisX ? fixedCoord : i;
                    MarkMatch(board, matchedFlags, mx, my);
                }

                if (runLength >= 4)
                {
                    AddUnique(lineClears, fixedCoord);
                }
            }
        }

        private static void ScanSkullRuns(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            bool horizontal)
        {
            if (horizontal)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    ScanSkullLine(board, tiles, matchedFlags, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanSkullLine(board, tiles, matchedFlags, x, axisX: false);
                }
            }
        }

        private static void ScanSkullLine(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            int fixedCoord,
            bool axisX)
        {
            int lineLength = axisX ? board.Width : board.Height;
            int index = 0;

            while (index < lineLength)
            {
                int x = axisX ? index : fixedCoord;
                int y = axisX ? fixedCoord : index;

                if (!tiles.IsSkull(board.GetType(x, y)))
                {
                    index++;
                    continue;
                }

                int start = index;
                index++;

                while (index < lineLength)
                {
                    int nx = axisX ? index : fixedCoord;
                    int ny = axisX ? fixedCoord : index;

                    if (!tiles.IsSkull(board.GetType(nx, ny)))
                    {
                        break;
                    }

                    index++;
                }

                int runLength = index - start;
                if (runLength < 3)
                {
                    continue;
                }

                for (int i = start; i < index; i++)
                {
                    int mx = axisX ? i : fixedCoord;
                    int my = axisX ? fixedCoord : i;
                    MarkMatch(board, matchedFlags, mx, my);
                }
            }
        }

        private static int GetSkullRunLengthAt(
            BoardState board,
            TileDefinitions tiles,
            int x,
            int y,
            bool horizontal)
        {
            if (!tiles.IsSkull(board.GetType(x, y)))
            {
                return 0;
            }

            int fixedCoord = horizontal ? y : x;
            int cellIndex = horizontal ? x : y;
            int lineLength = horizontal ? board.Width : board.Height;
            int index = 0;

            while (index < lineLength)
            {
                int cx = horizontal ? index : fixedCoord;
                int cy = horizontal ? fixedCoord : index;

                if (!tiles.IsSkull(board.GetType(cx, cy)))
                {
                    index++;
                    continue;
                }

                int start = index;
                index++;

                while (index < lineLength)
                {
                    int nx = horizontal ? index : fixedCoord;
                    int ny = horizontal ? fixedCoord : index;

                    if (!tiles.IsSkull(board.GetType(nx, ny)))
                    {
                        break;
                    }

                    index++;
                }

                if (cellIndex >= start && cellIndex < index)
                {
                    return index - start;
                }
            }

            return 0;
        }

        private static int MeasureShapeRunThroughCell(
            BoardState board,
            TileDefinitions tiles,
            int x,
            int y,
            int stepX,
            int stepY)
        {
            string centerType = board.GetType(x, y);
            if (!CanStartShapeRun(centerType, tiles))
            {
                return 0;
            }

            string anchorShape = null;
            if (!TryIncludeInShapeRun(centerType, ref anchorShape, tiles))
            {
                return 0;
            }

            int startX = x;
            int startY = y;
            int endX = x;
            int endY = y;

            int cx = x - stepX;
            int cy = y - stepY;
            while (IsInside(board, cx, cy))
            {
                string type = board.GetType(cx, cy);
                if (!TryIncludeInShapeRun(type, ref anchorShape, tiles))
                {
                    break;
                }

                startX = cx;
                startY = cy;
                cx -= stepX;
                cy -= stepY;
            }

            int fx = x + stepX;
            int fy = y + stepY;
            while (IsInside(board, fx, fy))
            {
                string type = board.GetType(fx, fy);
                if (!TryIncludeInShapeRun(type, ref anchorShape, tiles))
                {
                    break;
                }

                endX = fx;
                endY = fy;
                fx += stepX;
                fy += stepY;
            }

            int length = CountCellsBetween(startX, startY, endX, endY, stepX, stepY);
            if (length < 3)
            {
                return 0;
            }

            return IsValidShapeRunSegment(board, tiles, startX, startY, endX, endY, stepX, stepY)
                ? length
                : 0;
        }

        private static bool TryIncludeInShapeRun(string type, ref string anchorShape, TileDefinitions tiles)
        {
            if (!FitsInShapeRun(type, anchorShape, tiles))
            {
                return false;
            }

            if (tiles.IsShape(type))
            {
                if (string.IsNullOrEmpty(anchorShape))
                {
                    anchorShape = type;
                }
                else if (type != anchorShape)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidShapeRunSegment(
            BoardState board,
            TileDefinitions tiles,
            int startX,
            int startY,
            int endX,
            int endY,
            int stepX,
            int stepY)
        {
            string anchorShape = null;
            int shapeCount = 0;
            int wildcardCount = 0;
            int x = startX;
            int y = startY;

            while (true)
            {
                string type = board.GetType(x, y);

                if (IsWildcard(type, tiles))
                {
                    wildcardCount++;
                }
                else if (tiles.IsShape(type))
                {
                    if (string.IsNullOrEmpty(anchorShape))
                    {
                        anchorShape = type;
                    }
                    else if (type != anchorShape)
                    {
                        return false;
                    }

                    shapeCount++;
                }
                else
                {
                    return false;
                }

                if (x == endX && y == endY)
                {
                    break;
                }

                x += stepX;
                y += stepY;
            }

            return (shapeCount >= 1 && shapeCount + wildcardCount >= 3) || wildcardCount >= 3;
        }

        private static int CountCellsBetween(int startX, int startY, int endX, int endY, int stepX, int stepY)
        {
            if (stepX != 0)
            {
                return Mathf.Abs(endX - startX) + 1;
            }

            return Mathf.Abs(endY - startY) + 1;
        }

        private static bool IsInside(BoardState board, int x, int y) =>
            x >= 0 && y >= 0 && x < board.Width && y < board.Height;

        private static bool MarkEntireRow(BoardState board, TileDefinitions tiles, bool[] matchedFlags, int row)
        {
            bool changed = false;

            for (int x = 0; x < board.Width; x++)
            {
                if (!tiles.IsEmpty(board.GetType(x, row)) && !matchedFlags[board.ToIndex(x, row)])
                {
                    matchedFlags[board.ToIndex(x, row)] = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool MarkEntireColumn(BoardState board, TileDefinitions tiles, bool[] matchedFlags, int column)
        {
            bool changed = false;

            for (int y = 0; y < board.Height; y++)
            {
                if (!tiles.IsEmpty(board.GetType(column, y)) && !matchedFlags[board.ToIndex(column, y)])
                {
                    matchedFlags[board.ToIndex(column, y)] = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static void ApplyLineClears(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            for (int i = 0; i < clearedRows.Count; i++)
            {
                int row = clearedRows[i];
                for (int x = 0; x < board.Width; x++)
                {
                    if (!tiles.IsEmpty(board.GetType(x, row)))
                    {
                        MarkMatch(board, matchedFlags, x, row);
                    }
                }
            }

            for (int i = 0; i < clearedColumns.Count; i++)
            {
                int column = clearedColumns[i];
                for (int y = 0; y < board.Height; y++)
                {
                    if (!tiles.IsEmpty(board.GetType(column, y)))
                    {
                        MarkMatch(board, matchedFlags, column, y);
                    }
                }
            }
        }

        private static void AddUnique(List<int> list, int value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }

        private static void MarkMatch(BoardState board, bool[] matchedFlags, int x, int y)
        {
            matchedFlags[board.ToIndex(x, y)] = true;
        }
    }
}
