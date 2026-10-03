using System.IO;
using System.Speech.Synthesis;
using Xunit;
namespace Timer.Tests;
public class VoiceTests
{
    [Fact] public void ChineseEngineProducesAudio()
    {
        using var engine = new SpeechSynthesizer();
        var chinese = engine.GetInstalledVoices().FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName == "zh");
        Assert.NotNull(chinese);
        engine.SelectVoice(chinese.VoiceInfo.Name);
        using var audio = new MemoryStream(); engine.SetOutputToWaveStream(audio);
        engine.Speak("语音测试，上路闪现已记录");
        Assert.True(audio.Length > 1000);
    }
}
