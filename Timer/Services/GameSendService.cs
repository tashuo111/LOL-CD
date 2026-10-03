using System;
using System.Threading;
using System.Threading.Tasks;
namespace Timer.Services;
public enum GameSendResult { Success, GameNotForeground, GameNotFound, InputUnavailable, Cancelled, Error, Busy, Disabled }
public interface IGameSendService { bool CanSend(); Task<GameSendResult> SendAsync(string text); }
public interface IGameInput { bool ModifiersDown(); bool PressEnter(); bool SendText(string text); }
public sealed class GameSendService : IGameSendService, IDisposable
{
    private readonly IGameWindowService windows;
    private readonly IGameInput input;
    private readonly Func<TimerSettings> settings;
    private readonly CancellationTokenSource lifetime = new();
    private int sending;
    private long? lastAttempt;
    public string LastStage { get; private set; } = "未发送";
    public string InputDiagnostic => (input as WindowsGameInput)?.LastDiagnostic ?? "测试输入";
    public GameSendService(IGameWindowService windows, IGameInput input, Func<TimerSettings> settings)
    { this.windows = windows; this.input = input; this.settings = settings; }
    public bool CanSend() => settings().EnableGameSend && windows.IsForeground(windows.FindWindow());
    public async Task<GameSendResult> SendAsync(string text)
    {
        if (!settings().EnableGameSend) return GameSendResult.Disabled;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 200 || text.Contains('\n') || text.Contains('\r')) return GameSendResult.Error;
        if (Interlocked.CompareExchange(ref sending, 1, 0) != 0) return GameSendResult.Busy;
        try
        {
            LastStage = "检查发送间隔及游戏前台";
            if (lastAttempt.HasValue && Environment.TickCount64 - lastAttempt.Value < Math.Clamp(settings().GameSendCooldownMs, 1000, 5000)) return GameSendResult.Busy;
            IntPtr handle = windows.FindWindow();
            if (handle == IntPtr.Zero) return GameSendResult.GameNotFound;
            if (!windows.IsForeground(handle)) return GameSendResult.GameNotForeground;
            lastAttempt = Environment.TickCount64;
            for (int i = 0; input.ModifiersDown() && i < 20; i++) await Task.Delay(25, lifetime.Token);
            GameSendResult Check() => lifetime.IsCancellationRequested || !settings().EnableGameSend ? GameSendResult.Cancelled :
                !windows.IsForeground(handle) ? GameSendResult.GameNotForeground : input.ModifiersDown() ? GameSendResult.Cancelled : GameSendResult.Success;
            var check = Check(); if (check != GameSendResult.Success) return check;
            LastStage = "打开聊天：Enter";
            if (!input.PressEnter()) return GameSendResult.InputUnavailable;
            await Task.Delay(20, lifetime.Token);
            check = Check(); if (check != GameSendResult.Success) return check;
            LastStage = "输入文本：Unicode";
            if (!input.SendText(text)) return GameSendResult.InputUnavailable;
            await Task.Delay(20, lifetime.Token);
            check = Check(); if (check != GameSendResult.Success) return check;
            LastStage = "提交聊天：Enter";
            return input.PressEnter() ? GameSendResult.Success : GameSendResult.InputUnavailable;
        }
        catch (OperationCanceledException) { return GameSendResult.Cancelled; }
        catch (Exception) { return GameSendResult.Error; }
        finally { Interlocked.Exchange(ref sending, 0); }
    }
    public void Dispose() { lifetime.Cancel(); }
}
