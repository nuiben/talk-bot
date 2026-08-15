using System;
using System.Collections.Generic;

namespace talk.Tests
{
    // A page is put on the screen before it is read, and what comes off a page
    // is not what anyone would type: one paragraph can arrive as a single line
    // thousands of characters long, and a page can hold a word longer than the
    // terminal is wide.
    internal class TextDisplayTest : ITest
    {
        private const int Width = 40;

        public string Name
        {
            get { return "page text display"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            // A page that could not be read leaves nothing to show, and that
            // has to be nothing rather than a crash on the way to the menu.
            checks.Equal("no text is no lines", 0, ConsoleView.Wrap(null, Width).Count);
            checks.Equal("empty text is one empty line", 1,
                ConsoleView.Wrap("", Width).Count);

            // The common case: one long line, broken on spaces, with every line
            // inside the width.
            string paragraph = "Pengy is a penguin who lives in the freezer aisle of a " +
                "supermarket, which is not where penguins are supposed to live.";
            List<string> wrapped = ConsoleView.Wrap(paragraph, Width);
            checks.True("a long line is broken up", wrapped.Count > 1);
            foreach (string line in wrapped)
            {
                checks.True("\"" + line + "\" fits the width", line.Length <= Width);
            }
            checks.Equal("every word is kept", Words(paragraph), Words(Join(wrapped)));

            // Blank lines are what separate paragraphs, so they have to survive
            // or the whole page runs together.
            List<string> paragraphs = ConsoleView.Wrap("first\n\nsecond", Width);
            checks.Equal("paragraphs stay apart", 3, paragraphs.Count);
            checks.Equal("the blank line is kept", "", paragraphs[1]);

            // Pages carry both line endings, and a stray carriage return would
            // otherwise print as a control character.
            List<string> windows = ConsoleView.Wrap("first\r\nsecond", Width);
            checks.Equal("windows line endings split the line", 2, windows.Count);
            checks.Equal("no carriage return is left behind", "first", windows[0]);

            // A word wider than the terminal cannot be broken on a space, so it
            // gets a line of its own rather than being dropped or cut in half.
            string long_word = new string('x', Width * 2);
            List<string> single = ConsoleView.Wrap("a " + long_word + " b", Width);
            checks.True("a word longer than the width is kept whole",
                Join(single).Contains(long_word));

            // A line exactly the width has to fit rather than being wrapped one
            // word early.
            List<string> exact = ConsoleView.Wrap(new string('x', Width), Width);
            checks.Equal("a line of exactly the width is one line", 1, exact.Count);

            return checks.Report(Name, "page text wraps without losing or mangling words");
        }

        private static string Join(List<string> lines)
        {
            return string.Join(" ", lines.ToArray());
        }

        private static int Words(string text)
        {
            return TestSuite.Flatten(text).Split(' ').Length;
        }
    }
}
