using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timer.Services;
namespace Timer;
public sealed class HotkeyPickerWindow : Window
{
    public string SelectedHotkey { get; private set; }
    public HotkeyPickerWindow()
    {
        Title = "选择快捷键"; Width = 370; Height = 165; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var hint = new TextBlock { Text = "请按要使用的快捷键\n支持 Ctrl / Alt / Shift 组合，Esc 取消", Margin = new Thickness(22), FontSize = 16, TextWrapping = TextWrapping.Wrap };
        Content = hint;
        PreviewKeyDown += (_, e) => {
            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Escape) { DialogResult = false; return; }
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
            var parts = new List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            parts.Add(key.ToString());
            try { SelectedHotkey = string.Join("+", parts); HotkeyService.Parse(SelectedHotkey); DialogResult = true; }
            catch (ArgumentException error) { hint.Text = error.Message; }
        };
    }
}
