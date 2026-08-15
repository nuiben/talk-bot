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
            // The tests are no longer on the menu, so they run from the command
            // line instead and hand their result back as the exit code.
            if (args.Length > 0 && args[0] == "--test")
            {
                return TestSuite.Run();
            }

            ConsoleView view = new ConsoleView();
            PhraseLibrary model = new PhraseLibrary();
            int userSelection;
            do
            {
                userSelection = view.ShowMenu();
                if (userSelection == 1)
                {
                    model.AddPhrase(view.NewPhrase());
                }
                else if (userSelection == 2)
                {
                    model.RemovePhrase(view.DeletePhrase());
                }
                else if (userSelection == 3)
                {
                    view.DisplayPhrases(model.ListPhrases());
                    model.PlayPhrase(view.PlayPhrase());
                }
                else if (userSelection == 4)
                {
                    // A browser or network problem should return to the menu,
                    // not take down the program.
                    try
                    {
                        model.AddPhrase(view.StoryPhrase(PengyStory.Fetch()));
                        Console.WriteLine("Added \"" + PengyStory.Title +
                            "\" to the phrase list.");
                    }
                    catch (WebDriverException e)
                    {
                        Console.WriteLine("Pengy's story could not be fetched: " + e.Message);
                    }
                }
            }
            while (userSelection != 5);

            return 0;
        }
    }
}
