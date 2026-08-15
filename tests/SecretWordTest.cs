using System;
using System.Collections.Generic;
using System.IO;

namespace talk.Tests
{
    // The mascot's story used to have a row on the menu. It is now a word that
    // is not shown anywhere, so this is the only place that says what it is
    // and what it should still answer to: the case it is typed in, spaces
    // around it, and a wrong letter before it.
    //
    // It also checks the word stays out of the way, because a secret that
    // catches ordinary answers is a bug rather than an easter egg.
    internal class SecretWordTest : ITest
    {
        public string Name
        {
            get { return "secret word"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();
            TextReader console = Console.In;
            TextWriter screen = Console.Out;
            try
            {
                checks.Equal("the word is taken", ConsoleView.Pengy, Choose("pengy\n"));
                checks.Equal("shouting works too", ConsoleView.Pengy, Choose("PENGY\n"));
                checks.Equal("so does a capital letter", ConsoleView.Pengy,
                    Choose("Pengy\n"));
                checks.Equal("spaces around it are ignored", ConsoleView.Pengy,
                    Choose("  pengy  \n"));

                // Spelled out at the arrow menu, a wrong letter cannot be taken
                // back, so what counts is how the typing ends.
                checks.Equal("a wrong letter before it does not spoil it",
                    ConsoleView.Pengy, Choose("xpengy\n"));

                // Half the word, and the word with something after it, are
                // ordinary wrong answers: the menu asks again rather than
                // taking either.
                checks.Equal("half the word is not the word", 1, Choose("peng\n1\n"));
                checks.Equal("the word with more after it is not the word", 1,
                    Choose("pengyy\n1\n"));
                checks.Equal("the mascot's other name is not the word", 1,
                    Choose("penguin\n1\n"));

                // The rows that are on the menu still answer to their numbers,
                // and nothing about the secret is drawn on the screen.
                checks.Equal("a listed choice still works", 3, Choose("3\n"));
                checks.True("the secret is not shown on the menu",
                    !written.ToLowerInvariant().Contains("pengy"));

                // At the arrow menu the word is spelled out a key at a time,
                // and the same matcher decides when it has been. Nothing there
                // can be taken back, so only the end of the typing counts.
                Menu menu = ConsoleView.MainMenu();
                checks.Equal("spelled out at the menu", ConsoleView.Pengy,
                    menu.Secret("pengy"));
                checks.Equal("part of the way through", Menu.Cancelled,
                    menu.Secret("peng"));
                checks.Equal("after a keyboard full of other letters",
                    ConsoleView.Pengy, menu.Secret("wertypengy"));
                checks.Equal("nothing typed at all", Menu.Cancelled, menu.Secret(""));
                checks.Equal("no typing at all", Menu.Cancelled, menu.Secret(null));

                // A menu with no secret has to behave as it always did.
                List<MenuItem> rows = new List<MenuItem>();
                rows.Add(new MenuItem(1, "Add a phrase", ""));
                checks.Equal("a menu without a secret has none", Menu.Cancelled,
                    new Menu("PLAIN", rows).Secret("pengy"));
            }
            finally
            {
                Console.SetIn(console);
                Console.SetOut(screen);
            }

            return checks.Report(Name, "the mascot answers to his name and nothing else");
        }

        private string written = "";

        // The main menu as the view builds it, so the test uses the same rows
        // and the same secret the program does rather than a copy of them.
        // The typed form is asked for by name: whether the arrow keys are used
        // instead depends on the console the suite happens to be run from, and
        // a test that waits for a keystroke would never finish.
        private int Choose(string typed)
        {
            Console.SetIn(new StringReader(typed));
            TextWriter screen = Console.Out;
            StringWriter caught = new StringWriter();
            try
            {
                Console.SetOut(caught);
                return ConsoleView.MainMenu().ChooseByTyping();
            }
            finally
            {
                Console.SetOut(screen);
                written = caught.ToString();
            }
        }
    }
}
