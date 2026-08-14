#if WINDOWS
using System;
using System.Speech.Synthesis;

namespace talk
{
    class WindowsSpeechEngine : ISpeechEngine
    {
        private readonly SpeechSynthesizer tina = new SpeechSynthesizer();

        public WindowsSpeechEngine()
        {
            // Zira is not installed everywhere, so fall back to the default voice.
            try
            {
                tina.SelectVoice("Microsoft Zira Desktop");
            }
            catch (ArgumentException)
            {
            }
        }

        public void Speak(string phrase)
        {
            tina.Speak(phrase);
        }
    }
}
#endif
