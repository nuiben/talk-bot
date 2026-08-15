using System;
using System.IO;

namespace talk.Tests
{
    // Settings on disk. What matters here is that a file which does not say
    // everything still says what it does say: the file is meant to be edited by
    // hand, and half of one is what that produces.
    internal class SettingsStoreTest : ITest
    {
        public string Name
        {
            get { return "settings store"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            VoiceSettings settings = new VoiceSettings();
            settings.Voice = "af_heart";
            settings.Speed = -3;
            settings.Pitch = 4;
            settings.Volume = 80;

            string json = SettingsStore.Serialize(settings, SpeechEngine.Kokoro);
            checks.Equal("the file says which version it is", true,
                json.Contains("\"version\": 1"));
            checks.Equal("the engine is written", true, json.Contains("\"engine\": \"kokoro\""));
            checks.Equal("and the voice", true, json.Contains("\"af_heart\""));

            VoiceSettings read = new VoiceSettings();
            SettingsStore.Apply(json, read);
            checks.Equal("the voice comes back", "af_heart", read.Voice);
            checks.Equal("the speed comes back", -3, read.Speed);
            checks.Equal("the pitch comes back", 4, read.Pitch);
            checks.Equal("the volume comes back", 80, read.Volume);

            // A file that mentions one setting is one somebody edited to change
            // that setting. The rest are left where they were rather than reset
            // to the defaults the file did not ask for.
            VoiceSettings partial = new VoiceSettings();
            partial.Voice = "bf_emma";
            partial.Volume = 55;
            SettingsStore.Apply("{\"speed\": 7}", partial);
            checks.Equal("the setting in the file is applied", 7, partial.Speed);
            checks.Equal("the voice it did not mention is left alone", "bf_emma", partial.Voice);
            checks.Equal("and so is the volume", 55, partial.Volume);

            // Zero is a setting, not a missing one: a volume of nothing is
            // silence somebody chose, and it has to survive the trip.
            VoiceSettings quiet = new VoiceSettings();
            quiet.Volume = 0;
            VoiceSettings loaded = new VoiceSettings();
            SettingsStore.Apply(SettingsStore.Serialize(quiet, SpeechEngine.System), loaded);
            checks.Equal("a volume of zero is kept", 0, loaded.Volume);

            // Hand edited numbers are clamped by the settings themselves rather
            // than handed to a synthesizer that would read them as a speed of
            // five hundred words a minute.
            VoiceSettings clamped = new VoiceSettings();
            SettingsStore.Apply("{\"speed\": 500, \"pitch\": -500, \"volume\": 999}", clamped);
            checks.Equal("a speed past the top is clamped", VoiceSettings.Fastest,
                clamped.Speed);
            checks.Equal("a pitch past the bottom is clamped", VoiceSettings.Lowest,
                clamped.Pitch);
            checks.Equal("a volume past full is clamped", VoiceSettings.Loudest,
                clamped.Volume);

            // Nothing at all is the first run, and an empty object is a file
            // that has been emptied out; both leave the defaults standing.
            VoiceSettings untouched = new VoiceSettings();
            SettingsStore.Apply("", untouched);
            SettingsStore.Apply("{}", untouched);
            checks.Equal("an empty file leaves the speed alone", 0, untouched.Speed);
            checks.Equal("and the volume", 100, untouched.Volume);

            bool reported = false;
            try
            {
                SettingsStore.Apply("{ \"speed\": ", new VoiceSettings());
            }
            catch (SettingsStoreException)
            {
                reported = true;
            }
            checks.Equal("a broken file is reported", true, reported);

            return RunFileChecks(checks);
        }

        // Written where the tests run rather than in the user's data folder, so
        // a test run never touches settings somebody chose.
        private bool RunFileChecks(Checks checks)
        {
            string path = Path.Combine(Path.GetTempPath(),
                "talk-bot-test-" + Guid.NewGuid().ToString("N"), "config.json");
            try
            {
                // No file is the first run: the defaults stand, and nothing is
                // said about it.
                VoiceSettings settings = new VoiceSettings();
                SettingsStore.Load(path, settings);
                checks.Equal("a missing file leaves the defaults", 100, settings.Volume);

                settings.Voice = "en+f3";
                settings.Speed = 6;
                settings.Volume = 40;
                SettingsStore.Save(path, settings, SpeechEngine.System);
                checks.Equal("the file was written", true, File.Exists(path));

                VoiceSettings next = new VoiceSettings();
                SettingsStore.Load(path, next);
                checks.Equal("the voice survives the program", "en+f3", next.Voice);
                checks.Equal("as does the speed", 6, next.Speed);
                checks.Equal("and the volume", 40, next.Volume);

                File.WriteAllText(path, "{ \"volume\":");
                bool reported = false;
                try
                {
                    SettingsStore.Load(path, new VoiceSettings());
                }
                catch (SettingsStoreException)
                {
                    reported = true;
                }
                checks.Equal("a broken file on disk is reported", true, reported);
            }
            finally
            {
                string directory = Path.GetDirectoryName(path);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }

            return checks.Report(Name, "settings survive the program, and half a file still counts");
        }
    }
}
