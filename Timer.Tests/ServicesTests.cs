using Timer.Services;
using Xunit;

namespace Timer.Tests;
public class TextGeneratorTests
{
    [Fact] public void MayhemUsesFloorsAndFiltersExpired()
    {
        Assert.Equal("1楼1145 5楼2000", generator.Generate(new Dictionary<string,long> { ["SUP"] = 1200, ["TOP"] = 705, ["JUG"] = 600 }, 600, true));
    }
    [Fact] public void MayhemDefaultRoundsUpWithoutAnnouncingEarly()
    {
        Assert.Equal(177, TimerUtil.RecoverySeconds(0, 0, new TimerSettings().MayhemCooldownSeconds[0]));
        Assert.Equal(492, TimerUtil.RecoverySeconds(315000, 0, 176.47));
        Assert.Equal(493, TimerUtil.RecoverySeconds(315900, 0, 176.47));
        Assert.Equal(615, TimerUtil.RecoverySeconds(315000, 0, 300));
    }
    private readonly TextGeneratorService generator = new();
    [Fact] public void TopOnly() => Assert.Equal("top1015", generator.Generate(new Dictionary<string,long> { ["TOP"] = 615 }, 315));
    [Fact] public void TopAndJungle() => Assert.Equal("top1015 jug1132", generator.Generate(new Dictionary<string,long> { ["TOP"] = 615, ["JUG"] = 692 }, 315));
    [Fact] public void AllRolesFixedOrder() => Assert.Equal("top1015 jug1132 mid1205 ad1240 sup1305", generator.Generate(new Dictionary<string,long> { ["SUP"] = 785, ["MID"] = 725, ["AD"] = 760, ["JUG"] = 692, ["TOP"] = 615 }, 315));
    [Theory] [InlineData(615)] [InlineData(616)] public void ExpiredTimeRemoved(long now) => Assert.Equal("", generator.Generate(new Dictionary<string,long> { ["TOP"] = 615 }, now));
    [Fact] public void Empty() => Assert.Equal("", generator.Generate(new Dictionary<string,long>(), 0));
    [Fact] public void Padding() => Assert.Equal("top0105", generator.Generate(new Dictionary<string,long> { ["TOP"] = 65 }, 0));
}
public class GameSendTests
{
    private sealed class FakeWindow : IGameWindowService
    {
        public IntPtr Handle = new(1); public bool Foreground = true;
        public IntPtr FindWindow() => Handle;
        public bool IsForeground(IntPtr handle) => Foreground;
    }
    private sealed class FakeInput : IGameInput
    {
        public List<string> Calls = new(); public bool Available = true; public Action OnEnter;
        public bool ModifiersDown() => false;
        public bool PressEnter() { Calls.Add("Enter"); OnEnter?.Invoke(); return Available; }
        public bool SendText(string text) { Calls.Add(text); return Available; }
    }
    [Fact] public async Task DisabledDoesNotInput()
    {
        var input = new FakeInput(); using var sender = new GameSendService(new FakeWindow(), input, () => new TimerSettings());
        Assert.Equal(GameSendResult.Disabled, await sender.SendAsync("TOP")); Assert.Empty(input.Calls);
    }
    [Theory] [InlineData(true)] [InlineData(false)] public async Task MissingOrBackgroundDoesNotInput(bool missing)
    {
        var input = new FakeInput(); var window = new FakeWindow { Handle = missing ? IntPtr.Zero : new IntPtr(1), Foreground = false };
        using var sender = new GameSendService(window, input, () => new TimerSettings { EnableGameSend = true });
        Assert.Equal(missing ? GameSendResult.GameNotFound : GameSendResult.GameNotForeground, await sender.SendAsync("TOP")); Assert.Empty(input.Calls);
    }
    [Fact] public async Task OneSequenceAndDuplicateSuppression()
    {
        var input = new FakeInput(); using var sender = new GameSendService(new FakeWindow(), input, () => new TimerSettings { EnableGameSend = true });
        var first = sender.SendAsync("TOP"); Assert.Equal(GameSendResult.Busy, await sender.SendAsync("TOP"));
        Assert.Equal(GameSendResult.Success, await first); Assert.Equal(GameSendResult.Busy, await sender.SendAsync("TOP"));
        Assert.Equal(new[] { "Enter", "TOP", "Enter" }, input.Calls);
    }
    [Fact] public async Task StopsWhenFocusLost()
    {
        var window = new FakeWindow(); var input = new FakeInput { OnEnter = () => window.Foreground = false };
        using var sender = new GameSendService(window, input, () => new TimerSettings { EnableGameSend = true });
        Assert.Equal(GameSendResult.GameNotForeground, await sender.SendAsync("TOP")); Assert.Equal(new[] { "Enter" }, input.Calls);
    }
    [Fact] public async Task InputFailureDoesNotRetry()
    {
        var input = new FakeInput { Available = false }; using var sender = new GameSendService(new FakeWindow(), input, () => new TimerSettings { EnableGameSend = true });
        Assert.Equal(GameSendResult.InputUnavailable, await sender.SendAsync("TOP")); Assert.Single(input.Calls);
    }
    [Fact] public async Task ClosingCancelsPendingInput()
    {
        var input = new FakeInput(); var sender = new GameSendService(new FakeWindow(), input, () => new TimerSettings { EnableGameSend = true });
        var result = sender.SendAsync("TOP"); sender.Dispose(); Assert.Equal(GameSendResult.Cancelled, await result); Assert.Single(input.Calls);
    }
    [Theory] [InlineData("")] [InlineData("\nTOP")] public async Task InvalidTextNeverInputs(string text)
    {
        var input = new FakeInput(); using var sender = new GameSendService(new FakeWindow(), input, () => new TimerSettings { EnableGameSend = true });
        Assert.Equal(GameSendResult.Error, await sender.SendAsync(text)); Assert.Empty(input.Calls);
    }
    [Fact] public void DuplicateHotkeysRejected() => Assert.Throws<ArgumentException>(() => HotkeyService.Validate(new[] { "Ctrl+F6", "ctrl+f6" }));
    [Fact] public void DefaultsValid() => HotkeyService.Validate(new TimerSettings().AllHotkeys);
}


