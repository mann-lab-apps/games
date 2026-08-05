using System;
using System.Collections.Generic;

namespace MannLab.Games.Game2048Crash
{
    public enum Crash2048Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public readonly struct Crash2048MoveResult
    {
        public Crash2048MoveResult(bool moved, bool specialCrashed, bool gameOver)
        {
            Moved = moved;
            SpecialCrashed = specialCrashed;
            GameOver = gameOver;
        }

        public bool Moved { get; }

        public bool SpecialCrashed { get; }

        public bool GameOver { get; }
    }

    public sealed class Crash2048Board
    {
        public const int Size = 4;
        public const int CellCount = Size * Size;

        private readonly int[] cells = new int[CellCount];
        private readonly Random random;

        public Crash2048Board()
            : this(Environment.TickCount)
        {
        }

        public Crash2048Board(int seed)
        {
            random = new Random(seed);
            SpecialValue = 2;
            SpecialIndex = -1;
        }

        public int Stage { get; private set; }

        public int SpecialIndex { get; private set; }

        public int SpecialValue { get; private set; }

        public int GetValueAtIndex(int index)
        {
            return cells[index];
        }

        public bool IsSpecialAtIndex(int index)
        {
            return SpecialIndex == index;
        }

        public void StartNew()
        {
            Array.Clear(cells, 0, cells.Length);
            Stage = 0;
            SpecialValue = 2;
            SpecialIndex = -1;
            SpawnSpecialBlock();
            SpawnNormalTile();
            SpawnNormalTile();
        }

        public Crash2048MoveResult Move(Crash2048Direction direction)
        {
            var specialIndex = SpecialIndex;
            var moved = Slide(cells, ref specialIndex, SpecialValue, direction, out var specialCrashed);
            if (!moved)
            {
                return new Crash2048MoveResult(false, false, IsGameOver());
            }

            SpecialIndex = specialIndex;

            if (specialCrashed)
            {
                Stage++;
                SpecialValue *= 2;
                SpawnSpecialBlock();
            }

            SpawnNormalTile();

            return new Crash2048MoveResult(true, specialCrashed, IsGameOver());
        }

        public bool IsGameOver()
        {
            if (GetEmptyIndices().Count > 0)
            {
                return false;
            }

            foreach (Crash2048Direction direction in Enum.GetValues(typeof(Crash2048Direction)))
            {
                var copy = new int[CellCount];
                Array.Copy(cells, copy, CellCount);
                var specialIndex = SpecialIndex;
                if (Slide(copy, ref specialIndex, SpecialValue, direction, out _))
                {
                    return false;
                }
            }

            return true;
        }

        public void LoadForTests(int[] values, int specialIndex, int specialValue, int stage)
        {
            if (values == null || values.Length != CellCount)
            {
                throw new ArgumentException("Board test values must contain 16 cells.", nameof(values));
            }

            Array.Copy(values, cells, CellCount);
            SpecialIndex = specialIndex;
            SpecialValue = specialValue;
            Stage = stage;

            if (specialIndex >= 0)
            {
                cells[specialIndex] = 0;
            }
        }

        private bool SpawnNormalTile()
        {
            var emptyIndices = GetEmptyIndices();
            if (emptyIndices.Count == 0)
            {
                return false;
            }

            var index = emptyIndices[random.Next(emptyIndices.Count)];
            cells[index] = random.NextDouble() < 0.9 ? 2 : 4;
            return true;
        }

        private bool SpawnSpecialBlock()
        {
            var emptyIndices = GetEmptyIndices();
            if (emptyIndices.Count == 0)
            {
                SpecialIndex = -1;
                return false;
            }

            SpecialIndex = emptyIndices[random.Next(emptyIndices.Count)];
            cells[SpecialIndex] = 0;
            return true;
        }

        private List<int> GetEmptyIndices()
        {
            var indices = new List<int>();
            for (var i = 0; i < CellCount; i++)
            {
                if (i != SpecialIndex && cells[i] == 0)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        private static bool Slide(
            int[] targetCells,
            ref int specialIndex,
            int specialValue,
            Crash2048Direction direction,
            out bool specialCrashed)
        {
            var merged = new bool[CellCount];
            var moved = false;
            specialCrashed = false;
            var keepMoving = true;

            while (keepMoving)
            {
                keepMoving = false;
                foreach (var source in Traversal(direction))
                {
                    var value = targetCells[source];
                    if (value == 0)
                    {
                        continue;
                    }

                    var target = Neighbor(source, direction);
                    if (target < 0)
                    {
                        continue;
                    }

                    if (target == specialIndex)
                    {
                        if (value != specialValue)
                        {
                            continue;
                        }

                        targetCells[target] = value;
                        targetCells[source] = 0;
                        merged[target] = true;
                        merged[source] = false;
                        specialIndex = -1;
                        specialCrashed = true;
                        moved = true;
                        keepMoving = true;
                        continue;
                    }

                    if (targetCells[target] == 0)
                    {
                        targetCells[target] = value;
                        targetCells[source] = 0;
                        merged[target] = merged[source];
                        merged[source] = false;
                        moved = true;
                        keepMoving = true;
                        continue;
                    }

                    if (targetCells[target] == value && !merged[target] && !merged[source])
                    {
                        targetCells[target] = value * 2;
                        targetCells[source] = 0;
                        merged[target] = true;
                        merged[source] = false;
                        moved = true;
                        keepMoving = true;
                    }
                }
            }

            return moved;
        }

        private static IEnumerable<int> Traversal(Crash2048Direction direction)
        {
            switch (direction)
            {
                case Crash2048Direction.Left:
                    for (var row = 0; row < Size; row++)
                    {
                        for (var column = 0; column < Size; column++)
                        {
                            yield return row * Size + column;
                        }
                    }

                    break;
                case Crash2048Direction.Right:
                    for (var row = 0; row < Size; row++)
                    {
                        for (var column = Size - 1; column >= 0; column--)
                        {
                            yield return row * Size + column;
                        }
                    }

                    break;
                case Crash2048Direction.Up:
                    for (var column = 0; column < Size; column++)
                    {
                        for (var row = 0; row < Size; row++)
                        {
                            yield return row * Size + column;
                        }
                    }

                    break;
                case Crash2048Direction.Down:
                    for (var column = 0; column < Size; column++)
                    {
                        for (var row = Size - 1; row >= 0; row--)
                        {
                            yield return row * Size + column;
                        }
                    }

                    break;
            }
        }

        private static int Neighbor(int index, Crash2048Direction direction)
        {
            var row = index / Size;
            var column = index % Size;

            switch (direction)
            {
                case Crash2048Direction.Left:
                    return column == 0 ? -1 : index - 1;
                case Crash2048Direction.Right:
                    return column == Size - 1 ? -1 : index + 1;
                case Crash2048Direction.Up:
                    return row == 0 ? -1 : index - Size;
                case Crash2048Direction.Down:
                    return row == Size - 1 ? -1 : index + Size;
                default:
                    return -1;
            }
        }
    }
}
