using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Timer.Services;
namespace Timer;
public partial class MainWindow
{
    private readonly StackPanel[] floorEditors = new StackPanel[5];
    private bool modeInitializing;
    private string PositionName(int index) => settings.MayhemMode ? $"{index + 1}楼" : TextGeneratorService.Roles[index];
    private double CooldownFor(int index, bool boot, bool star) => settings.MayhemMode ? settings.MayhemCooldownSeconds[index] : TimerUtil.FlashCooldownSeconds(index, boot, star);
    private void InitializeMode()
    {
        modeInitializing = true;
        ModeSelector.SelectedIndex = settings.MayhemMode ? 1 : 0;
        var grid = (Grid)Content;
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(489, 63 + i * 40, 0, 0) };
            row.Children.Add(new TextBlock { Text = "CD 秒", FontSize = 16, VerticalAlignment = VerticalAlignment.Center, Width = 65 });
            var field = new TextBox { Text = settings.MayhemCooldownSeconds[i].ToString("0.##", CultureInfo.InvariantCulture), Width = 105, FontSize = 18, ToolTip = "填写游戏内实际闪现 CD（含装备/强化）；默认 176.47 秒。按 Enter 或离开输入框保存。" };
            void Apply()
            {
                if (!double.TryParse(field.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value) || value < 0.1 || value > 600)
                { field.Text = settings.MayhemCooldownSeconds[index].ToString("0.##", CultureInfo.InvariantCulture); ShowStatus("CD 必须为 0.1–600 秒。"); return; }
                settings.MayhemCooldownSeconds[index] = value; SaveSettings(); RefreshTeamText();
            }
            field.LostKeyboardFocus += (_, _) => Apply();
            field.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Apply(); e.Handled = true; } };
            row.Children.Add(field); grid.Children.Add(row); floorEditors[i] = row;
        }
        modeInitializing = false; UpdateModeLabels();
    }
    private void ModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (modeInitializing || settings == null || flowWindow == null) return;
        bool mayhem = ModeSelector.SelectedIndex == 1;
        if (settings.MayhemMode == mayhem) return;
        settings.MayhemMode = mayhem;
        TopClear_Click(null, null); JugClear_Click(null, null); MidClear_Click(null, null); BotClear_Click(null, null); SupClear_Click(null, null);
        UpdateModeLabels(); SaveSettings();
        ShowStatus("模式已切换，旧 CD 记录已清空；比赛时间保留。", 8);
    }
    private void UpdateModeLabels()
    {
        ModeSelector.SelectedIndex = settings.MayhemMode ? 1 : 0;
        var buttons = new[] { TopButton, JugButton, MidButton, BotButton, SupButton };
        var names = new[] { "上路", "打野", "中路", "下路", "辅助" };
        var boots = new[] { TopBoot, JugBoot, MidBoot, BotBoot, SupBoot };
        var stars = new[] { TopStar, JugStar, MidStar, BotStar, SupStar };
        for (int i = 0; i < 5; i++)
        {
            buttons[i].Content = settings.MayhemMode ? PositionName(i) : names[i];
            boots[i].ToolTip = $"明朗鞋：+{(i == 2 ? 20 : 10)} 急速；与星界急速相加后计算闪现 CD。";
            stars[i].ToolTip = "星界：+18 急速。闪现 CD = 300 ÷ (1 + 总急速 ÷ 100)。";
            boots[i].Visibility = stars[i].Visibility = settings.MayhemMode ? Visibility.Collapsed : Visibility.Visible;
            if (floorEditors[i] != null) floorEditors[i].Visibility = settings.MayhemMode ? Visibility.Visible : Visibility.Collapsed;
        }
        UpdateFlowLabels(); UpdateHotkeyLabels(); RefreshTeamText();
        ModeSelector.ToolTip = "海克斯模式基础闪现约 176.47 秒（300÷1.7）；装备/强化请按各楼游戏内实际 CD 修改。切换会清空 CD 记录。";
    }
    private void UpdateFlowLabels()
    {
        if (flowWindow == null) return;
        var labels = new[] { flowWindow.Top_Copy, flowWindow.Jug_Copy, flowWindow.Mid_Copy, flowWindow.Bot_Copy, flowWindow.Sup_Copy };
        var names = new[] { "上单", "打野", "中单", "下路", "辅助" };
        for (int i = 0; i < 5; i++) labels[i].Content = settings.MayhemMode ? PositionName(i) : names[i];
    }
}
