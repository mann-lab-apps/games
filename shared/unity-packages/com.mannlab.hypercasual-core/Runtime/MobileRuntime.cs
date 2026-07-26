using UnityEngine;

namespace MannLab.HyperCasual
{
    public static class MobileRuntime
    {
        public static void ApplyDefaults(int targetFrameRate = 60)
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = false;
        }
    }
}

