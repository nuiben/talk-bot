using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    // finally, so a failed test still closes the browser.
                    try
                    {
                        testNode.Initialize();
                        testNode.ExecuteTest();
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
