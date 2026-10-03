using System;
namespace Timer;
public static class TimerUtil
{
    public static double FlashCooldownSeconds(int roleIndex, bool boots, bool cosmicInsight)
    {
        int haste = (boots ? (roleIndex == 2 ? 20 : 10) : 0) + (cosmicInsight ? 18 : 0);
        return 300.0 / (1.0 + haste / 100.0);
    }

    // Whole-second estimates round up so we do not announce readiness early.
    public static long RecoverySeconds(long start, long gameStart, double cooldownSeconds) => (long)Math.Ceiling((start - gameStart) / 1000.0 + cooldownSeconds);
    public static string ChangeTimeContent(long start, long gameStart, double cooldownSeconds, out bool isReady)
    {
        long recovery = RecoverySeconds(start, gameStart, cooldownSeconds);
        long remaining = recovery - Math.Max(0, (Environment.TickCount64 - gameStart) / 1000);
        isReady = remaining <= 0;
        return isReady ? "就绪" : $"{remaining}秒（{recovery / 60:00}:{recovery % 60:00}）";
    }
}

