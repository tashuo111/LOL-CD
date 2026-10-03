using System;
using System.Linq;
using System.Speech.Synthesis;
namespace Timer.Services;
public sealed class VoiceService : IDisposable
{
    private SpeechSynthesizer engine;
    public event Action<string> Failed;
    public string VoiceName => engine?.Voice.Name ?? "未初始化";
    public void Speak(string text)
    {
        try
        {
            if (engine == null)
            {
                engine = new SpeechSynthesizer { Volume = 100, Rate = 0 };
                engine.SetOutputToDefaultAudioDevice();
                var chinese = engine.GetInstalledVoices().FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName == "zh");
                if (chinese != null) engine.SelectVoice(chinese.VoiceInfo.Name);
                engine.SpeakCompleted += (_, e) => { if (e.Error != null) Failed?.Invoke("语音播放失败：" + e.Error.Message); };
            }
            engine.SpeakAsync(text);
        }
        catch (Exception e) { Failed?.Invoke("语音播放失败：" + e.Message); }
    }
    public void Stop() { try { engine?.SpeakAsyncCancelAll(); } catch (InvalidOperationException) { } }
    public void Dispose() { Stop(); engine?.Dispose(); engine = null; }
}

