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

            ConsoleView view = new ConsoleView();
            PhraseLibrary model = new PhraseLibrary();
            int userSelection;
            do
            {
                userSelection = view.ShowMenu();
                if (userSelection == 1)
                {
                    // Null when the user typed nothing, which the view has
                    // already said so on screen.
                    Phrase typed = view.NewPhrase();
                    if (typed != null)
                    {
                        model.AddPhrase(typed);
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
                }
                else if (userSelection == ConsoleView.Pengy)
                {
                    // Spelling out the mascot's name at the menu saves his
                    // story as a phrase. It is not read straight away: the
                    // point is that it turns up in the list, for whoever went
                    // looking to find.
                    ConsoleView.Notice("Pengy waddles in.", ConsoleColor.Cyan);
                    AddFromWeb(view, model, PengyStory.PageUrl, false);
                }
            }
            while (userSelection != ConsoleView.Exit);

            return 0;
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

        // Saves a page as a phrase, and reads it straight away when the user
        // asked for the page by name. The text is put on the screen first, so
        // the user can see what came off the page while it is being read, and
        // still has it there once the speech has finished or been stopped. A
        // browser, network or content problem should return to the menu, not
        // take down the program.
        private static void AddFromWeb(ConsoleView view, PhraseLibrary model, string url,
            bool speakNow)
        {
            try
            {
                Phrase phrase = view.FetchedPhrase(WebPage.Read(url));
                model.AddPhrase(phrase);
                ConsoleView.Notice("Saved " + phrase.GetPhrase().Length +
                    " characters as phrase " + phrase.GetId() + ".", ConsoleColor.Green);
                if (speakNow)
                {
                    view.ShowText(url, phrase.GetPhrase());
                    phrase.Play();
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
