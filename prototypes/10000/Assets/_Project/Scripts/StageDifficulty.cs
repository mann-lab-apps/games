namespace MannLab.Games.Game10000
{
    public static class StageDifficulty
    {
        public static float GetTimeLimit(int stage)
        {
            if (stage <= 5)
            {
                return 10f;
            }

            if (stage <= 10)
            {
                return 8f;
            }

            if (stage <= 20)
            {
                return 6f;
            }

            if (stage <= 35)
            {
                return 4.5f;
            }

            if (stage <= 50)
            {
                return 3.5f;
            }

            return 2.5f;
        }
    }
}

