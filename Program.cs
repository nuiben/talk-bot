using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace talk
{
    class Program
    {
        // MVC Menu Screen
        static void Main(string[] args)
        {

            ConsoleView view = new ConsoleView();
            PhraseLibrary model = new PhraseLibrary();
            Test testNode = new Test();
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
                    // not take down the program. finally closes the browser
                    // either way.
                    try
                    {
                        testNode.Initialize();
                        testNode.ExecuteTest();
                    }
                    catch (WebDriverException e)
                    {
                        Console.WriteLine("Test could not run: " + e.Message);
                    }
                    finally
                    {
                        testNode.ClearMemory();
                    }
                }
            }
            while (userSelection != 5);
        }
    }
}
