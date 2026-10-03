using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Timer.Services;
public sealed class TimerSettings
{
    public bool MayhemMode { get; set; }
    public double[] MayhemCooldownSeconds { get; set; } = { 176.47, 176.47, 176.47, 176.47, 176.47 };
    public bool EnableGameSend { get; set; } = false;
    public bool VoiceEnabled { get; set; } = false;
    public string GameSendHotkey { get; set; } = "F6";
    public string CopyHotkey { get; set; } = "F7";
    public int GameSendCooldownMs { get; set; } = 1000;
    public string[] RecordHotkeys { get; set; } = { "F1", "F2", "F3", "F4", "F5" };
    public string ResetHotkey { get; set; } = "Decimal";
    public string AddHotkey { get; set; } = "Add";
    public string SubtractHotkey { get; set; } = "Subtract";
    public string OverlayHotkey { get; set; } = "NumPad0";
    public double OverlayTop { get; set; } = 125;
    public double OverlayLeft { get; set; } = 80;
    [JsonIgnore] public IEnumerable<string> AllHotkeys => new[] { RecordHotkeys[0], RecordHotkeys[1], RecordHotkeys[2], RecordHotkeys[3], RecordHotkeys[4], GameSendHotkey, CopyHotkey, ResetHotkey, AddHotkey, SubtractHotkey, OverlayHotkey };
}
public sealed class SettingsService
{
    public string FilePath { get; }
    public SettingsService(string filePath = null) => FilePath = filePath ?? ResolvePath(AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    public static string ResolvePath(string programDirectory, string localDataDirectory) =>
        File.Exists(Path.Combine(programDirectory, "portable.mode"))
            ? Path.Combine(programDirectory, "settings.json")
            : Path.Combine(localDataDirectory, "LkfunTimer", "settings.json");
    public string LoadWarning { get; private set; }
    public TimerSettings Load()
    {
        try
        {
            var settings = File.Exists(FilePath) ? JsonSerializer.Deserialize<TimerSettings>(File.ReadAllText(FilePath)) : ImportLegacySettings();
            if (settings == null || settings.RecordHotkeys == null || settings.RecordHotkeys.Length != 5) throw new InvalidDataException("设置格式无效");
            settings.GameSendCooldownMs = Math.Clamp(settings.GameSendCooldownMs, 1000, 5000);
            if (settings.MayhemCooldownSeconds == null || settings.MayhemCooldownSeconds.Length != 5)
                settings.MayhemCooldownSeconds = new TimerSettings().MayhemCooldownSeconds;
            for (int i = 0; i < 5; i++)
                if (!double.IsFinite(settings.MayhemCooldownSeconds[i]) || settings.MayhemCooldownSeconds[i] < 0.1 || settings.MayhemCooldownSeconds[i] > 600)
                    settings.MayhemCooldownSeconds[i] = 176.47;
            HotkeyService.Validate(settings.AllHotkeys);
            return settings;
        }
        catch (Exception e) when (e is IOException || e is JsonException || e is ArgumentException || e is UnauthorizedAccessException)
        { LoadWarning = "设置读取失败，已使用默认值：" + e.Message; return new TimerSettings(); }
    }
    private static TimerSettings ImportLegacySettings()
    {
        var settings = new TimerSettings();
        string path = Path.Combine(AppContext.BaseDirectory, "conf.ini");
        if (!File.Exists(path)) return settings;
        foreach (string line in File.ReadLines(path))
        {
            var pair = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2) continue;
            switch (pair[0])
            {
                case "Top": settings.RecordHotkeys[0] = pair[1]; break;
                case "Jug": settings.RecordHotkeys[1] = pair[1]; break;
                case "Mid": settings.RecordHotkeys[2] = pair[1]; break;
                case "Bot": settings.RecordHotkeys[3] = pair[1]; break;
                case "Sup": settings.RecordHotkeys[4] = pair[1]; break;
                case "Add": settings.AddHotkey = pair[1]; break;
                case "Subtract": settings.SubtractHotkey = pair[1]; break;
                case "Decimal": settings.ResetHotkey = pair[1]; break;
                case "Switch": settings.OverlayHotkey = pair[1]; break;
                case "FlowWindowTop": if (double.TryParse(pair[1], out var top)) settings.OverlayTop = top; break;
                case "FlowWindowLeft": if (double.TryParse(pair[1], out var left)) settings.OverlayLeft = left; break;
            }
        }
        return settings;
    }
    public void Save(TimerSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(FilePath + ".tmp", FilePath, true);
    }
}
