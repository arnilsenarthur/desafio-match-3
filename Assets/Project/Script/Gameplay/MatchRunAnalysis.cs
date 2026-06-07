using System.Collections.Generic;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Gameplay
{
    public readonly struct MatchRun
    {
        public MatchRun(bool horizontal, int fixedIndex, int startIndex, int endIndex, int count)
        {
            Horizontal = horizontal;
            FixedIndex = fixedIndex;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Count = count;
        }

        public bool Horizontal { get; }
        public int FixedIndex { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        public int Count { get; }

        public bool Contains(Vector2Int cell)
        {
            if (Horizontal)
            {
                return cell.y == FixedIndex && cell.x >= StartIndex && cell.x <= EndIndex;
            }

            return cell.x == FixedIndex && cell.y >= StartIndex && cell.y <= EndIndex;
        }
    }

    public static class MatchRunAnalysis
    {
        public const int SpecialLinearMatchMinimum = 4;

        public static bool HasSpecialBonusMatch(BoardSequence sequence)
        {
            if (sequence == null)
            {
                return false;
            }

            if (sequence.ClearedRows.Count > 0 || sequence.ClearedColumns.Count > 0)
            {
                return true;
            }

            return HasLinearRunOfAtLeast(sequence.MatchedPosition, SpecialLinearMatchMinimum);
        }

        public static bool HasLinearRunOfAtLeast(IReadOnlyList<Vector2Int> matchedCells, int minimumCount)
        {
            if (matchedCells == null || matchedCells.Count == 0 || minimumCount <= 0)
            {
                return false;
            }

            foreach (MatchRun run in FindRuns(matchedCells))
            {
                if (run.Count >= minimumCount)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsCellInLinearRunOfAtLeast(
            Vector2Int cell,
            IReadOnlyList<Vector2Int> matchedCells,
            int minimumCount)
        {
            if (matchedCells == null || matchedCells.Count == 0 || minimumCount <= 0)
            {
                return false;
            }

            foreach (MatchRun run in FindRuns(matchedCells))
            {
                if (run.Count >= minimumCount && run.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<MatchRun> FindRuns(IReadOnlyList<Vector2Int> matchedCells)
        {
            if (matchedCells == null || matchedCells.Count == 0)
            {
                yield break;
            }

            foreach (MatchRun run in FindAxisRuns(matchedCells, horizontal: true))
            {
                yield return run;
            }

            foreach (MatchRun run in FindAxisRuns(matchedCells, horizontal: false))
            {
                yield return run;
            }
        }

        private static IEnumerable<MatchRun> FindAxisRuns(IReadOnlyList<Vector2Int> cells, bool horizontal)
        {
            var grouped = new Dictionary<int, List<int>>();

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                int fixedIndex = horizontal ? cell.y : cell.x;
                int variableIndex = horizontal ? cell.x : cell.y;

                if (!grouped.TryGetValue(fixedIndex, out List<int> values))
                {
                    values = new List<int>();
                    grouped.Add(fixedIndex, values);
                }

                values.Add(variableIndex);
            }

            foreach (KeyValuePair<int, List<int>> pair in grouped)
            {
                pair.Value.Sort();
                int runStart = pair.Value[0];
                int previous = pair.Value[0];

                for (int i = 1; i <= pair.Value.Count; i++)
                {
                    int current = i < pair.Value.Count ? pair.Value[i] : previous + 2;
                    if (current == previous + 1)
                    {
                        previous = current;
                        continue;
                    }

                    int count = previous - runStart + 1;
                    if (count >= 3)
                    {
                        yield return new MatchRun(horizontal, pair.Key, runStart, previous, count);
                    }

                    if (i < pair.Value.Count)
                    {
                        runStart = pair.Value[i];
                        previous = pair.Value[i];
                    }
                }
            }
        }
    }
}
