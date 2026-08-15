using System;

namespace talk.Tests
{
    // A menu redraws itself by winding the cursor back over the lines it wrote,
    // so everything it writes has to still be on the screen. The voice list is
    // two dozen entries long and used to be drawn whole, which scrolled the top
    // of it away and left every redraw after that in the wrong place.
    //
    // The window that fixes it is arithmetic, so it can be checked here rather
    // than by watching a terminal.
    internal class MenuWindowTest : ITest
    {
        private const int Page = 6;

        public string Name
        {
            get { return "menu window"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            // How much of a list is shown. Six at a time, except that a list
            // one row too long is shown whole: scrolling a seven row menu to
            // save one line hides a row for nothing.
            checks.Equal("a short list is shown whole", 3, Menu.Shown(3));
            checks.Equal("a full window is shown whole", 6, Menu.Shown(6));
            checks.Equal("one row too many is still shown whole", 7, Menu.Shown(7));
            checks.Equal("two rows too many scroll", 6, Menu.Shown(8));
            checks.Equal("a long list scrolls", 6, Menu.Shown(24));

            // A list that fits never scrolls, whichever row is highlighted.
            for (int row = 0; row < Page; row++)
            {
                checks.Equal("a list of six starts at the top on row " + row, 0,
                    Menu.FirstVisible(row, 0, Page, Page));
            }
            checks.Equal("a list shorter than the window starts at the top", 0,
                Menu.FirstVisible(2, 0, 3, Page));

            // Walking down a long list: the window sits still until the
            // highlight reaches the bottom of it, then follows a row at a time.
            checks.Equal("the top of a long list is the top", 0,
                Menu.FirstVisible(0, 0, 24, Page));
            checks.Equal("the last row of the window does not scroll", 0,
                Menu.FirstVisible(5, 0, 24, Page));
            checks.Equal("one row further scrolls by one", 1,
                Menu.FirstVisible(6, 0, 24, Page));
            checks.Equal("and again", 2, Menu.FirstVisible(7, 1, 24, Page));

            // And back up, which is the case that made the list jump about when
            // the window only ever followed downwards.
            checks.Equal("coming back up does not move the window yet", 4,
                Menu.FirstVisible(5, 4, 24, Page));
            checks.Equal("until the highlight is above it", 3,
                Menu.FirstVisible(3, 4, 24, Page));

            // The ends. The last row is the bottom of the window rather than
            // the top, or the list would show five blank lines under it.
            checks.Equal("the last row fills the window", 18,
                Menu.FirstVisible(23, 0, 24, Page));
            checks.Equal("wrapping to the first row goes back to the top", 0,
                Menu.FirstVisible(0, 18, 24, Page));

            // A window left past the end - by a list that has had rows removed
            // from it, or a jump to the bottom and back - is pulled back in.
            checks.Equal("a window past the end is pulled back", 18,
                Menu.FirstVisible(20, 22, 24, Page));
            checks.Equal("a window past a short list is pulled back", 0,
                Menu.FirstVisible(0, 5, 3, Page));

            // Whatever the window is, the highlight has to be inside it, or
            // the user is walking a list they cannot see.
            for (int count = 1; count <= 30; count++)
            {
                int window = 0;
                for (int row = 0; row < count; row++)
                {
                    int shown = Menu.Shown(count);
                    window = Menu.FirstVisible(row, window, count, shown);
                    checks.True("row " + row + " of " + count + " is in the window",
                        row >= window && row < window + shown);
                    checks.True("the window of " + count + " stays in the list",
                        window >= 0 && window + shown <= count);
                }
            }

            return checks.Report(Name, "long lists scroll a row at a time and stay on screen");
        }
    }
}
