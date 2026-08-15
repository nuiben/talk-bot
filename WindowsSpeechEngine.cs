#if WINDOWS
using System;
using System.Collections.Generic;
using System.Security;
using System.Speech.Synthesis;

namespace talk
{
    class WindowsSpeechEngine : ISpeechEngine
    {
        private readonly SpeechSynthesizer tina = new SpeechSynthesizer();

        // Spoken asynchronously so the keyboard can be watched while it plays;
        // the wait puts the blocking behaviour back for the caller.
        public void Speak(string phrase)
        {
            VoiceSettings settings = VoiceSettings.Current;
            Apply(settings);

            // System.Speech has no pitch of its own, so a pitched phrase is
            // handed over as SSML instead. A voice that will not take the
            // markup still gets to read the words.
            Prompt spoken = null;
            if (settings.Pitch != 0)
            {
                try
                {
                    spoken = tina.SpeakSsmlAsync(Ssml(phrase, settings));
                }
                catch (FormatException)
                {
                }
            }
            if (spoken == null)
            {
                spoken = tina.SpeakAsync(phrase);
            }

            SpeechInterrupt.WaitFor(delegate { return spoken.IsCompleted; }, Stop);
        }

        // Rate and volume are already on the ranges the settings use; the voice
        // is reselected each time because it may have been changed since the
        // last phrase, and a name that is no longer installed should not stop
        // the phrase being read.
        private void Apply(VoiceSettings settings)
        {
            tina.Rate = settings.Speed;
            tina.Volume = settings.Volume;

            string wanted = settings.Voice == null ? "Microsoft Zira Desktop" : settings.Voice;
            try
            {
                tina.SelectVoice(wanted);
            }
            catch (ArgumentException)
            {
            }
        }

        // Pitch is a percentage either side of the voice's own, and the text is
        // escaped because anything that looks like markup inside a fetched page
        // would otherwise be read as SSML.
        private static string Ssml(string phrase, VoiceSettings settings)
        {
            int percent = settings.Pitch * 5;
            string sign = percent >= 0 ? "+" : "";
            return "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
                "xml:lang=\"en-US\"><prosody pitch=\"" + sign + percent + "%\">" +
                SecurityElement.Escape(phrase) + "</prosody></speak>";
        }

        public void Stop()
        {
            tina.SpeakAsyncCancelAll();
        }

        public string[] AvailableVoices()
        {
            List<string> voices = new List<string>();
            foreach (InstalledVoice installed in tina.GetInstalledVoices())
            {
                if (installed.Enabled)
                {
                    voices.Add(installed.VoiceInfo.Name);
                }
            }
            return voices.ToArray();
        }

        public string Describe()
        {
            return "Windows speech - voice, speed, pitch and volume";
        }
    }
}
#endif
