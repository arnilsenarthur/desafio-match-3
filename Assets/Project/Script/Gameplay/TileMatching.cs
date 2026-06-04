using System.Collections.Generic;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public static class TileMatching
    {
        public static bool FindAndMarkMatches(
            BoardState board,
            TileTypeRegistry registry,
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

            ScanColorRuns(board, registry, matchedFlags, clearedRows, clearedColumns, horizontal: true);
            ScanColorRuns(board, registry, matchedFlags, clearedRows, clearedColumns, horizontal: false);
            ScanSkullRuns(board, registry, matchedFlags, horizontal: true);
            ScanSkullRuns(board, registry, matchedFlags, horizontal: false);

            ApplyLineClears(board, matchedFlags, clearedRows, clearedColumns);

            for (int i = 0; i < cellCount; i++)
            {
                if (matchedFlags[i])
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountSkullsInMatches(BoardState board, TileTypeRegistry registry, bool[] matchedFlags)
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

                    if (registry.IsSkull(board.GetType(x, y)))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static bool CreatesMatchAt(BoardState board, TileTypeRegistry registry, int x, int y)
        {
            if (board.GetType(x, y) < 0)
            {
                return false;
            }

            return MeasureColorRunThroughCell(board, registry, x, y, 1, 0) >= 3 ||
                   MeasureColorRunThroughCell(board, registry, x, y, 0, 1) >= 3 ||
                   GetSkullRunLengthAt(board, registry, x, y, horizontal: true) >= 3 ||
                   GetSkullRunLengthAt(board, registry, x, y, horizontal: false) >= 3;
        }

        public static void PropagateBombClears(
            BoardState board,
            TileTypeRegistry registry,
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

                        if (!registry.IsBomb(board.GetType(x, y)))
                        {
                            continue;
                        }

                        if (MarkEntireRow(board, matchedFlags, y))
                        {
                            changed = true;
                            AddUnique(clearedRows, y);
                        }

                        if (MarkEntireColumn(board, matchedFlags, x))
                        {
                            changed = true;
                            AddUnique(clearedColumns, x);
                        }
                    }
                }
            }
        }

        public static bool FitsInColorRun(int cellType, int anchorColor, TileTypeRegistry registry)
        {
            if (registry.IsEmpty(cellType))
            {
                return false;
            }

            if (registry.IsSkull(cellType))
            {
                return false;
            }

            if (anchorColor < 0)
            {
                return registry.IsColor(cellType) || registry.IsJoker(cellType) || registry.IsBomb(cellType);
            }

            if (cellType == anchorColor)
            {
                return true;
            }

            return registry.IsJoker(cellType) || registry.IsBomb(cellType);
        }

        private static void ScanColorRuns(
            BoardState board,
            TileTypeRegistry registry,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns,
            bool horizontal)
        {
            if (horizontal)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    ScanColorLine(board, registry, matchedFlags, clearedRows, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanColorLine(board, registry, matchedFlags, clearedColumns, x, axisX: false);
                }
            }
        }

        private static void ScanColorLine(
            BoardState board,
            TileTypeRegistry registry,
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
                int type = board.GetType(x, y);

                if (registry.IsEmpty(type) || registry.IsSkull(type))
                {
                    index++;
                    continue;
                }

                int start = index;
                int anchorColor = registry.IsColor(type) ? type : -1;
                int stepX = axisX ? 1 : 0;
                int stepY = axisX ? 0 : 1;
                index++;

                while (index < lineLength)
                {
                    int nx = axisX ? index : fixedCoord;
                    int ny = axisX ? fixedCoord : index;
                    int nextType = board.GetType(nx, ny);

                    if (!TryIncludeInColorRun(nextType, ref anchorColor, registry))
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
                    !IsValidColorRunSegment(board, registry, startX, startY, endX, endY, stepX, stepY))
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
            TileTypeRegistry registry,
            bool[] matchedFlags,
            bool horizontal)
        {
            if (horizontal)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    ScanSkullLine(board, registry, matchedFlags, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanSkullLine(board, registry, matchedFlags, x, axisX: false);
                }
            }
        }

        private static void ScanSkullLine(
            BoardState board,
            TileTypeRegistry registry,
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

                if (!registry.IsSkull(board.GetType(x, y)))
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

                    if (!registry.IsSkull(board.GetType(nx, ny)))
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
            TileTypeRegistry registry,
            int x,
            int y,
            bool horizontal)
        {
            if (!registry.IsSkull(board.GetType(x, y)))
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

                if (!registry.IsSkull(board.GetType(cx, cy)))
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

                    if (!registry.IsSkull(board.GetType(nx, ny)))
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
            TileTypeRegistry registry,
            int x,
            int y,
            int stepX,
            int stepY)
        {
            int centerType = board.GetType(x, y);
            if (registry.IsEmpty(centerType) || registry.IsSkull(centerType))
            {
                return 0;
            }

            if (!registry.IsColor(centerType) &&
                !registry.IsJoker(centerType) &&
                !registry.IsBomb(centerType))
            {
                return 0;
            }

            int anchorColor = registry.IsColor(centerType) ? centerType : -1;
            int startX = x;
            int startY = y;
            int endX = x;
            int endY = y;

            int cx = x - stepX;
            int cy = y - stepY;
            while (IsInside(board, cx, cy))
            {
                int type = board.GetType(cx, cy);
                if (!TryIncludeInColorRun(type, ref anchorColor, registry))
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
                int type = board.GetType(fx, fy);
                if (!TryIncludeInColorRun(type, ref anchorColor, registry))
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

            return IsValidColorRunSegment(board, registry, startX, startY, endX, endY, stepX, stepY)
                ? length
                : 0;
        }

        private static bool TryIncludeInColorRun(int type, ref int anchorColor, TileTypeRegistry registry)
        {
            if (!FitsInColorRun(type, anchorColor, registry))
            {
                return false;
            }

            if (registry.IsColor(type))
            {
                if (anchorColor < 0)
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
            TileTypeRegistry registry,
            int startX,
            int startY,
            int endX,
            int endY,
            int stepX,
            int stepY)
        {
            int anchorColor = -1;
            int wildcardCount = 0;
            int x = startX;
            int y = startY;

            while (true)
            {
                int type = board.GetType(x, y);

                if (registry.IsColor(type))
                {
                    if (anchorColor < 0)
                    {
                        anchorColor = type;
                    }
                    else if (type != anchorColor)
                    {
                        return false;
                    }
                }
                else if (registry.IsJoker(type) || registry.IsBomb(type))
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

            return anchorColor >= 0 || wildcardCount >= 3;
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

        private static bool MarkEntireRow(BoardState board, bool[] matchedFlags, int row)
        {
            bool changed = false;

            for (int x = 0; x < board.Width; x++)
            {
                if (board.GetType(x, row) >= 0 && !matchedFlags[board.ToIndex(x, row)])
                {
                    matchedFlags[board.ToIndex(x, row)] = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool MarkEntireColumn(BoardState board, bool[] matchedFlags, int column)
        {
            bool changed = false;

            for (int y = 0; y < board.Height; y++)
            {
                if (board.GetType(column, y) >= 0 && !matchedFlags[board.ToIndex(column, y)])
                {
                    matchedFlags[board.ToIndex(column, y)] = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static void ApplyLineClears(
            BoardState board,
            bool[] matchedFlags,
            List<int> clearedRows,
            List<int> clearedColumns)
        {
            for (int i = 0; i < clearedRows.Count; i++)
            {
                int row = clearedRows[i];
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.GetType(x, row) >= 0)
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
                    if (board.GetType(column, y) >= 0)
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
