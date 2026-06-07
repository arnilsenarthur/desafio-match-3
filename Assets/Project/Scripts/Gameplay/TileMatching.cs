using System.Collections.Generic;
using Gazeus.DesafioMatch3.Data;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    // Core match-3 rules. Tiles fall into three families: shapes (circle, square, ...),
    // wildcards (joker, bomb) and the skull. Two independent rules decide what clears:
    //
    //   1) Shape run (>= 3 in a line). Jokers and bombs act as wildcards that stand in
    //      for the run's anchor shape, so a run is valid with 3 shapes OR 2 shapes + 1
    //      wildcard. Legend: A/B = shapes, J = joker, * = wildcard (joker or bomb).
    //
    //          A A A   -> match (3 shapes)
    //          A J A   -> match (wildcard fills the gap)
    //          A A J   -> match (wildcard on the edge)
    //          A A J A -> match (length 4)
    //          A B A   -> no match (two different shapes, wildcard can't unify them)
    //          A A J J -> match, but mixes 2 shapes + 2 wildcards
    //
    //   2) Same-type run (>= 3 identical specials in a line). Lets skulls, jokers or
    //      bombs clear on their own even with no shape present:
    //
    //          S S S   -> match (skulls)
    //          J J J   -> match (jokers)
    //          B B B   -> match (bombs)
    //          J B J   -> no match (specials must be the SAME type)
    //
    // A run of length >= 4 (rule 1) also queues a full row/column line clear. Bombs that
    // end up matched additionally detonate their whole row and column (see PropagateBombClears).
    public static class TileMatching
    {
        // Recomputes every match on the board into matchedFlags. Order matters: shape runs
        // run first (they may queue line clears), then same-type special runs, then the
        // queued line clears are expanded into actual matched cells.
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
            ScanSameTypeSpecialRuns(board, tiles, matchedFlags, horizontal: true);
            ScanSameTypeSpecialRuns(board, tiles, matchedFlags, horizontal: false);

            ApplyLineClears(board, tiles, matchedFlags, clearedRows, clearedColumns);

            return HasAnyMatch(board, matchedFlags);
        }

        // Any already-matched bomb detonates its entire row and column. Newly cleared cells
        // may include further bombs, so the scan repeats until a full pass adds nothing
        // (chain reactions). Run after FindAndMarkMatches has seeded the initial matches.
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

        private static bool HasAnyMatch(BoardState board, bool[] matchedFlags)
        {
            int cellCount = board.Width * board.Height;
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

        // Cheap single-cell test used by board generation/refill to avoid spawning a tile
        // that would instantly form a match. Checks both axes for a shape run or a same-type
        // special run passing through this cell.
        public static bool CreatesMatchAt(BoardState board, TileDefinitions tiles, int x, int y)
        {
            if (tiles.IsEmpty(board.GetType(x, y)))
            {
                return false;
            }

            return MeasureShapeRunThroughCell(board, tiles, x, y, 1, 0) >= 3 ||
                   MeasureShapeRunThroughCell(board, tiles, x, y, 0, 1) >= 3 ||
                   GetSameTypeRunLengthAt(board, tiles, x, y, horizontal: true) >= 3 ||
                   GetSameTypeRunLengthAt(board, tiles, x, y, horizontal: false) >= 3;
        }

        // Can this cell extend a shape run whose anchor shape is anchorShape (null = not yet
        // decided)? Empty cells and skulls never fit; wildcards always fit; a real shape fits
        // only while the anchor is unset or identical to it.
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

        private static bool IsSpecial(string typeId, TileDefinitions tiles) =>
            tiles.IsSkull(typeId) || tiles.IsJoker(typeId) || tiles.IsBomb(typeId);

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

        // Walks a single row (axisX) or column looking for shape runs. axisX==true means the
        // line is horizontal and fixedCoord is its y; otherwise it is vertical and fixedCoord is x.
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

                // Skip cells that can't anchor a run (empty / skull).
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

                // Greedily extend while the next cell still fits the run's anchor shape.
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

                // A long-enough run still has to satisfy the shape/wildcard ratio (e.g. an
                // all-wildcard run is rejected here and handled by the same-type scan instead).
                // On failure, retry from start+1 so a later cell can anchor a different run.
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

                // 4+ in a line earns a full row/column clear, expanded later in ApplyLineClears.
                if (runLength >= 4)
                {
                    AddUnique(lineClears, fixedCoord);
                }

                index = start + 1;
            }
        }

        private static void ScanSameTypeSpecialRuns(
            BoardState board,
            TileDefinitions tiles,
            bool[] matchedFlags,
            bool horizontal)
        {
            if (horizontal)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    ScanSameTypeSpecialLine(board, tiles, matchedFlags, y, axisX: true);
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    ScanSameTypeSpecialLine(board, tiles, matchedFlags, x, axisX: false);
                }
            }
        }

        // Matches runs of >= 3 identical specials (S S S / J J J / B B B). Unlike the shape
        // scan, the run is held to exact type equality, so mixed specials never combine.
        private static void ScanSameTypeSpecialLine(
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
                string type = board.GetType(x, y);

                if (!IsSpecial(type, tiles))
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

                    if (board.GetType(nx, ny) != type)
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

        private static int GetSameTypeRunLengthAt(
            BoardState board,
            TileDefinitions tiles,
            int x,
            int y,
            bool horizontal)
        {
            string type = board.GetType(x, y);
            if (!IsSpecial(type, tiles))
            {
                return 0;
            }

            int stepX = horizontal ? 1 : 0;
            int stepY = horizontal ? 0 : 1;
            int length = 1;

            int cx = x - stepX;
            int cy = y - stepY;
            while (IsInside(board, cx, cy) && board.GetType(cx, cy) == type)
            {
                length++;
                cx -= stepX;
                cy -= stepY;
            }

            int fx = x + stepX;
            int fy = y + stepY;
            while (IsInside(board, fx, fy) && board.GetType(fx, fy) == type)
            {
                length++;
                fx += stepX;
                fy += stepY;
            }

            return length;
        }

        // Length of the shape run passing through (x,y) along one axis, or 0 if it isn't a
        // valid run. Expands outward from the cell in both directions, growing the segment
        // while neighbours keep fitting the anchor shape.
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

        // Tests a cell against the running shape and, on success, locks in the anchor shape
        // the first time a real shape is seen. Wildcards pass through without setting it.
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

        // Final gate for a candidate segment: every cell must be a wildcard or a shape equal
        // to the single anchor shape, and the mix must be either 3+ shapes, or 2+ shapes
        // backed by at least one wildcard. This is what rejects pure-wildcard segments.
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

            return shapeCount >= 3 || (shapeCount >= 2 && wildcardCount >= 1);
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
