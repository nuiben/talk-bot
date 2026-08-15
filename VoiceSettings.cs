using System;

namespace talk
{
    // How Talk Bot should sound, described in units the user can reason about
    // rather than in the ones any one synthesizer happens to use. Every backend
    // takes the same -10..10 dials and converts them to whatever it wants, so
    // the same settings mean roughly the same thing on every platform.
    //
    // Voice is a name the current backend understands, or null for whichever
    // voice it starts with. The names differ per backend, which is why they are
    // asked for rather than listed here.
    internal class VoiceSettings
    {
        public const int Slowest = -10;
        public const int Fastest = 10;
        public const int Lowest = -10;
        public const int Highest = 10;
        public const int Quietest = 0;
        public const int Loudest = 100;

        // One shared set of settings, because the menu changes them and the
        // engine reads them at the moment it speaks.
        private static VoiceSettings current = new VoiceSettings();

        public static VoiceSettings Current
        {
            get { return current; }
        }

        private string voice;
        private int speed;
        private int pitch;
        private int volume = 100;

        public string Voice
        {
            get { return voice; }
            set { voice = string.IsNullOrEmpty(value) ? null : value; }
        }

        public int Speed
        {
            get { return speed; }
            set { speed = Clamp(value, Slowest, Fastest); }
        }

        public int Pitch
        {
            get { return pitch; }
            set { pitch = Clamp(value, Lowest, Highest); }
        }

        public int Volume
        {
            get { return volume; }
            set { volume = Clamp(value, Quietest, Loudest); }
        }

        public string VoiceName
        {
            get { return voice == null ? "default" : voice; }
        }

        public void Reset()
        {
            voice = null;
            speed = 0;
            pitch = 0;
            volume = 100;
        }

        // Scales a dial onto a backend's own range, keeping 0 on the value that
        // backend treats as normal. The two halves are scaled separately
        // because a backend's normal is rarely the middle of its range: espeak
        // speaks at 175 words a minute out of a possible 80 to 450.
        public static int Scale(int dial, int lowest, int normal, int highest)
        {
            if (dial == 0)
            {
                return normal;
            }
            if (dial < 0)
            {
                return normal - (normal - lowest) * -dial / 10;
            }
            return normal + (highest - normal) * dial / 10;
        }

        public static int Clamp(int value, int low, int high)
        {
            if (value < low)
            {
                return low;
            }
            if (value > high)
            {
                return high;
            }
            return value;
        }

        // Said after a change so the user hears the setting rather than reading
        // a number and guessing.
        public const string Sample =
            "Hello, this is Talk Bot. This is how I sound right now.";
    }
}
