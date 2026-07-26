using System;
using System.Collections.Generic;

namespace MannLab.Games.Game10000
{
    public sealed class BoardGenerator
    {
        private const string Target = "10000";
        private readonly Random random;

        public BoardGenerator()
            : this(Environment.TickCount)
        {
        }

        public BoardGenerator(int seed)
        {
            random = new Random(seed);
        }

        public BoardData Generate()
        {
            var digits = new int[BoardData.Size, BoardData.Size];

            for (var row = 0; row < BoardData.Size; row++)
            {
                for (var col = 0; col < BoardData.Size; col++)
                {
                    digits[row, col] = random.Next(0, 10);
                }
            }

            PlaceGuaranteedTarget(digits);

            return new BoardData(digits, FindAllTargetIndices(digits));
        }

        private void PlaceGuaranteedTarget(int[,] digits)
        {
            var horizontal = random.Next(0, 2) == 0;
            var row = random.Next(0, BoardData.Size);
            var col = random.Next(0, BoardData.Size);

            if (horizontal)
            {
                col = random.Next(0, BoardData.Size - Target.Length + 1);
            }
            else
            {
                row = random.Next(0, BoardData.Size - Target.Length + 1);
            }

            for (var i = 0; i < Target.Length; i++)
            {
                var targetRow = horizontal ? row : row + i;
                var targetCol = horizontal ? col + i : col;
                digits[targetRow, targetCol] = Target[i] - '0';
            }
        }

        private static HashSet<int> FindAllTargetIndices(int[,] digits)
        {
            var indices = new HashSet<int>();

            for (var row = 0; row < BoardData.Size; row++)
            {
                for (var col = 0; col < BoardData.Size; col++)
                {
                    AddTargetIfMatched(digits, indices, row, col, 0, 1);
                    AddTargetIfMatched(digits, indices, row, col, 1, 0);
                }
            }

            return indices;
        }

        private static void AddTargetIfMatched(int[,] digits, HashSet<int> indices, int row, int col, int rowStep, int colStep)
        {
            var endRow = row + rowStep * (Target.Length - 1);
            var endCol = col + colStep * (Target.Length - 1);

            if (endRow >= BoardData.Size || endCol >= BoardData.Size)
            {
                return;
            }

            for (var i = 0; i < Target.Length; i++)
            {
                var digit = digits[row + rowStep * i, col + colStep * i];
                if (digit != Target[i] - '0')
                {
                    return;
                }
            }

            for (var i = 0; i < Target.Length; i++)
            {
                var targetRow = row + rowStep * i;
                var targetCol = col + colStep * i;
                indices.Add(targetRow * BoardData.Size + targetCol);
            }
        }
    }
}

