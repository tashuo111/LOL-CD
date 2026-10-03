using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Timer.Services;

namespace Timer;
public partial class MainWindow
{
    private readonly SettingsService settingsService;
    private TimerSettings settings;
    private readonly TextGeneratorService textGenerator = new();
    private readonly GameWindowService gameWindows = new();
    private readonly ClipboardService clipboard = new();
    private GameSendService gameSend;
    private readonly HashSet<string> recorded = new();
    private readonly DispatcherTimer statusTimer = new();
    private bool settingsOpen;

    private string RefreshTeamText()
    {
        var times = new Dictionary<string, long>();
        void Add(string name, string role, long start, CheckBox boot, CheckBox star)
        {
            if (recorded.Contains(name)) times[role] = TimerUtil.RecoverySeconds(start, GameStartTime, CooldownFor(Array.IndexOf(TextGeneratorService.Roles.ToArray(), role), boot.IsChecked == true, star.IsChecked == true));
        }
        Add("Top", "TOP", TopStartTime, TopBoot, TopStar);
        Add("Jug", "JUG", JugStartTime, JugBoot, JugStar);
        Add("Mid", "MID", MidStartTime, MidBoot, MidStar);
        Add("Bot", "AD", BotStartTime, BotBoot, BotStar);
        Add("Sup", "SUP", SupStartTime, SupBoot, SupStar);
        return TeamText.Text = textGenerator.Generate(times, Math.Max(0, (Environment.TickCount64 - GameStartTime) / 1000), settings?.MayhemMode == true);
    }
    private void Generate_Click(object sender, RoutedEventArgs e) => GenerateAndCopy();
    private void Copy_Click(object sender, RoutedEventArgs e) => GenerateAndCopy();
    private string GenerateAndCopy()
    {
        string text = RefreshTeamText();
        if (text.Length == 0) { ShowStatus("没有正在冷却的记录，未复制或发送。"); return text; }
        ShowStatus(clipboard.Copy(text) ? "已生成并自动复制" : "已生成；剪贴板被占用，请再次生成复制。");
        return text;
    }
    // This button is explanatory: only the registered user shortcut can perform game input.
    private void SendHint_Click(object sender, RoutedEventArgs e) => ShowStatus(!settings.EnableGameSend ? "游戏内发送未启用。请在发送设置中开启。" : $"请切回 LOL，关闭聊天输入框，再按 {settings.GameSendHotkey} 发送一次。");
    private async void SendFromHotkey()
    {
        string text = RefreshTeamText();
        if (text.Length == 0) { ShowStatus("没有正在冷却的记录，未发送。"); return; }
        var result = await gameSend.SendAsync(text);
        HotkeyStatus.Text = $"快捷键：{settings.GameSendHotkey} 已松开 → {result}（{gameSend.LastStage}）";
        try
        {
            string folder = System.IO.Path.GetDirectoryName(settingsService.FilePath);
            System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "last-send.json"), System.Text.Json.JsonSerializer.Serialize(new {
                Time = DateTimeOffset.Now, Hotkey = settings.GameSendHotkey, Result = result.ToString(),
                Stage = gameSend.LastStage, Input = gameSend.InputDiagnostic, TextLength = text.Length
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (System.IO.IOException) { }
        catch (UnauthorizedAccessException) { }
        ShowStatus(result switch {
            GameSendResult.Success => "已发送（输入已提交）",
            GameSendResult.Disabled => "游戏内发送未启用。",
            GameSendResult.GameNotFound => "游戏未运行，未发送。",
            GameSendResult.GameNotForeground => "LOL 当前不是前台窗口，未发送。",
            GameSendResult.InputUnavailable => "当前游戏环境不允许发送，请使用复制模式。",
            GameSendResult.Busy => "正在发送或间隔未到，已忽略重复按键。",
            GameSendResult.Cancelled => "发送已取消，请使用复制模式。",
            _ => "发送失败，请使用复制模式。"
        }, result == GameSendResult.Success ? 1 : 7);
    }
    private void ShowStatus(string message, int seconds = 5)
    {
        statusTimer.Stop(); statusTimer.Tick -= ClearStatus;
        SendStatus.Text = message; statusTimer.Interval = TimeSpan.FromSeconds(seconds);
        statusTimer.Tick += ClearStatus; statusTimer.Start();
    }
    private void ClearStatus(object sender, EventArgs e) { statusTimer.Stop(); SendStatus.Text = ""; }
    private void BindHotkeys(TimerSettings value)
    {
        hotkeys.Clear();
        if (chkEnableKey.IsChecked != true) return;
        Action[] actions = { () => TopButton_Click(null, null), () => JugButton_Click(null, null), () => MidButton_Click(null, null), () => BotButton_Click(null, null), () => SupButton_Click(null, null), SendFromHotkey, () => Copy_Click(null, null), () => GameButton_Click(null, null), () => GameAdd_Click(null, null), () => GameSubtract_Click(null, null), () => Switch(null, null) };
        var keys = value.AllHotkeys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            var action = actions[i];
            hotkeys.Register(keys[i], () => { if (chkEnableKey.IsChecked == true && !settingsOpen) action(); }, onRelease: i == 5);
        }
    }
    private void RegisterHotkeys()
    {
        try { BindHotkeys(settings); }
        catch (Exception e) { hotkeys?.Clear(); HotkeyStatus.Text = "快捷键启动失败：" + e.Message; ShowStatus(e.Message + "；请在发送设置中修改。", 30); return; }
        hotkeys.Triggered -= OnHotkeyTriggered;
        hotkeys.Triggered += OnHotkeyTriggered;
        HotkeyStatus.Text = chkEnableKey.IsChecked == true ? "快捷键：全局监听中" : "快捷键：已关闭";
        UpdateHotkeyLabels();
    }
    private void OnHotkeyTriggered(string key) => HotkeyStatus.Text = "快捷键：已识别 " + key;
    private void ToggleHotkeys_Click(object sender, RoutedEventArgs e) => RegisterHotkeys();
    private void UpdateHotkeyLabels()
    {
        CopyButton.Content = "生成文本 " + settings.CopyHotkey;
        SelectedSendHotkey.Text = settings.GameSendHotkey;
        HotkeyHint.FontSize = 16;
        HotkeyHint.Content = $"{settings.ResetHotkey} 重置  {settings.AddHotkey} +时长  {settings.SubtractHotkey} -时长  {settings.OverlayHotkey} 浮窗\n" +
            string.Join("  ", settings.RecordHotkeys.Select((key, i) => key + " " + PositionName(i)));
    }
    private void SaveSettings()
    {
        try { settingsService.Save(settings); }
        catch (Exception e) { ShowStatus("设置保存失败：" + e.Message); }
    }
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        settingsOpen = true;
        try
        {
            var dialog = new SendSettingsWindow(settings, GenerateAndCopy, gameWindows, candidate => {
                try
                {
                    BindHotkeys(candidate);
                    settingsService.Save(candidate);
                    settings = candidate;
                    UpdateHotkeyLabels();
                    return null;
                }
                catch (Exception error)
                {
                    try { BindHotkeys(settings); } catch { hotkeys.Clear(); }
                    return error.Message;
                }
            }) { Owner = this };
            dialog.ShowDialog();
        }
        finally { settingsOpen = false; }
    }
    private void SelectSendHotkey_Click(object sender, RoutedEventArgs e)
    {
        settingsOpen = true;
        try
        {
            var picker = new HotkeyPickerWindow { Owner = this };
            if (picker.ShowDialog() != true) return;
            string previous = settings.GameSendHotkey;
            try
            {
                settings.GameSendHotkey = picker.SelectedHotkey;
                HotkeyService.Validate(settings.AllHotkeys);
                settingsService.Save(settings);
                RegisterHotkeys();
                ShowStatus("发送快捷键已设置为 " + settings.GameSendHotkey);
            }
            catch (Exception error) { settings.GameSendHotkey = previous; ShowStatus(error.Message, 12); }
        }
        finally { settingsOpen = false; }
    }
    private void VoiceToggle_Click(object sender, RoutedEventArgs e)
    {
        settings.VoiceEnabled = chkVoice.IsChecked == true;
        SaveSettings();
        if (chkVoice.IsChecked == true) voice.Speak("语音播报已开启"); else voice.Stop();
    }
    private void VoiceTest_Click(object sender, RoutedEventArgs e)
    {
        chkVoice.IsChecked = true;
        settings.VoiceEnabled = true; SaveSettings();
        voice.Speak("AD 闪现已就绪");
        ShowStatus("语音测试：" + voice.VoiceName + "；输出到 Windows 默认扬声器。", 10);
    }
}

