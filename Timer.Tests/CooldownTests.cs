using Xunit;

namespace Timer.Tests;

public class CooldownTests
{
    [Theory]
    [InlineData(0, false, false, 300)]
    [InlineData(1, false, false, 300)]
    [InlineData(2, false, false, 300)]
    [InlineData(3, false, false, 300)]
    [InlineData(4, false, false, 300)]
    [InlineData(0, false, true, 254.23728813559322)]
    [InlineData(1, false, true, 254.23728813559322)]
    [InlineData(2, false, true, 254.23728813559322)]
    [InlineData(3, false, true, 254.23728813559322)]
    [InlineData(4, false, true, 254.23728813559322)]
    [InlineData(0, true, false, 272.7272727272727)]
    [InlineData(1, true, false, 272.7272727272727)]
    [InlineData(2, true, false, 250)]
    [InlineData(3, true, false, 272.7272727272727)]
    [InlineData(4, true, false, 272.7272727272727)]
    [InlineData(0, true, true, 234.375)]
    [InlineData(1, true, true, 234.375)]
    [InlineData(2, true, true, 217.3913043478261)]
    [InlineData(3, true, true, 234.375)]
    [InlineData(4, true, true, 234.375)]
    public void FlashCooldownUsesAdditiveHaste(int role, bool boots, bool cosmicInsight, double expected)
    {
        double cooldown = TimerUtil.FlashCooldownSeconds(role, boots, cosmicInsight);
        Assert.Equal(expected, cooldown, 9);
        Assert.Equal((long)System.Math.Ceiling(300 + expected), TimerUtil.RecoverySeconds(300000, 0, cooldown));
    }
}
