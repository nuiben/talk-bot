using System;
using System.Collections.Generic;
using System.IO;

namespace talk.Tests
{
    // Drives the two prompts that read a line from the user - the number a
    // dial is set to, and the menu as it is offered when the console cannot be
    // drawn on - with the things people actually type at them.
    //
    // Both prompts are given a reader and a writer of their own: the reader so
    // the answers can be typed by the test, and the writer so the suite's
    // report is not buried under a dozen redrawn menus. What each prompt wrote
    // is kept, because being told why an answer was not taken is half of
    // handling it. The real console is put back afterwards, since the menu
    // opens as soon as this suite has finished.
    internal class ConsoleInputTest : ITest
    {
        public string Name
        {
            get { return "console input"; }
        }

        private string written = "";

        public bool Run()
        {
            Checks checks = new Checks();
            TextReader console = Console.In;
            TextWriter screen = Console.Out;
            try
            {
                CheckNumbers(checks);
                CheckMenu(checks);
            }
            finally
            {
                Console.SetIn(console);
                Console.SetOut(screen);
            }

            return checks.Report(Name, "typed numbers and menu choices are all handled");
        }

        // A number prompt is answered by hand, so most of what arrives is not a
        // number. None of it may change the setting or end the program.
        private void CheckNumbers(Checks checks)
        {
            checks.Equal("a number is taken", 5, Answer("5", 0));
            checks.Equal("a negative number is taken", -4, Answer("-4", 0));
            checks.Equal("a number with spaces around it is taken", 7, Answer("  7  ", 0));
            checks.Equal("a signed number is taken", 3, Answer("+3", 0));

            // Past the end of the dial, which is easy to type when the range is
            // -10 to 10 and the volume next to it runs to 100.
            checks.Equal("a number past the top stops at the top", 10, Answer("40", 0));
            checks.Equal("a number past the bottom stops at the bottom", -10,
                Answer("-40", 0));

            // Anything that is not a number leaves the setting alone, which is
            // the only answer that cannot surprise anyone.
            checks.Equal("an empty line leaves the setting", 6, Answer("", 6));
            checks.Equal("spaces leave the setting", 6, Answer("   ", 6));
            checks.Equal("a word leaves the setting", 6, Answer("fast", 6));
            checks.Equal("half a number leaves the setting", 6, Answer("5fast", 6));
            checks.Equal("a decimal leaves the setting", 6, Answer("2.5", 6));
            checks.Equal("a number too big to hold leaves the setting", 6,
                Answer("99999999999999999999", 6));
            checks.Equal("a line of punctuation leaves the setting", 6, Answer("!!!", 6));

            // Being left alone silently would read as the setting having been
            // taken, so the prompt has to say what it did.
            checks.True("a refused answer says the setting was left alone",
                written.Contains("Left at 6"));

            // The end of a piped input arrives as no line at all rather than as
            // an empty one.
            checks.Equal("the end of the input leaves the setting", 6, Answer(null, 6));

            // The prompt has to carry the range and the current value, or the
            // only way to find out what may be typed is to type something.
            Answer("0", 3);
            checks.True("the prompt gives the range", written.Contains("-10 to 10"));
            checks.True("the prompt gives the current value", written.Contains("now 3"));
        }

        // The typed form of the menu, which is what a piped input or a
        // redirected console gets instead of the arrow keys.
        private void CheckMenu(Checks checks)
        {
            checks.Equal("a listed choice is taken", 7, Choose("7\n"));
            checks.Equal("a choice with spaces around it is taken", 1, Choose(" 1 \n"));

            // A number that is not on the menu, a word, and an empty line all
            // ask again rather than counting as a choice, so each of these is
            // followed by a real answer.
            checks.Equal("a number that is not on the menu asks again", 1, Choose("4\n1\n"));
            checks.True("and says so", written.Contains("not one of the choices"));
            checks.Equal("a word asks again", 1, Choose("exit\n1\n"));
            checks.Equal("an empty line asks again", 1, Choose("\n1\n"));

            // The rows are numbered by what they return, not by where they sit,
            // so the second row is 7 rather than 2.
            checks.Equal("a row's position rather than its number asks again", 7,
                Choose("2\n7\n"));

            // The end of the input has to end the menu, or a piped run spins on
            // a reader with nothing left to give.
            checks.Equal("the end of the input backs out", Menu.Cancelled, Choose(""));
            checks.Equal("a wrong answer then the end of the input backs out",
                Menu.Cancelled, Choose("nope\n"));

            // Every row has to be offered, since a choice that is not printed
            // cannot be typed.
            checks.True("the menu lists what can be chosen",
                written.Contains("Add a phrase") && written.Contains("Exit"));
        }

        private int Answer(string typed, int current)
        {
            Console.SetIn(new StringReader(typed == null ? "" : typed + "\n"));
            return (int)Capture(delegate
            {
                return ConsoleView.AskForNumber("Speed", VoiceSettings.Slowest,
                    VoiceSettings.Fastest, current);
            });
        }

        private int Choose(string typed)
        {
            List<MenuItem> items = new List<MenuItem>();
            items.Add(new MenuItem(1, "Add a phrase", ""));
            items.Add(new MenuItem(7, "Exit", ""));

            Console.SetIn(new StringReader(typed));
            return (int)Capture(delegate
            {
                return new Menu("TALK BOT", items).ChooseByTyping();
            });
        }

        // Runs one prompt with the screen pointed at a string, so what it wrote
        // can be checked and the suite's own report stays readable.
        private object Capture(Func<object> prompt)
        {
            TextWriter screen = Console.Out;
            StringWriter caught = new StringWriter();
            try
            {
                Console.SetOut(caught);
                return prompt();
            }
            finally
            {
                Console.SetOut(screen);
                written = caught.ToString();
            }
        }
    }
}
