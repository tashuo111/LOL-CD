using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Timer.Services;

namespace Timer;
public sealed class SendSettingsWindow : Window
{
    public SendSettingsWindow(TimerSettings original, Func<string> preview, GameWindowService windows, Func<TimerSettings, string> save)
    {
        Title = "游戏内发送设置"; Width = 510; Height = 720; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(22) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var enabled = new CheckBox { Content = "启用游戏内快捷发送", IsChecked = original.EnableGameSend, FontSize = 18 };
        panel.Children.Add(enabled);
        panel.Children.Add(new TextBlock { Text = "此功能通过用户主动快捷键触发。不同游戏环境可能限制第三方输入。无法保证任何第三方工具不存在账号风险。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) });
        var state = new TextBlock { Text = "状态：未检测" }; panel.Children.Add(state);
        var detect = new Button { Content = "检测游戏状态", Margin = new Thickness(0, 6, 0, 10) };
        detect.Click += (_, _) => state.Text = windows.FindWindow() == IntPtr.Zero ? "状态：游戏未运行" : "状态：游戏运行中";
        panel.Children.Add(detect);
        panel.Children.Add(new TextBlock { Text = "快捷键可用 F8、Ctrl+F8 等格式，不能重复。", Margin = new Thickness(0, 0, 0, 8) });
        var fields = new List<TextBox>();
        var names = new[] { "TOP 记录", "JUG 记录", "MID 记录", "AD 记录", "SUP 记录", "发送到游戏", "仅复制", "重置游戏", "增加时长", "减少时长", "显示/隐藏浮窗" };
        var keys = original.AllHotkeys.ToArray();
        for (int i = 0; i < names.Length; i++)
        {
            var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock { Text = names[i], Width = 145, VerticalAlignment = VerticalAlignment.Center });
            var field = new TextBox { Text = keys[i], Padding = new Thickness(3) };
            var choose = new Button { Content = "选择", Width = 55, Margin = new Thickness(4, 0, 0, 0) };
            DockPanel.SetDock(choose, Dock.Right); row.Children.Add(choose);
            choose.Click += (_, _) => { var picker = new HotkeyPickerWindow { Owner = this }; if (picker.ShowDialog() == true) field.Text = picker.SelectedHotkey; };
            row.Children.Add(field); fields.Add(field); panel.Children.Add(row);
        }
        panel.Children.Add(new TextBlock { Text = "最短发送间隔（1000–5000 ms）", Margin = new Thickness(0, 8, 0, 4) });
        var interval = new TextBox { Text = original.GameSendCooldownMs.ToString() }; panel.Children.Add(interval);
        var generated = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) }; panel.Children.Add(generated);
        var test = new Button { Content = "测试生成文本", Margin = new Thickness(0, 6, 0, 6) }; panel.Children.Add(test);
        test.Click += (_, _) => generated.Text = preview();
        panel.Children.Add(new TextBlock { Text = "发送前请保持 LOL 在前台、聊天输入框关闭。普通输入只能确认提交，不能验证游戏是否收到；若未收到，请用复制模式。", TextWrapping = TextWrapping.Wrap });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Firebrick, Margin = new Thickness(0, 6, 0, 6) }; panel.Children.Add(error);
        var apply = new Button { Content = "保存", Padding = new Thickness(8) }; panel.Children.Add(apply);
        apply.Click += (_, _) => {
            try
            {
                var values = fields.Select(f => f.Text.Trim()).ToArray(); HotkeyService.Validate(values);
                if (!int.TryParse(interval.Text, out int delay) || delay < 1000 || delay > 5000) throw new ArgumentException("发送间隔必须为 1000–5000 ms。");
                var candidate = JsonSerializer.Deserialize<TimerSettings>(JsonSerializer.Serialize(original));
                candidate.RecordHotkeys = values.Take(5).ToArray(); candidate.GameSendHotkey = values[5]; candidate.CopyHotkey = values[6];
                candidate.ResetHotkey = values[7]; candidate.AddHotkey = values[8]; candidate.SubtractHotkey = values[9]; candidate.OverlayHotkey = values[10];
                candidate.EnableGameSend = enabled.IsChecked == true; candidate.GameSendCooldownMs = delay;
                string failure = save(candidate); if (failure != null) { error.Text = failure; return; }
                DialogResult = true;
            }
            catch (Exception ex) { error.Text = ex.Message; }
        };
    }
}
