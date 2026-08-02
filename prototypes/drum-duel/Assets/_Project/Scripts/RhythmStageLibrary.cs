using System;

namespace MannLab.Games.DrumDuel
{
    public static class RhythmStageLibrary
    {
        private static readonly RhythmPattern[] Patterns =
        {
            new RhythmPattern(true, false, false, false),
            new RhythmPattern(true, false, true, false),
            new RhythmPattern(true, false, false, true),
            new RhythmPattern(true, true, false, false),
            new RhythmPattern(false, true, false, true),
            new RhythmPattern(true, false, true, true),
            new RhythmPattern(true, true, false, true),
            new RhythmPattern(false, true, true, false),
            new RhythmPattern(true, true, true, false),
            new RhythmPattern(false, true, true, true),
            new RhythmPattern(true, true, true, true),
        };

        public static RhythmPattern PatternForStage(int stage)
        {
            if (stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), "Stage must be one or greater.");
            }

            var index = (stage - 1) % Patterns.Length;
            return Patterns[index];
        }

        public static float BeatsPerMinuteForStage(int stage)
        {
            if (stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), "Stage must be one or greater.");
            }

            var earlyRamp = 80f + Math.Min(stage - 1, 9) * 4f;
            if (stage <= 10)
            {
                return earlyRamp;
            }

            return Math.Min(160f, 116f + (stage - 10) * 2.5f);
        }

        public static float TickDurationForStage(int stage)
        {
            return 60f / BeatsPerMinuteForStage(stage);
        }
    }
}
