using System;

namespace talk.Tests
{
    // The dials are the one place a user types a number straight into the
    // program, so this checks that every number they can type lands somewhere
    // sensible: past either end, at either end, and the ones that are not
    // numbers at all are the console test's business.
    internal class VoiceSettingsTest : ITest
    {
        public string Name
        {
            get { return "voice settings"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();
            VoiceSettings settings = new VoiceSettings();

            // Out of range in either direction stops at the end of the dial
            // rather than being passed on to a synthesizer that would read it
            // as a speed of minus two hundred words a minute.
            settings.Speed = 999;
            checks.Equal("speed above the top", VoiceSettings.Fastest, settings.Speed);
            settings.Speed = -999;
            checks.Equal("speed below the bottom", VoiceSettings.Slowest, settings.Speed);
            settings.Pitch = int.MaxValue;
            checks.Equal("pitch at int.MaxValue", VoiceSettings.Highest, settings.Pitch);
            settings.Pitch = int.MinValue;
            checks.Equal("pitch at int.MinValue", VoiceSettings.Lowest, settings.Pitch);
            settings.Volume = 250;
            checks.Equal("volume above full", VoiceSettings.Loudest, settings.Volume);
            settings.Volume = -1;
            checks.Equal("volume below silent", VoiceSettings.Quietest, settings.Volume);

            // A volume of zero is a real setting, not a missing one, so it has
            // to survive being set.
            settings.Volume = 0;
            checks.Equal("volume set to silent", 0, settings.Volume);

            // A new bot starts at normal and full, and Reset puts it back.
            VoiceSettings fresh = new VoiceSettings();
            checks.Equal("speed starts normal", 0, fresh.Speed);
            checks.Equal("pitch starts normal", 0, fresh.Pitch);
            checks.Equal("volume starts full", 100, fresh.Volume);
            checks.Equal("voice starts as the default", null, fresh.Voice);
            checks.Equal("the default voice has a name to show", "default", fresh.VoiceName);

            settings.Voice = "en+f3";
            settings.Reset();
            checks.Equal("reset clears the voice", null, settings.Voice);
            checks.Equal("reset returns the volume", 100, settings.Volume);

            // A name that is empty or nothing but spaces would be handed over
            // as a voice the synthesizer does not have, which fails silently.
            settings.Voice = "";
            checks.Equal("an empty voice name is the default", null, settings.Voice);
            settings.Voice = "   ";
            checks.Equal("a blank voice name is the default", null, settings.Voice);
            settings.Voice = "  en-gb  ";
            checks.Equal("a padded voice name is trimmed", "en-gb", settings.Voice);
            settings.Voice = null;
            checks.Equal("a null voice name is the default", null, settings.Voice);

            // Scaling has to keep 0 on each backend's own normal, and the ends
            // of the dial on the ends of its range, or a setting means one
            // thing on Linux and another on a Mac.
            checks.Equal("normal speed is espeak's normal", 175,
                VoiceSettings.Scale(0, 80, 175, 450));
            checks.Equal("the slowest speed is espeak's slowest", 80,
                VoiceSettings.Scale(-10, 80, 175, 450));
            checks.Equal("the fastest speed is espeak's fastest", 450,
                VoiceSettings.Scale(10, 80, 175, 450));
            checks.Equal("the lowest pitch is espeak's lowest", 0,
                VoiceSettings.Scale(-10, 0, 50, 99));
            checks.Equal("the highest pitch is espeak's highest", 99,
                VoiceSettings.Scale(10, 0, 50, 99));

            // Every step in between has to stay inside the range and keep
            // going the same way, since a dial that doubles back would be worse
            // than one that did nothing.
            int previous = -1;
            for (int dial = VoiceSettings.Slowest; dial <= VoiceSettings.Fastest; dial++)
            {
                int scaled = VoiceSettings.Scale(dial, 80, 175, 450);
                checks.True("speed dial " + dial + " stays in range",
                    scaled >= 80 && scaled <= 450);
                checks.True("speed dial " + dial + " is faster than " + (dial - 1),
                    scaled > previous);
                previous = scaled;
            }

            return checks.Report(Name, "dials clamp, reset and scale onto every backend");
        }
    }
}
