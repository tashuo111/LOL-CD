using System;
namespace Timer;
public static class TimerUtil
{
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

