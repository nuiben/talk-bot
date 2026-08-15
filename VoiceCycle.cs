using System;
using System.Collections.Generic;

namespace talk
{
    // Where the settings screen is standing in a backend's list of voices, so
    // that the left and right arrows have somewhere to step from.
    //
    // A language of its own is kept rather than worked out from the voice each
    // time, because the default voice is in no language at all: without it,
    // stepping off the default would land back at the first language every
    // time rather than the one the user had walked to.
    //
    // Nothing here draws anything. The screen asks it what to show and tells it
    // which way the arrow went, which is what lets the walking be tested
    // without a console to press keys at.
    internal class VoiceCycle
    {
        private readonly VoiceSettings settings;
        private readonly string[] voices;
        private readonly string[] languages;
        private readonly ISpeechEngine engine;

        private int language;

        public VoiceCycle(VoiceSettings newSettings, ISpeechEngine newEngine)
        {
            settings = newSettings;
            engine = newEngine;
            voices = newEngine.AvailableVoices();
            if (voices == null)
            {
                voices = new string[0];
            }
            languages = ConsoleView.Groups(voices, newEngine);
            language = LanguageOf(settings.Voice);
        }

        // More than one, because a backend whose voices are all one language
        // has nothing to choose between and should not be given a row saying
        // so.
        public bool HasLanguages
        {
            get { return languages.Length > 1; }
        }

        public string[] Languages
        {
            get { return languages; }
        }

        public bool HasVoices()
        {
            return voices.Length > 0;
        }

        // The voices the arrows walk: the ones in the language being shown, or
        // all of them on a backend that does not sort them into languages.
        public string[] VoicesHere
        {
            get
            {
                if (!HasLanguages)
                {
                    return voices;
                }
                return ConsoleView.InGroup(voices, engine, languages[language]);
            }
        }

        // The default is the first stop on the dial rather than a row of its
        // own somewhere else, so the way back to it is the same left arrow that
        // walks everything else.
        private string[] Choices
        {
            get
            {
                List<string> choices = new List<string>();
                choices.Add(null);
                choices.AddRange(VoicesHere);
                return choices.ToArray();
            }
        }

        public int LanguageIndex
        {
            get { return language; }
            set
            {
                if (languages.Length == 0 || value < 0 || value >= languages.Length)
                {
                    return;
                }
                language = value;
                // The voice that was set belongs to the language being left, so
                // it is stepped to the first of the new one: a language row that
                // changed nothing anybody could hear would be a label, not a
                // setting.
                string[] here = VoicesHere;
                settings.Voice = here.Length > 0 ? here[0] : null;
            }
        }

        public int VoicesIn(string named)
        {
            return ConsoleView.InGroup(voices, engine, named).Length;
        }

        public string LanguageName()
        {
            return languages.Length == 0 ? "" : languages[language];
        }

        public string DescribeLanguage()
        {
            return languages.Length + " languages, " + VoicesHere.Length + " voices in this one";
        }

        public void TurnLanguage(int direction)
        {
            if (languages.Length == 0)
            {
                return;
            }
            LanguageIndex = Step(language, direction, languages.Length);
        }

        public string VoiceName()
        {
            return settings.VoiceName;
        }

        public string DescribeVoice()
        {
            if (!HasVoices())
            {
                return "This synthesizer only has the one voice";
            }
            string[] choices = Choices;
            string where = "voice " + (IndexOf(choices, settings.Voice) + 1) +
                " of " + choices.Length;
            return HasLanguages ? where + " in " + LanguageName() : where;
        }

        public void TurnVoice(int direction)
        {
            string[] choices = Choices;
            if (choices.Length == 0)
            {
                return;
            }
            int next = Step(IndexOf(choices, settings.Voice), direction, choices.Length);
            settings.Voice = choices[next];
        }

        // Which language a voice belongs to, and the first one for a voice that
        // belongs to none - the default, or a name left over from a backend
        // that is no longer the one selected.
        private int LanguageOf(string voice)
        {
            if (voice == null)
            {
                return 0;
            }
            string group = engine.VoiceGroup(voice);
            for (int i = 0; i < languages.Length; i++)
            {
                if (languages[i] == group)
                {
                    return i;
                }
            }
            return 0;
        }

        // Not in the list means the default, which is where the dial starts.
        private static int IndexOf(string[] choices, string voice)
        {
            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] == voice)
                {
                    return i;
                }
            }
            return 0;
        }

        // Wrapping, in the way the up and down arrows wrap: holding one arrow
        // reaches every voice rather than stopping at an end the row gives no
        // sign of having reached.
        internal static int Step(int index, int direction, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            return ((index + direction) % count + count) % count;
        }
    }
}
