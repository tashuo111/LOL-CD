using Timer.Services;
using Xunit;
namespace Timer.Tests;
public class HotkeyTests
{
    [Fact] public void SendingWaitsUntilShortcutAndModifiersAreReleased()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("Ctrl+F6"), () => calls++, true);
        state.Process(0xA2, 0, true, false)?.Invoke();
        state.Process(0x75, 2, true, false)?.Invoke();
        state.Process(0x75, 2, true, false)?.Invoke(); Assert.Equal(0, calls);
        state.Process(0x75, 2, false, false)?.Invoke(); Assert.Equal(0, calls);
        state.Process(0xA2, 2, false, false)?.Invoke(); Assert.Equal(1, calls);
        state.Process(0xA2, 0, false, false)?.Invoke(); Assert.Equal(1, calls);
    }
    [Fact] public void LaterKeyCancelsPreviousSendShortcutLikeLastActive()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("F6"), () => calls++, true);
        state.Process(0x75, 0, true, false)?.Invoke(); state.Process(0x41, 0, true, false)?.Invoke();
        state.Process(0x41, 0, false, false)?.Invoke(); state.Process(0x75, 0, false, false)?.Invoke();
        Assert.Equal(0, calls);
    }
    [Fact] public void HoldF6OnlyTriggersOnceUntilReleased()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("F6"), () => calls++);
        state.Process(0x75, 0, true, false)?.Invoke();
        state.Process(0x75, 0, true, false)?.Invoke();
        Assert.Equal(1, calls);
        state.Process(0x75, 0, false, false);
        state.Process(0x75, 0, true, false)?.Invoke();
        Assert.Equal(2, calls);
    }
    [Fact] public void InjectedKeysDoNotTriggerActions()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("F6"), () => calls++);
        state.Process(0x75, 0, true, true)?.Invoke(); Assert.Equal(0, calls);
        state.Process(0x75, 0, true, false)?.Invoke(); Assert.Equal(1, calls);
    }
    [Fact] public void ModifiersMustMatch()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("Ctrl+F6"), () => calls++);
        state.Process(0x75, 0, true, false)?.Invoke(); Assert.Equal(0, calls);
        state.Process(0x75, 0, false, false);
        state.Process(0x75, 2, true, false)?.Invoke(); Assert.Equal(1, calls);
    }
    [Fact] public void RebindingInvalidatesPendingCallback()
    {
        var state = new HotkeyStateMachine(); int calls = 0;
        state.Register(HotkeyService.Parse("F6"), () => calls++);
        var pending = state.Process(0x75, 0, true, false); state.Clear(); pending?.Invoke(); Assert.Equal(0, calls);
    }
    [Fact] public void AkariNumpadPlusAliasWorks() => Assert.Equal(HotkeyService.Parse("Add"), HotkeyService.Parse("NumpadPlus"));
    [Fact] public void WindowsListenerInstallsWithoutExclusiveKeyReservation()
    {
        Exception failure = null;
        var thread = new Thread(() => {
            try { using var listener = new HotkeyService(IntPtr.Zero); foreach (var key in new TimerSettings().AllHotkeys) listener.Register(key, () => { }); }
            catch(Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(5))); Assert.Null(failure);
    }
}
