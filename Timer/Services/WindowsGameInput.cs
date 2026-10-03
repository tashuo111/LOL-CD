using System;
using System.Runtime.InteropServices;
namespace Timer.Services;
public sealed class WindowsGameInput : IGameInput
{
    public static readonly UIntPtr OwnInputTag = new UIntPtr(0x4C4B4654u);
    public string LastDiagnostic { get; private set; } = "未执行输入";
    // MOUSEINPUT ensures correct native INPUT union size on x86 and x64.
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    public bool ModifiersDown() => Down(0x10) || Down(0x11) || Down(0x12) || Down(0x5B) || Down(0x5C);
    private static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    private static Input Key(ushort vk, ushort scan, uint flags) => new() { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { Key = vk, Scan = scan, Flags = flags, Extra = OwnInputTag } } };
    private bool Send(Input[] events)
    {
        uint inserted = SendInput((uint)events.Length, events, Marshal.SizeOf<Input>());
        LastDiagnostic = $"SendInput {inserted}/{events.Length}, Win32={Marshal.GetLastWin32Error()}";
        return inserted == events.Length;
    }
    public bool PressEnter()
    {
        // A short down/up interval matches normal typing and Akari's Enter sequence.
        ushort scan = (ushort)MapVirtualKey(0x0D, 0);
        if (!Send(new[] { Key(0x0D, scan, 0) })) return false;
        System.Threading.Thread.Sleep(20);
        return Send(new[] { Key(0x0D, scan, 2) });
    }
    public bool SendText(string text)
    {
        var events = new Input[text.Length * 2];
        for (int i = 0; i < text.Length; i++) { events[i * 2] = Key(0, text[i], 4); events[i * 2 + 1] = Key(0, text[i], 6); }
        uint count = SendInput((uint)events.Length, events, Marshal.SizeOf<Input>());
        string diagnostic = $"SendInput 文本 {count}/{events.Length}, Win32={Marshal.GetLastWin32Error()}";
        if ((count & 1) != 0) Send(new[] { events[count] });
        LastDiagnostic = diagnostic;
        return count == events.Length;
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint code, uint type);
}
