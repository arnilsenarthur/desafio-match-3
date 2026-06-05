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

            ScanColorRuns(board, tiles, matchedFlags, clearedRows, clearedColumns, horizontal: true);
            ScanColorRuns(board, tiles, matchedFlags, clearedRows, clearedColumns, horizontal: false);
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

            return MeasureColorRunThroughCell(board, tiles, x, y, 1, 0) >= 3 ||
                   MeasureColorRunThroughCell(board, tiles, x, y, 0, 1) >= 3 ||
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

        public static bool FitsInColorRun(string cellType, string anchorColor, TileDefinitions tiles)
        {
            if (tiles.IsEmpty(cellType))
            {
                return false;
            }

            if (tiles.IsSkull(cellType))
            {
                return false;
            }

            if (string.IsNullOrEmpty(anchorColor))
            {
                return tiles.IsColor(cellType) || tiles.IsJoker(cellType) || tiles.IsBomb(cellType);
            }

            if (cellType == anchorColor)
            {
                return true;
            }

            return tiles.IsJoker(cellType) || tiles.IsBomb(cellType);
        }

        private static void ScanColorRuns(
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
                    ScanColorLine(board, tiles, matchedFlags, clearedRows, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanColorLine(board, tiles, matchedFlags, clearedColumns, x, axisX: false);
                }
            }
        }

        private static void ScanColorLine(
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

                if (tiles.IsEmpty(type) || tiles.IsSkull(type))
                {
                    index++;
                    continue;
                }

                int start = index;
                string anchorColor = tiles.IsColor(type) ? type : null;
                int stepX = axisX ? 1 : 0;
                int stepY = axisX ? 0 : 1;
                index++;

                while (index < lineLength)
                {
                    int nx = axisX ? index : fixedCoord;
                    int ny = axisX ? fixedCoord : index;
                    string nextType = board.GetType(nx, ny);

                    if (!TryIncludeInColorRun(nextType, ref anchorColor, tiles))
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
                    !IsValidColorRunSegment(board, tiles, startX, startY, endX, endY, stepX, stepY))
                {
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

        private static int MeasureColorRunThroughCell(
            BoardState board,
            TileDefinitions tiles,
            int x,
            int y,
            int stepX,
            int stepY)
        {
            string centerType = board.GetType(x, y);
            if (tiles.IsEmpty(centerType) || tiles.IsSkull(centerType))
            {
                return 0;
            }

            if (!tiles.IsColor(centerType) &&
                !tiles.IsJoker(centerType) &&
                !tiles.IsBomb(centerType))
            {
                return 0;
            }

            string anchorColor = tiles.IsColor(centerType) ? centerType : null;
            int startX = x;
            int startY = y;
            int endX = x;
            int endY = y;

            int cx = x - stepX;
            int cy = y - stepY;
            while (IsInside(board, cx, cy))
            {
                string type = board.GetType(cx, cy);
                if (!TryIncludeInColorRun(type, ref anchorColor, tiles))
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
                if (!TryIncludeInColorRun(type, ref anchorColor, tiles))
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

            return IsValidColorRunSegment(board, tiles, startX, startY, endX, endY, stepX, stepY)
                ? length
                : 0;
        }

        private static bool TryIncludeInColorRun(string type, ref string anchorColor, TileDefinitions tiles)
        {
            if (!FitsInColorRun(type, anchorColor, tiles))
            {
                return false;
            }

            if (tiles.IsColor(type))
            {
                if (string.IsNullOrEmpty(anchorColor))
                {
                    anchorColor = type;
                }
                else if (type != anchorColor)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidColorRunSegment(
            BoardState board,
            TileDefinitions tiles,
            int startX,
            int startY,
            int endX,
            int endY,
            int stepX,
            int stepY)
        {
            string anchorColor = null;
            int wildcardCount = 0;
            int x = startX;
            int y = startY;

            while (true)
            {
                string type = board.GetType(x, y);

                if (tiles.IsColor(type))
                {
                    if (string.IsNullOrEmpty(anchorColor))
                    {
                        anchorColor = type;
                    }
                    else if (type != anchorColor)
                    {
                        return false;
                    }
                }
                else if (tiles.IsJoker(type) || tiles.IsBomb(type))
                {
                    wildcardCount++;
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

            return !string.IsNullOrEmpty(anchorColor) || wildcardCount >= 3;
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
