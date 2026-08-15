using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using talk.Tests;

namespace talk
{
    class Program
    {
        // MVC Menu Screen
        static int Main(string[] args)
        {
            // --test runs the suite and nothing else, for a build or a CI job
            // that only wants the exit code. --quick leaves out the page tests,
            // which are the browser and the network and so all of the minute
            // the suite takes; what is left is the input handling, which is
            // worth running on every change.
            bool quick = HasFlag(args, "--quick");
            if (HasFlag(args, "--test"))
            {
                return TestSuite.Run(!quick);
            }

            // Otherwise QA runs on the way to the menu, so a fresh build is
            // checked without anyone having to remember to check it. The tests
            // drive a real browser against real pages, so they take a minute
            // and need the network; --noqa skips them when neither is worth it.
            if (!HasFlag(args, "--noqa"))
            {
                ConsoleView.Notice("Running QA - pass --noqa to skip.", ConsoleColor.DarkGray);
                if (TestSuite.Run(!quick) != 0)
                {
                    // A failing page check says nothing about whether the menu
                    // works, so it is reported and stepped past rather than
                    // keeping the user out of the program.
                    ConsoleView.Notice("QA reported problems. Carrying on to the menu.",
                        ConsoleColor.Yellow);
                }
            }

            // Settings first, because the engine they name is the one the menu
            // then describes, and a phrase spoken before the settings screen is
            // ever opened should be in the voice the user last chose.
            LoadSettings();

            ConsoleView view = new ConsoleView();
            PhraseLibrary model = new PhraseLibrary(LoadPhrases());
            int userSelection;
            do
            {
                userSelection = view.ShowMenu();
                if (userSelection == 1)
                {
                    // Null when the user typed nothing, which the view has
                    // already said so on screen.
                    Phrase typed = view.NewPhrase(model.TakeNextId());
                    if (typed != null)
                    {
                        model.AddPhrase(typed);
                        SavePhrases(model);
                    }
                }
                else if (userSelection == 2)
                {
                    // Cancelled means the user backed out of the picker, which
                    // is not a failure worth reporting.
                    int chosen = view.ChoosePhrase("REMOVE", model.ListPhrases());
                    if (chosen != Menu.Cancelled && model.RemovePhrase(chosen))
                    {
                        ConsoleView.Notice("Removed.", ConsoleColor.Green);
                        SavePhrases(model);
                    }
                }
                else if (userSelection == 3)
                {
                    int chosen = view.ChoosePhrase("SPEAK", model.ListPhrases());
                    if (chosen != Menu.Cancelled)
                    {
                        model.PlayPhrase(chosen);
                    }
                }
                else if (userSelection == 4)
                {
                    AddFromWeb(view, model, view.AskForUrl(), true);
                }
                else if (userSelection == 5)
                {
                    view.ConfigureVoice();
                    // Written on the way out of the screen rather than on every
                    // turn of a dial, since the arrows now change a setting on
                    // each keypress and none of them is the one the user has
                    // settled on until they leave.
                    SaveSettings();
                }
                else if (userSelection == ConsoleView.Pengy)
                {
                    // Spelling out the mascot's name at the menu saves his
                    // story as a phrase, without putting it on the screen: the
                    // point is that it turns up in the list, for whoever went
                    // looking to find.
                    ConsoleView.Notice("Pengy waddles in.", ConsoleColor.Cyan);
                    AddFromWeb(view, model, PengyStory.PageUrl, false);
                }
            }
            while (userSelection != ConsoleView.Exit);

            return 0;
        }

        // A config file that cannot be read is reported and stepped past, and
        // deliberately not written over on the way past: a file that is there
        // but unreadable is more likely a mistake worth keeping than settings
        // worth losing. The next visit to the settings screen writes it.
        private static void LoadSettings()
        {
            try
            {
                SettingsStore.Load();
            }
            catch (SettingsStoreException e)
            {
                ConsoleView.Notice("Saved settings could not be loaded: " + e.Message,
                    ConsoleColor.Yellow);
            }
        }

        private static void SaveSettings()
        {
            try
            {
                SettingsStore.Save();
            }
            catch (SettingsStoreException e)
            {
                ConsoleView.Notice("Settings could not be saved: " + e.Message,
                    ConsoleColor.Yellow);
            }
        }

        // A phrase file that cannot be read is reported and stepped past, in
        // the way a failing page check is: the menu still works without it. It
        // is deliberately not written over on the way past - a file that is
        // there but unreadable is more likely a mistake worth keeping than
        // phrases worth losing, and the next save says so.
        private static Phrase[] LoadPhrases()
        {
            try
            {
                Phrase[] saved = PhraseStore.Load();
                if (saved.Length > 0)
                {
                    ConsoleView.Notice("Loaded " + saved.Length + " saved " +
                        (saved.Length == 1 ? "phrase." : "phrases."), ConsoleColor.DarkGray);
                }
                return saved;
            }
            catch (PhraseStoreException e)
            {
                ConsoleView.Notice("Saved phrases could not be loaded: " + e.Message,
                    ConsoleColor.Yellow);
                return new Phrase[0];
            }
        }

        // Written after each change rather than on the way out, because the way
        // out is not always taken: the window gets closed on a program that
        // spends most of its time waiting for a keypress.
        private static void SavePhrases(PhraseLibrary model)
        {
            try
            {
                PhraseStore.Save(model.ListPhrases());
            }
            catch (PhraseStoreException e)
            {
                ConsoleView.Notice("Phrases could not be saved: " + e.Message,
                    ConsoleColor.Yellow);
            }
        }

        // Flags are looked for anywhere in the arguments rather than only in
        // first position, so "--noqa --test" and "--test --noqa" behave alike.
        private static bool HasFlag(string[] args, string flag)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Saves a page as a phrase. Nothing is spoken here: a page runs to
        // thousands of characters, and having the whole of one start reading
        // the moment it is added left the user waiting on speech they had not
        // asked for. It goes into the list like any other phrase and is read
        // from Say a phrase, when they choose it.
        //
        // The text is still put on the screen when the user asked for the page
        // themselves, so they can see what came off it. Pengy's story is not
        // shown, because the point of it is finding it in the list. A browser,
        // network or content problem should return to the menu, not take down
        // the program.
        private static void AddFromWeb(ConsoleView view, PhraseLibrary model, string url,
            bool showText)
        {
            try
            {
                // Read first, so a page that cannot be fetched does not use up
                // an ID on the way to being reported.
                string text = WebPage.Read(url);
                Phrase phrase = view.FetchedPhrase(model.TakeNextId(), text);
                model.AddPhrase(phrase);
                SavePhrases(model);
                ConsoleView.Notice("Saved " + phrase.GetPhrase().Length +
                    " characters as phrase " + phrase.GetId() + ".", ConsoleColor.Green);
                if (showText)
                {
                    view.ShowText(url, phrase.GetPhrase());
                }
            }
            catch (PageNotReadableException e)
            {
                ConsoleView.Notice("Nothing to read: " + e.Message, ConsoleColor.Yellow);
            }
            catch (WebDriverException e)
            {
                ConsoleView.Notice("That page could not be opened: " + e.Message,
                    ConsoleColor.Red);
            }
        }
    }
}
