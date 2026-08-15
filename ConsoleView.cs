using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace talk
{
    class ConsoleView
    {
        public const int Exit = 7;

        int phraseID = 0;

        // Built once rather than per pass, so the highlight stays on the entry
        // the user last ran instead of jumping back to the top each time.
        private readonly Menu mainMenu = new Menu("TALK BOT", new List<MenuItem>
        {
            new MenuItem(1, "Add a phrase", "Type something for Talk Bot to say"),
            new MenuItem(2, "Remove a phrase", "Take a phrase back out of the list"),
            new MenuItem(3, "Make Talk Bot talk", "Read one of the saved phrases aloud"),
            new MenuItem(4, "Add Pengy's story as a phrase", "Fetch pengy.md from GitHub"),
            new MenuItem(5, "Read a web page out loud", "Paste a URL and hear the page"),
            new MenuItem(6, "Voice settings", "Pick a voice and set speed, pitch and volume"),
            new MenuItem(Exit, "Exit", "Close Talk Bot")
        });

        // Escape means the same thing as picking Exit, so the key that backs
        // out of every other menu also backs out of this one.
        public int ShowMenu()
        {
            Console.WriteLine();
            int choice = mainMenu.Choose();
            return choice == Menu.Cancelled ? Exit : choice;
        }

        public Phrase NewPhrase()
        {
            Console.WriteLine("What phrase would you like to add?");
            string phraseName = Console.ReadLine();
            phraseID = phraseID + 1;
            Phrase toBeAdded = new Phrase(phraseID, phraseName);
            return toBeAdded;
        }

        // Text pulled off a page arrives from the web rather than the keyboard,
        // but it takes the next ID in the same sequence as a typed phrase.
        public Phrase FetchedPhrase(string text)
        {
            phraseID = phraseID + 1;
            return new Phrase(phraseID, text);
        }

        public string AskForUrl()
        {
            Console.WriteLine("Which page should Talk Bot read?");
            return Console.ReadLine();
        }

        // Picks a phrase by walking the list instead of asking for an ID, and
        // returns Menu.Cancelled when the user backs out or has saved nothing
        // yet. A fetched page runs to thousands of characters, so the row shows
        // its opening and the detail line says how much more there is.
        public int ChoosePhrase(string title, Phrase[] phrases)
        {
            if (phrases.Length == 0)
            {
                Notice("No phrases saved yet.", ConsoleColor.Yellow);
                return Menu.Cancelled;
            }

            List<MenuItem> items = new List<MenuItem>();
            foreach (Phrase p in phrases)
            {
                string text = p.GetPhrase() == null ? "" : p.GetPhrase();
                items.Add(new MenuItem(p.GetId(), text, text.Length + " characters"));
            }

            Console.WriteLine();
            return new Menu(title, items).Choose();
        }

        // A page can run to thousands of characters, so it is shown before it
        // is read rather than only being heard: the user can see what arrived,
        // and still has it on the screen after the speech has been stopped.
        public void ShowText(string title, string text)
        {
            Console.WriteLine();
            Notice(title, ConsoleColor.Cyan);
            Console.WriteLine();
            foreach (string line in Wrap(text, LineWidth))
            {
                Console.WriteLine("  " + line);
            }
            Console.WriteLine();
        }

        private const int LineWidth = 76;

        // Breaks on spaces so a paragraph arriving as one long line still reads
        // as a paragraph. A word longer than the width is left to the terminal,
        // which is rare enough not to be worth cutting a word in half over.
        private static List<string> Wrap(string text, int width)
        {
            List<string> lines = new List<string>();
            foreach (string paragraph in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = "";
                foreach (string word in paragraph.Split(' '))
                {
                    if (line.Length > 0 && line.Length + 1 + word.Length > width)
                    {
                        lines.Add(line);
                        line = "";
                    }
                    line = line.Length == 0 ? word : line + " " + word;
                }
                lines.Add(line);
            }
            return lines;
        }

        // Voice, speed, pitch and volume, on one screen that shows what each is
        // set to now. The menu is rebuilt every pass because the labels carry
        // the current values, and it stays open until the user backs out, so
        // several dials can be tried against the preview without coming back
        // through the main menu each time.
        public void ConfigureVoice()
        {
            VoiceSettings settings = VoiceSettings.Current;
            ISpeechEngine engine = SpeechEngine.Current;
            Notice(engine.Describe(), ConsoleColor.DarkGray);

            while (true)
            {
                List<MenuItem> items = new List<MenuItem>();
                items.Add(new MenuItem(1, "Voice: " + settings.VoiceName,
                    "Pick from the voices this synthesizer has"));
                items.Add(new MenuItem(2, "Speed: " + Dial(settings.Speed),
                    "-10 is slow, 0 is normal, 10 is fast"));
                items.Add(new MenuItem(3, "Pitch: " + Dial(settings.Pitch),
                    "-10 is low, 0 is normal, 10 is high"));
                items.Add(new MenuItem(4, "Volume: " + settings.Volume,
                    "0 is silent, 100 is full"));
                items.Add(new MenuItem(5, "Preview", "Hear the settings as they stand"));
                items.Add(new MenuItem(6, "Reset", "Back to the voice this bot started with"));
                items.Add(new MenuItem(7, "Back", "Return to the main menu"));

                Console.WriteLine();
                int chosen = new Menu("VOICE SETTINGS", items).Choose();
                if (chosen == Menu.Cancelled || chosen == 7)
                {
                    return;
                }

                if (chosen == 1)
                {
                    ChooseVoice(settings, engine);
                }
                else if (chosen == 2)
                {
                    settings.Speed = AskForNumber("Speed", VoiceSettings.Slowest,
                        VoiceSettings.Fastest, settings.Speed);
                }
                else if (chosen == 3)
                {
                    settings.Pitch = AskForNumber("Pitch", VoiceSettings.Lowest,
                        VoiceSettings.Highest, settings.Pitch);
                }
                else if (chosen == 4)
                {
                    settings.Volume = AskForNumber("Volume", VoiceSettings.Quietest,
                        VoiceSettings.Loudest, settings.Volume);
                }
                else if (chosen == 5)
                {
                    engine.Speak(VoiceSettings.Sample);
                }
                else if (chosen == 6)
                {
                    settings.Reset();
                    Notice("Back to the default voice.", ConsoleColor.Green);
                }
            }
        }

        // Zero is worth marking as the normal setting, since a dial reading 0
        // otherwise looks like it has been turned all the way down.
        private static string Dial(int value)
        {
            return value == 0 ? "0 (normal)" : value.ToString();
        }

        private static void ChooseVoice(VoiceSettings settings, ISpeechEngine engine)
        {
            string[] voices = engine.AvailableVoices();
            if (voices.Length == 0)
            {
                Notice("This synthesizer only has the one voice.", ConsoleColor.Yellow);
                return;
            }

            // Value 0 is the default rather than a voice, so the user can get
            // back to it without knowing what it is called.
            List<MenuItem> items = new List<MenuItem>();
            items.Add(new MenuItem(0, "Default voice", "Whichever voice the synthesizer starts with"));
            for (int i = 0; i < voices.Length; i++)
            {
                items.Add(new MenuItem(i + 1, voices[i], ""));
            }

            Console.WriteLine();
            int chosen = new Menu("VOICE", items).Choose();
            if (chosen == Menu.Cancelled)
            {
                return;
            }

            settings.Voice = chosen == 0 ? null : voices[chosen - 1];
            Notice("Voice set to " + settings.VoiceName + ".", ConsoleColor.Green);
        }

        // Anything that is not a number leaves the setting where it was, which
        // is what pressing enter on an empty line does too.
        private static int AskForNumber(string label, int low, int high, int current)
        {
            Console.Write(label + " (" + low + " to " + high + ", now " + current + "): ");
            string entered = Console.ReadLine();
            int value;
            if (entered == null || !int.TryParse(entered.Trim(), out value))
            {
                Notice("Left at " + current + ".", ConsoleColor.DarkGray);
                return current;
            }

            int clamped = VoiceSettings.Clamp(value, low, high);
            Notice(label + " is now " + clamped + ".", ConsoleColor.Green);
            return clamped;
        }

        // Notices from the menu loop go through here so a success or a failure
        // reads as part of the same screen rather than a stray line.
        public static void Notice(string message, ConsoleColor color)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine("  " + message);
            Console.ForegroundColor = previous;
        }
    }
}
