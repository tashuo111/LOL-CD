using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
namespace Timer.Services;

// Ordinary Windows keyboard listener, like Akari. No exclusive RegisterHotKey
// reservations, game hooks, injection into processes, or elevated input.
public sealed class HotkeyService : IDisposable
{
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly KeyboardProc callback;
    private readonly HotkeyStateMachine state = new();
    private IntPtr hook;
    public event Action<string> Triggered;
    public HotkeyService(IntPtr handle)
    {
        callback = OnKeyboard;
        hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "全局键盘监听启动失败");
    }
    public static (uint modifiers, uint key) Parse(string text)
    {
        uint modifiers = 0;
        string[] parts = (text ?? "").Split('+', StringSplitOptions.TrimEntries);
        foreach (string modifier in parts.Take(parts.Length - 1))
            modifiers |= modifier.ToLowerInvariant() switch { "ctrl" => 2u, "alt" => 1u, "shift" => 4u, _ => throw new ArgumentException("快捷键修饰符无效：" + text) };
        string name = parts.Last().ToLowerInvariant() switch { "numpadplus" => "Add", "numpadminus" => "Subtract", _ => parts.Last() };
        if (!Enum.TryParse<Key>(name, true, out var key) || key == Key.None || key == Key.System) throw new ArgumentException("快捷键无效：" + text);
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0 || new uint[] { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }.Contains(vk)) throw new ArgumentException("请按完整快捷键：" + text);
        return (modifiers, vk);
    }
    public static void Validate(IEnumerable<string> keys)
    {
        var seen = new HashSet<(uint, uint)>();
        foreach (var key in keys) if (!seen.Add(Parse(key))) throw new ArgumentException("快捷键重复：" + key);
    }
    public void Register(string key, Action action, bool onRelease = false) => state.Register(Parse(key), () => { Triggered?.Invoke(key); action(); }, onRelease);
    public void Clear() => state.Clear();
    private static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    private IntPtr OnKeyboard(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var input = Marshal.PtrToStructure<KeyboardData>(data);
            int msg = message.ToInt32();
            bool keyDown = msg == 0x100 || msg == 0x104;
            bool keyUp = msg == 0x101 || msg == 0x105;
            if (keyDown || keyUp)
            {
                uint mods = (Down(0x11) ? 2u : 0) | (Down(0x12) ? 1u : 0) | (Down(0x10) ? 4u : 0) | (Down(0x5B) || Down(0x5C) ? 8u : 0);
                // Akari accepts external keyboard events, including remote desktop input.
                // Ignore only our own SendInput events, not every LLKHF_INJECTED event.
                var action = state.Process(input.Key, mods, keyDown, input.Extra == WindowsGameInput.OwnInputTag || input.Key == 231);
                if (action != null) dispatcher.BeginInvoke(action, DispatcherPriority.Input);
            }
        }
        return CallNextHookEx(hook, code, message, data);
    }
    public void Dispose() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } Clear(); }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardData { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
    private delegate IntPtr KeyboardProc(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, KeyboardProc proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string module);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
}

public sealed class HotkeyStateMachine
{
    private readonly Dictionary<(uint modifiers, uint key), (Action action, bool onRelease)> bindings = new();
    private readonly HashSet<uint> pressed = new();
    private Action pendingRelease;
    private int generation;
    public void Register((uint modifiers, uint key) key, Action action, bool onRelease = false)
    {
        if (!bindings.TryAdd(key, (action, onRelease))) throw new ArgumentException("快捷键重复");
    }
    public void Clear() { bindings.Clear(); pendingRelease = null; generation++; }
    public Action Process(uint key, uint modifiers, bool down, bool injected)
    {
        if (injected) return null;
        if (!down)
        {
            pressed.Remove(key);
            if (pressed.Count != 0) return null;
            var pending = pendingRelease; pendingRelease = null;
            return pending;
        }
        if (!pressed.Add(key)) return null;
        if (key is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5) return null;
        pendingRelease = null; // Akari last-active: a later ordinary key replaces the previous shortcut.
        if (!bindings.TryGetValue((modifiers, key), out var binding)) return null;
        int version = generation;
        Action invoke = () => { if (version == generation) binding.action(); };
        if (binding.onRelease) { pendingRelease = invoke; return null; }
        return invoke;
    }
}
