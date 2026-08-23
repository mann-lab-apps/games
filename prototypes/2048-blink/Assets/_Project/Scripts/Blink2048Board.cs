using System;
using System.Collections.Generic;

namespace MannLab.Games.Game2048Blink
{
    public enum Blink2048Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public readonly struct Blink2048MoveResult
    {
        public Blink2048MoveResult(bool moved, bool gameOver, int scoreGained, int spawnedTileIndex, int spawnedTileValue)
        {
            Moved = moved;
            GameOver = gameOver;
            ScoreGained = scoreGained;
            SpawnedTileIndex = spawnedTileIndex;
            SpawnedTileValue = spawnedTileValue;
        }

        public bool Moved { get; }

        public bool GameOver { get; }

        public int ScoreGained { get; }

        public int SpawnedTileIndex { get; }

        public int SpawnedTileValue { get; }
    }

    public sealed class Blink2048Board
    {
        public const int Size = 4;
        public const int CellCount = Size * Size;

        private readonly int[] cells = new int[CellCount];
        private readonly Random random;

        public Blink2048Board()
            : this(Environment.TickCount)
        {
        }

        public Blink2048Board(int seed)
        {
            random = new Random(seed);
        }

        public int Score { get; private set; }

        public int Turn { get; private set; }

        public int HighestTile { get; private set; }

        public int GrayCrossPhase => Turn % Size;

        public int HiddenRow => GrayCrossPhase;

        public int HiddenColumn => (GrayCrossPhase + 2) % Size;

        public string GrayCrossName => $"Cross {GrayCrossPhase + 1}/{Size}";

        public int GetValueAtIndex(int index)
        {
            return cells[index];
        }

        public bool IsHiddenIndex(int index)
        {
            return index / Size == HiddenRow || index % Size == HiddenColumn;
        }

        public void StartNew()
        {
            Array.Clear(cells, 0, cells.Length);
            Score = 0;
            Turn = 0;
            HighestTile = 0;
            SpawnTile(out _, out _);
            SpawnTile(out _, out _);
        }

        public Blink2048MoveResult Move(Blink2048Direction direction)
        {
            var before = new int[CellCount];
            Array.Copy(cells, before, CellCount);

            var scoreGained = Slide(direction);
            var moved = !CellsEqual(before, cells);
            if (!moved)
            {
                return new Blink2048MoveResult(false, IsGameOver(), 0, -1, 0);
            }

            Score += scoreGained;
            Turn++;
            SpawnTile(out var spawnedTileIndex, out var spawnedTileValue);
            RefreshHighestTile();

            return new Blink2048MoveResult(true, IsGameOver(), scoreGained, spawnedTileIndex, spawnedTileValue);
        }

        public bool IsGameOver()
        {
            if (GetEmptyIndices().Count > 0)
            {
                return false;
            }

            for (var row = 0; row < Size; row++)
            {
                for (var column = 0; column < Size; column++)
                {
                    var index = row * Size + column;
                    var value = cells[index];
                    if (column + 1 < Size && cells[index + 1] == value)
                    {
                        return false;
                    }

                    if (row + 1 < Size && cells[index + Size] == value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void LoadForTests(int[] values, int score = 0, int turn = 0)
        {
            if (values == null || values.Length != CellCount)
            {
                throw new ArgumentException("Board test values must contain 16 cells.", nameof(values));
            }

            Array.Copy(values, cells, CellCount);
            Score = score;
            Turn = turn;
            RefreshHighestTile();
        }

        private int Slide(Blink2048Direction direction)
        {
            var scoreGained = 0;
            for (var line = 0; line < Size; line++)
            {
                var indices = LineIndices(direction, line);
                var merged = MergeLine(indices, out var lineScore);
                scoreGained += lineScore;
                for (var i = 0; i < Size; i++)
                {
                    cells[indices[i]] = merged[i];
                }
            }

            return scoreGained;
        }

        private int[] MergeLine(int[] indices, out int lineScore)
        {
            lineScore = 0;
            var values = new List<int>(Size);
            foreach (var index in indices)
            {
                var value = cells[index];
                if (value > 0)
                {
                    values.Add(value);
                }
            }

            var merged = new List<int>(Size);
            for (var i = 0; i < values.Count; i++)
            {
                if (i + 1 < values.Count && values[i] == values[i + 1])
                {
                    var value = values[i] * 2;
                    merged.Add(value);
                    lineScore += value;
                    i++;
                    continue;
                }

                merged.Add(values[i]);
            }

            while (merged.Count < Size)
            {
                merged.Add(0);
            }

            return merged.ToArray();
        }

        private bool SpawnTile(out int spawnedTileIndex, out int spawnedTileValue)
        {
            spawnedTileIndex = -1;
            spawnedTileValue = 0;
            var emptyIndices = GetEmptyIndices();
            if (emptyIndices.Count == 0)
            {
                return false;
            }

            spawnedTileIndex = emptyIndices[random.Next(emptyIndices.Count)];
            spawnedTileValue = random.NextDouble() < 0.9 ? 2 : 4;
            cells[spawnedTileIndex] = spawnedTileValue;
            if (spawnedTileValue > HighestTile)
            {
                HighestTile = spawnedTileValue;
            }

            return true;
        }

        private List<int> GetEmptyIndices()
        {
            var result = new List<int>();
            for (var i = 0; i < CellCount; i++)
            {
                if (cells[i] == 0)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private void RefreshHighestTile()
        {
            var highest = 0;
            foreach (var value in cells)
            {
                if (value > highest)
                {
                    highest = value;
                }
            }

            HighestTile = highest;
        }

        private static int[] LineIndices(Blink2048Direction direction, int line)
        {
            var result = new int[Size];
            for (var i = 0; i < Size; i++)
            {
                switch (direction)
                {
                    case Blink2048Direction.Left:
                        result[i] = line * Size + i;
                        break;
                    case Blink2048Direction.Right:
                        result[i] = line * Size + (Size - 1 - i);
                        break;
                    case Blink2048Direction.Up:
                        result[i] = i * Size + line;
                        break;
                    case Blink2048Direction.Down:
                        result[i] = (Size - 1 - i) * Size + line;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }

            return result;
        }

        private static bool CellsEqual(int[] left, int[] right)
        {
            for (var i = 0; i < CellCount; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
