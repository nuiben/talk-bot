using System;
using System.Collections.Generic;

namespace talk.Tests
{
    // A backend that has whatever voices the test needs it to. The settings
    // screen used to reach a voice through two menus, and the arrows now walk
    // the same list in place, so what the arrows land on is worth checking
    // without a console to press them at.
    internal class FakeEngine : ISpeechEngine
    {
        private readonly string[] voices;
        private readonly Dictionary<string, string> groups;

        public FakeEngine(string[] newVoices, Dictionary<string, string> newGroups)
        {
            voices = newVoices;
            groups = newGroups;
        }

        public void Speak(string phrase)
        {
        }

        public void Stop()
        {
        }

        public string[] AvailableVoices()
        {
            return voices;
        }

        public string VoiceGroup(string voice)
        {
            string group;
            return groups != null && groups.TryGetValue(voice, out group) ? group : null;
        }

        public bool HonoursPitch
        {
            get { return true; }
        }

        public string Describe()
        {
            return "a test engine";
        }
    }

    internal class VoiceCycleTest : ITest
    {
        public string Name
        {
            get { return "voice cycle"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            // Kokoro's shape in miniature: voices sorted into languages, which
            // is what the language row is for.
            Dictionary<string, string> groups = new Dictionary<string, string>();
            groups["af_heart"] = "American English";
            groups["af_bella"] = "American English";
            groups["bf_emma"] = "British English";
            groups["jf_alpha"] = "Japanese";
            string[] voices = new string[] { "af_heart", "af_bella", "bf_emma", "jf_alpha" };

            VoiceSettings settings = new VoiceSettings();
            FakeEngine engine = new FakeEngine(voices, groups);
            VoiceCycle cycle = new VoiceCycle(settings, engine);

            checks.Equal("languages are offered when there is more than one", true,
                cycle.HasLanguages);
            checks.Equal("three languages were found", 3, cycle.Languages.Length);
            checks.Equal("and the first is the one the voices start in",
                "American English", cycle.LanguageName());

            // The default is the first stop, so the right arrow reaches the
            // voices and the left one comes back to it.
            checks.Equal("the dial starts at the default voice", "default", cycle.VoiceName());
            cycle.TurnVoice(1);
            checks.Equal("right steps to the first voice", "af_heart", cycle.VoiceName());
            cycle.TurnVoice(1);
            checks.Equal("and on to the second", "af_bella", cycle.VoiceName());
            cycle.TurnVoice(-1);
            checks.Equal("left comes back", "af_heart", cycle.VoiceName());
            cycle.TurnVoice(-1);
            checks.Equal("and back to the default", "default", cycle.VoiceName());

            // Wrapping, in the way the up and down arrows wrap: an end that
            // stops dead gives no sign of being an end.
            cycle.TurnVoice(-1);
            checks.Equal("left off the default wraps to the last voice", "af_bella",
                cycle.VoiceName());
            cycle.TurnVoice(1);
            checks.Equal("and right wraps back to the default", "default", cycle.VoiceName());

            // The voice dial only ever walks the language being shown, which is
            // the whole reason a language row exists: a hundred and fifty
            // voices in one list is not something an arrow key can cross.
            cycle.TurnLanguage(1);
            checks.Equal("right steps to the next language", "British English",
                cycle.LanguageName());
            checks.Equal("and takes the voice with it", "bf_emma", cycle.VoiceName());
            cycle.TurnVoice(1);
            checks.Equal("the dial stays inside that language", "default", cycle.VoiceName());

            cycle.TurnLanguage(-1);
            checks.Equal("left comes back to the language before", "American English",
                cycle.LanguageName());
            cycle.TurnLanguage(-1);
            checks.Equal("and left again wraps to the last", "Japanese", cycle.LanguageName());
            checks.Equal("with its own first voice", "jf_alpha", cycle.VoiceName());

            // A language chosen from the list behaves as the arrow does.
            cycle.LanguageIndex = 0;
            checks.Equal("choosing a language sets it", "American English",
                cycle.LanguageName());
            checks.Equal("and its first voice", "af_heart", cycle.VoiceName());
            cycle.LanguageIndex = 99;
            checks.Equal("a language that is not there is ignored", "American English",
                cycle.LanguageName());

            // Reopening the screen on a voice already chosen has to come back
            // to the language that voice is in, not to the first one.
            settings.Voice = "jf_alpha";
            VoiceCycle reopened = new VoiceCycle(settings, engine);
            checks.Equal("a saved voice reopens in its own language", "Japanese",
                reopened.LanguageName());
            checks.Equal("still on that voice", "jf_alpha", reopened.VoiceName());

            // An engine whose voices are one list, which is espeak: no language
            // row, and the arrows walk everything it has.
            VoiceSettings plain = new VoiceSettings();
            VoiceCycle flat = new VoiceCycle(plain,
                new FakeEngine(new string[] { "en", "en+f3" }, null));
            checks.Equal("one language is no language row", false, flat.HasLanguages);
            checks.Equal("and every voice is on the dial", 2, flat.VoicesHere.Length);
            flat.TurnVoice(1);
            checks.Equal("which the arrows still walk", "en", flat.VoiceName());

            // A backend with one voice has nothing to walk, and the row says so
            // rather than offering arrows that do nothing.
            VoiceCycle silent = new VoiceCycle(new VoiceSettings(),
                new FakeEngine(new string[0], null));
            checks.Equal("a backend with one voice has no dial", false, silent.HasVoices());
            checks.Equal("and says as much", true,
                silent.DescribeVoice().Contains("only has the one voice"));
            silent.TurnVoice(1);
            checks.Equal("turning it changes nothing", "default", silent.VoiceName());
            silent.TurnLanguage(1);
            checks.Equal("nor does turning a language it has not got", "", silent.LanguageName());

            return checks.Report(Name, "the arrows walk voices and languages and wrap at both ends");
        }
    }
}
