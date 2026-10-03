using System.Runtime.InteropServices;
using System.Windows;
namespace Timer.Services;
public sealed class ClipboardService
{
    public bool Copy(string text)
    {
        try { Clipboard.SetText(text); return true; }
        catch (ExternalException) { return false; }
    }
}
