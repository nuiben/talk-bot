using System;
using System.Collections.Generic;

namespace talk.Tests
{
    // Everything about the Kokoro backend that can be checked without the
    // model. The model is a 320MB download and an audio device, neither of
    // which a test run should need, so what is checked here is the part that
    // decides what the user is offered and what the synthesizer is asked for:
    // the voice list, the groups the settings screen splits it into, and the
    // speed dial.
    internal class KokoroVoiceTest : ITest
    {
        public string Name
        {
            get { return "kokoro voices"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();
            KokoroSpeechEngine engine = new KokoroSpeechEngine();

            // The voices ship with the package rather than with the model, so
            // they can be listed before anything has been downloaded. An empty
            // list here means they were not copied next to the executable, and
            // the settings screen would say Kokoro has only one voice.
            string[] voices = engine.AvailableVoices();
            checks.True("the voices are listed without the model", voices.Length > 0);
            checks.True("af_heart is among them",
                Array.IndexOf(voices, KokoroSpeechEngine.DefaultVoiceName) >= 0);

            // English first, because the phrase library and the sample line are
            // both English and that is what most of this list's users want.
            if (voices.Length > 0)
            {
                checks.Equal("the list starts with an American English voice",
                    "American English",
                    First(engine.VoiceGroup(voices[0])));
            }

            // Every voice has to land in a group, or the settings screen would
            // offer a language menu that some voices are unreachable from.
            List<string> ungrouped = new List<string>();
            foreach (string voice in voices)
            {
                if (engine.VoiceGroup(voice) == null)
                {
                    ungrouped.Add(voice);
                }
            }
            checks.Equal("every voice is in a group", 0, ungrouped.Count);

            // The groups have to add back up to the whole list, which is what
            // the settings screen relies on when it shows one group at a time.
            string[] groups = ConsoleView.Groups(voices, engine);
            checks.True("there is more than one group", groups.Length > 1);
            int counted = 0;
            foreach (string group in groups)
            {
                counted = counted + ConsoleView.InGroup(voices, engine, group).Length;
            }
            checks.Equal("the groups hold every voice", voices.Length, counted);

            // A name Kokoro does not have must not be reported as belonging
            // anywhere, or it would be offered and then quietly not used.
            checks.Equal("an espeak voice is not a Kokoro group", null,
                engine.VoiceGroup("en+f3"));

            // The dial has to reach both ends and keep 0 on the pace the model
            // was trained at, the same as every other backend's scaling.
            checks.Equal("normal speed is Kokoro's normal", 1f, KokoroSpeechEngine.Speed(0));
            checks.Equal("the slowest speed is half", 0.5f,
                KokoroSpeechEngine.Speed(VoiceSettings.Slowest));
            checks.Equal("the fastest speed is double", 2f,
                KokoroSpeechEngine.Speed(VoiceSettings.Fastest));

            float previous = 0f;
            for (int dial = VoiceSettings.Slowest; dial <= VoiceSettings.Fastest; dial++)
            {
                float scaled = KokoroSpeechEngine.Speed(dial);
                checks.True("speed dial " + dial + " stays in range",
                    scaled >= 0.5f && scaled <= 2f);
                checks.True("speed dial " + dial + " is faster than " + (dial - 1),
                    scaled > previous);
                previous = scaled;
            }

            // Switching backends has to drop the voice, since "af_heart" means
            // nothing to espeak and "en+f3" means nothing to Kokoro.
            string startedOn = SpeechEngine.Selected;
            try
            {
                SpeechEngine.Select(SpeechEngine.Kokoro);
                VoiceSettings.Current.Voice = KokoroSpeechEngine.DefaultVoiceName;
                SpeechEngine.Select(SpeechEngine.System);
                checks.Equal("switching engines clears the voice", null,
                    VoiceSettings.Current.Voice);
            }
            finally
            {
                SpeechEngine.Select(startedOn);
                VoiceSettings.Current.Voice = null;
            }

            return checks.Report(Name,
                "the voice list, its groups and the speed dial, with no model needed");
        }

        // The group label carries the gender after the language, and only the
        // language half is worth asserting on.
        private static string First(string group)
        {
            if (group == null)
            {
                return null;
            }
            int bracket = group.IndexOf(" (", StringComparison.Ordinal);
            return bracket < 0 ? group : group.Substring(0, bracket);
        }
    }
}
