using System.Collections.Generic;

namespace MannLab.Games.Game10000
{
    public sealed class BoardData
    {
        public const int Size = 10;

        public BoardData(int[,] digits, HashSet<int> targetIndices)
        {
            Digits = digits;
            TargetIndices = targetIndices;
        }

        public int[,] Digits { get; }

        public HashSet<int> TargetIndices { get; }

        public bool IsTargetCell(int index)
        {
            return TargetIndices.Contains(index);
        }

        public int GetDigitAtIndex(int index)
        {
            return Digits[index / Size, index % Size];
        }
    }
}

