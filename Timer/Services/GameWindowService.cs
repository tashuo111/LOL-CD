using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Timer.Services;
public interface IGameWindowService { IntPtr FindWindow(); bool IsForeground(IntPtr handle); }
public sealed class GameWindowService : IGameWindowService
{
    public IntPtr FindWindow()
    {
        foreach (var process in Process.GetProcessesByName("League of Legends"))
        {
            using (process)
            {
                try { if (process.MainWindowHandle != IntPtr.Zero) return process.MainWindowHandle; }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }
        return IntPtr.Zero;
    }
    public bool IsForeground(IntPtr handle) => handle != IntPtr.Zero && IsWindow(handle) && GetForegroundWindow() == handle;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
}
