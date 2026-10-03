using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Timer.Services;
using Xunit;

namespace Timer.Tests;
public class WindowTests
{
    [Fact] public void PortablePackageUsesAdjacentSettingsInsteadOfOldComputerSettings()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "portable.mode"), "");
            string resolved = SettingsService.ResolvePath(folder, "old-profile");
            Assert.Equal(Path.Combine(folder, "settings.json"), resolved);
            var service = new SettingsService(resolved);
            service.Save(new TimerSettings { GameSendHotkey = "X", RecordHotkeys = new[] { "Ctrl+D1", "Ctrl+D2", "Ctrl+D3", "Ctrl+D4", "Ctrl+D5" } });
            Assert.Equal("X", service.Load().GameSendHotkey);
            Assert.Equal("Ctrl+D1", service.Load().RecordHotkeys[0]);
        }
        finally { File.Delete(Path.Combine(folder, "settings.json")); File.Delete(Path.Combine(folder, "portable.mode")); Directory.Delete(folder); }
    }
    [Fact] public void MainWindowRendersAndManualRecordsReset()
    {
        Exception failure = null;
        var thread = new Thread(() => {
            MainWindow window = null;
            try
            {
                window = new MainWindow(new SettingsService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.Contains("全局监听中", ((TextBlock)window.FindName("HotkeyStatus")).Text);
                void Invoke(string name) => typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, new object[] { null, null });
                string Text() => ((TextBox)window.FindName("TeamText")).Text;
                Assert.Equal("", Text());
                Invoke("TopButton_Click"); Assert.Matches("^top050[01]$", Text());
                Invoke("JugButton_Click"); Assert.Matches("^top050[01] jug050[01]$", Text());
                Invoke("TopClear_Click"); Assert.Matches("^jug050[01]$", Text());
                Invoke("GameButton_Click"); Assert.Equal("", Text());
                var settings = (TimerSettings)typeof(MainWindow).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);
                settings.MayhemMode = true;
                typeof(MainWindow).GetMethod("UpdateModeLabels", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, null);
                Assert.Equal("1楼", ((Button)window.FindName("TopButton")).Content);
                Invoke("TopButton_Click"); Assert.Matches("^1楼025[78]$", Text());
                long recordedAt = (long)typeof(MainWindow).GetField("TopStartTime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);
                typeof(MainWindow).GetField("GameStartTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(window, recordedAt - 300000);
                Invoke("Toptimer_Tick");
                typeof(MainWindow).GetMethod("RefreshTeamText", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, null);
                Assert.Equal("1楼0757", Text());
                Assert.Contains("07:57", ((Label)window.FindName("TopLabel")).Content.ToString());
                settings.MayhemCooldownSeconds[0] = 120;
                Invoke("Toptimer_Tick");
                typeof(MainWindow).GetMethod("RefreshTeamText", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, null);
                Assert.Equal("1楼0700", Text());
                Assert.Contains("07:00", ((Label)window.FindName("TopLabel")).Content.ToString());
                Invoke("GameButton_Click"); Assert.Equal("", Text());
                window.Measure(new Size(693, 560)); window.Arrange(new Rect(0, 0, 693, 560)); window.UpdateLayout();
                var content = (FrameworkElement)window.Content;
                content.Measure(new Size(675, 580)); content.Arrange(new Rect(0, 0, 675, 580)); content.UpdateLayout();
                Assert.True(((TextBox)window.FindName("TeamText")).ActualWidth > 500);
                string path = Environment.GetEnvironmentVariable("TIMER_PREVIEW_PATH");
                if (!string.IsNullOrEmpty(path))
                {
                    var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(path); encoder.Save(file);
                }
            }
            catch (Exception e) { failure = e; }
            finally { window?.Close(); System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "WPF render timed out");
        Assert.Null(failure);
    }
    [Fact] public void SettingsRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var service = new SettingsService(path);
            service.Save(new TimerSettings { EnableGameSend = true, GameSendHotkey = "Ctrl+F8", CopyHotkey = "F9", GameSendCooldownMs = 2300 });
            var loaded = service.Load();
            Assert.True(loaded.EnableGameSend); Assert.Equal("Ctrl+F8", loaded.GameSendHotkey); Assert.Equal("F9", loaded.CopyHotkey); Assert.Equal(2300, loaded.GameSendCooldownMs);
            File.WriteAllText(path, "{broken"); Assert.False(service.Load().EnableGameSend); Assert.NotNull(service.LoadWarning);
        }
        finally { File.Delete(path); }
    }
}



