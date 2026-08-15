using System;
using System.Collections.Generic;

namespace talk.Tests
{
    // What the user types is handed to another program, so this checks the
    // command line that gets built for it. None of this starts a synthesizer,
    // so it runs on a machine that has none installed.
    //
    // The case that matters most is a phrase that begins with a dash. Every
    // backend here parses its arguments with getopt, so "-v is the voice flag"
    // typed as a phrase was read as options and spoken as nothing at all.
    internal class SpeechArgumentTest : ITest
    {
        public string Name
        {
            get { return "speech arguments"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();
            VoiceSettings plain = new VoiceSettings();

            foreach (string command in new string[] { "espeak-ng", "espeak", "say", "spd-say" })
            {
                List<string> arguments = CommandSpeechEngine.Arguments(
                    command, "-v is the voice flag", plain);
                int separator = arguments.IndexOf("--");
                checks.True(command + " ends its options with --", separator >= 0);
                checks.Equal(command + " puts the phrase last, behind the --",
                    "-v is the voice flag", arguments[arguments.Count - 1]);
                checks.Equal(command + " passes the phrase as one argument",
                    separator + 2, arguments.Count);
            }

            // A phrase is one argument however many spaces, quotes or newlines
            // are in it, so nothing in it can turn into an option or a second
            // phrase.
            string awkward = "say \"--help\" -v; rm -rf /\nand a second line";
            List<string> awkwardArguments = CommandSpeechEngine.Arguments(
                "espeak-ng", awkward, plain);
            checks.Equal("quotes, semicolons and newlines stay in the phrase",
                awkward, awkwardArguments[awkwardArguments.Count - 1]);

            // Nothing typed should reach the synthesizer as an empty phrase
            // rather than as a null it will not take.
            List<string> empty = CommandSpeechEngine.Arguments("espeak-ng", null, plain);
            checks.Equal("a null phrase becomes an empty one", "",
                empty[empty.Count - 1]);

            // festival reads from its standard input, so it gets no phrase on
            // the command line and no dials it cannot set.
            List<string> festival = CommandSpeechEngine.Arguments(
                "festival", "-v is the voice flag", plain);
            checks.Equal("festival is asked to read its input", 1, festival.Count);
            checks.Equal("festival takes no phrase argument", "--tts", festival[0]);

            // The dials have to arrive in each backend's own units.
            VoiceSettings set = new VoiceSettings();
            set.Voice = "en+f3";
            set.Speed = -10;
            set.Pitch = 10;
            set.Volume = 40;

            List<string> espeak = CommandSpeechEngine.Arguments("espeak-ng", "hello", set);
            checks.Equal("espeak is given the voice", "en+f3", After(espeak, "-v"));
            checks.Equal("espeak is given words a minute", "80", After(espeak, "-s"));
            checks.Equal("espeak is given a 0-99 pitch", "99", After(espeak, "-p"));
            checks.Equal("espeak is given an amplitude", "40", After(espeak, "-a"));

            List<string> spd = CommandSpeechEngine.Arguments("spd-say", "hello", set);
            checks.Equal("spd-say is given a voice type", "en+f3", After(spd, "-t"));
            checks.Equal("spd-say is given a -100 to 100 rate", "-100", After(spd, "-r"));
            checks.Equal("spd-say is given a -100 to 100 pitch", "100", After(spd, "-p"));
            checks.Equal("spd-say is given a -100 to 100 volume", "-20", After(spd, "-i"));
            checks.True("spd-say is asked to wait for the phrase to finish",
                spd.Contains("-w"));

            List<string> mac = CommandSpeechEngine.Arguments("say", "hello", set);
            checks.Equal("say is given the voice", "en+f3", After(mac, "-v"));
            checks.Equal("say is given words a minute", "90", After(mac, "-r"));

            // A macOS voice name has a space in it, and it has to stay one
            // argument or the voice becomes "Bad" and "News" becomes a phrase.
            VoiceSettings spaced = new VoiceSettings();
            spaced.Voice = "Bad News";
            List<string> spacedArguments = CommandSpeechEngine.Arguments(
                "say", "hello", spaced);
            checks.Equal("a voice name with a space stays one argument", "Bad News",
                After(spacedArguments, "-v"));

            // The default voice is the synthesizer's own, so nothing is passed.
            List<string> defaulted = CommandSpeechEngine.Arguments(
                "espeak-ng", "hello", new VoiceSettings());
            checks.True("no voice is named when the default is in use",
                !defaulted.Contains("-v"));

            return checks.Report(Name,
                "phrases survive dashes and quotes, and dials reach each backend");
        }

        // The value that follows a flag, or null when the flag is not there.
        private static string After(List<string> arguments, string flag)
        {
            int at = arguments.IndexOf(flag);
            if (at < 0 || at + 1 >= arguments.Count)
            {
                return null;
            }
            return arguments[at + 1];
        }
    }
}
