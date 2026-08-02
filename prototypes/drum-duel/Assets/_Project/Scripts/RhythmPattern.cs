using System;

namespace MannLab.Games.DrumDuel
{
    public readonly struct RhythmPattern
    {
        public const int TickCount = 4;

        private readonly bool tick0;
        private readonly bool tick1;
        private readonly bool tick2;
        private readonly bool tick3;

        public RhythmPattern(bool tick0, bool tick1, bool tick2, bool tick3)
        {
            this.tick0 = tick0;
            this.tick1 = tick1;
            this.tick2 = tick2;
            this.tick3 = tick3;
        }

        public bool HasHitAt(int tickIndex)
        {
            switch (tickIndex)
            {
                case 0:
                    return tick0;
                case 1:
                    return tick1;
                case 2:
                    return tick2;
                case 3:
                    return tick3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tickIndex));
            }
        }

        public int HitCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < TickCount; i++)
                {
                    if (HasHitAt(i))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string ToPulseString()
        {
            return $"{Mark(0)} {Mark(1)} {Mark(2)} {Mark(3)}";
        }

        private string Mark(int tickIndex)
        {
            return HasHitAt(tickIndex) ? "x" : ".";
        }
    }
}
