using System;
using System.Collections.Generic;

namespace talk
{
    // One row of a Menu. Value is what the caller gets back when the row is
    // chosen, so it can be a menu number or a phrase ID. Detail is the line the
    // row opens to show while it is highlighted, and may be empty.
    internal class MenuItem
    {
        private readonly int value;
        private readonly string label;
        private readonly string detail;

        public MenuItem(int newValue, string newLabel, string newDetail)
        {
            value = newValue;
            label = newLabel;
            detail = newDetail == null ? "" : newDetail;
        }

        public int Value
        {
            get { return value; }
        }

        public string Label
        {
            get { return label; }
        }

        public string Detail
        {
            get { return detail; }
        }
    }

    // A list the user walks with the arrow keys rather than typing a number.
    // The screen only ever explains the one choice the user is looking at: the
    // highlighted row's detail is written to a slot kept below the list.
    //
    // The frame is the same height on every draw, which is what makes it read
    // as a pane with a highlight moving inside it rather than a list that
    // reflows under the cursor. The detail used to open and close between the
    // rows, accordion style, which pushed every row below it down a line on
    // every keystroke, and the two "more above/below" lines came and went at
    // the ends of a long list, which moved the footer as well. The detail now
    // has a line of its own whether or not the row has anything to say, and
    // the scroll marks live in the two columns to the left of the rows, so
    // neither costs a line that is sometimes there and sometimes not.
    //
    // Redrawing works by counting the lines written and winding the cursor back
    // over them, so the menu updates in place instead of scrolling a fresh copy
    // down the terminal on every keystroke. Because the height is fixed and
    // every line is padded to the full width, a redraw overwrites the frame
    // where it stands: blanking it first would show through as a flicker.
    internal class Menu
    {
        // Returned when the user presses escape rather than choosing.
        public const int Cancelled = -1;

        private const int Width = 46;

        // The first two columns carry the scroll marks, so a row is drawn into
        // what is left of the line.
        private const int Gutter = 2;

        private const int RowWidth = Width - Gutter;

        // Lines the frame writes besides the rows themselves: the title rule,
        // the blank under the list, the detail slot, the bottom rule and the
        // key hints. Fixed, so the whole frame is.
        private const int Furniture = 5;

        // How many rows are shown for a list of this length. A list one row too
        // long is shown whole rather than scrolled for the sake of that row:
        // the voice settings are seven rows, and hiding the last of them behind
        // a scroll to save a single line helps nobody.
        internal static int Shown(int count)
        {
            return count <= PageSize + 1 ? count : PageSize;
        }

        // Rows shown at once. A menu is redrawn by winding the cursor back over
        // the lines it wrote, which only works while all of them are still on
        // the screen: a list longer than the terminal scrolls the top of itself
        // away, and every redraw after that lands in the wrong place and leaves
        // a trail of half-erased menus. The voice list runs to two dozen
        // entries, so it is shown a window at a time instead.
        private const int PageSize = 6;

        // The row the window starts at, kept between redraws so the list does
        // not jump back to the top on every keystroke.
        private int first;

        private const ConsoleColor Accent = ConsoleColor.Cyan;

        private readonly string title;
        private readonly List<MenuItem> items;

        // Words that are not on the menu and return a value of their own when
        // they are typed. Nothing about them is drawn, which is the point.
        private readonly Dictionary<string, int> secrets =
            new Dictionary<string, int>();

        // What has been typed since the last key that was not a letter, so a
        // word can be spelled out at a menu that is otherwise driven by arrows.
        private string spelled = "";

        private int selected;

        public Menu(string newTitle, List<MenuItem> newItems)
        {
            title = newTitle;
            items = newItems;
        }

        // A word that works at this menu without appearing on it. It is matched
        // whichever way the menu is being driven: spelled out a letter at a
        // time against the arrow keys, or typed as the whole answer when the
        // console is redirected.
        public void AddSecret(string word, int value)
        {
            secrets[word.ToLowerInvariant()] = value;
        }

        // The value a word is worth, or Cancelled when it is not a secret. The
        // end of what was typed is what counts, so a mistyped letter can be
        // followed by the word itself rather than having to be undone.
        internal int Secret(string typed)
        {
            string lower = typed == null ? "" : typed.ToLowerInvariant().Trim();
            foreach (KeyValuePair<string, int> secret in secrets)
            {
                if (lower.EndsWith(secret.Key, StringComparison.Ordinal))
                {
                    return secret.Value;
                }
            }
            return Cancelled;
        }

        // The row that was highlighted last time, so reopening a menu puts the
        // user back where they were instead of at the top.
        public int Selected
        {
            get { return selected; }
            set { selected = Clamp(value); }
        }

        public int Choose()
        {
            if (items.Count == 0)
            {
                return Cancelled;
            }

            // A redirected console has no keys to read and no cursor to wind
            // back, which is how this runs under piped input or a test, so the
            // same list is offered as a typed prompt there.
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return ChooseByTyping();
            }

            // The cursor has nothing to say while a menu is up - there is
            // nothing to type at - and left visible it sits blinking under the
            // frame, competing with the highlight for the eye. It is put back
            // however this returns, including by way of an exception, since a
            // terminal left without a cursor is worse than the flicker.
            Console.CursorVisible = false;
            try
            {
                return Walk();
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }

        private int Walk()
        {
            // The height of the frame as it currently stands on the screen, and
            // so how far the next draw has to wind back to land on it. Zero
            // until the first draw, which has nothing to wind back over.
            int height = 0;

            while (true)
            {
                height = Draw(height);
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow || key.KeyChar == 'k')
                {
                    // Wrapping means holding one arrow always reaches every
                    // row, including Exit at the bottom.
                    selected = (selected - 1 + items.Count) % items.Count;
                }
                else if (key.Key == ConsoleKey.DownArrow || key.KeyChar == 'j')
                {
                    selected = (selected + 1) % items.Count;
                }
                else if (key.Key == ConsoleKey.Home || key.Key == ConsoleKey.PageUp)
                {
                    selected = 0;
                }
                else if (key.Key == ConsoleKey.End || key.Key == ConsoleKey.PageDown)
                {
                    selected = items.Count - 1;
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
                {
                    // The frame is only taken off the screen on the way out.
                    // Moving the highlight leaves it where it is and draws
                    // over it.
                    Erase(height);
                    Echo(items[selected]);
                    return items[selected].Value;
                }
                else if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q')
                {
                    Erase(height);
                    return Cancelled;
                }
                else if (key.KeyChar >= '1' && key.KeyChar <= '9')
                {
                    // The old numbers still work for anyone with them in their
                    // fingers, but they jump the highlight rather than commit,
                    // so a mistyped digit costs nothing.
                    int index = key.KeyChar - '1';
                    if (index < items.Count)
                    {
                        selected = index;
                    }
                }
                else if (char.IsLetter(key.KeyChar))
                {
                    // Letters otherwise do nothing here, so they are collected
                    // in case they are spelling out a word the menu does not
                    // show. j, k and q have already been dealt with above,
                    // which is why no secret may contain one.
                    spelled = spelled + char.ToLowerInvariant(key.KeyChar);
                    if (spelled.Length > 32)
                    {
                        // Only the tail can ever match, so the rest is dropped
                        // rather than kept for a menu that stays open all day.
                        spelled = spelled.Substring(spelled.Length - 32);
                    }
                    int secret = Secret(spelled);
                    if (secret != Cancelled)
                    {
                        spelled = "";
                        Erase(height);
                        return secret;
                    }
                }
            }
        }

        // Draws the frame over the one already on the screen, winding the
        // cursor back over the given number of lines to find it, and returns
        // the number of lines written so the next draw can do the same.
        private int Draw(int rewind)
        {
            if (rewind > 0)
            {
                Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - rewind));
            }

            int shown = Shown(items.Count);
            first = FirstVisible(selected, first, items.Count, shown);
            int below = items.Count - (first + shown);

            Fill(Rule(title), Accent);

            for (int i = first; i < first + shown; i++)
            {
                // A list that just stops looks like the whole of it, so the
                // ends of a window that has more beyond them are marked.
                string mark = "  ";
                if (i == first && first > 0)
                {
                    mark = "^ ";
                }
                else if (i == first + shown - 1 && below > 0)
                {
                    mark = "v ";
                }
                Row(items[i], i == selected, mark);
            }

            Fill("", ConsoleColor.Gray);
            Fill("    " + Fit(items[selected].Detail, Width - 4), ConsoleColor.DarkGray);

            // Which row of how many, for a list too long to show at once. A
            // short list is all on the screen and can be counted by eye.
            Fill(Rule(items.Count > shown
                ? (selected + 1) + " of " + items.Count
                : ""), Accent);
            Fill("  up/down move    enter select    esc back", ConsoleColor.DarkGray);
            return shown + Furniture;
        }

        // One row, and the two columns of gutter to its left that carry the
        // scroll marks.
        private static void Row(MenuItem item, bool highlighted, string mark)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(mark);
            Console.ForegroundColor = previous;

            if (highlighted)
            {
                Highlight("> " + Fit(item.Label, RowWidth - 2));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(("  " + Fit(item.Label, RowWidth - 2)).PadRight(RowWidth));
                Console.ForegroundColor = previous;
                Console.WriteLine();
            }
        }

        // Which row the window starts at. The window only moves when the
        // highlight would leave it, so walking a long list scrolls a row at a
        // time from either end rather than jumping about, and a short list
        // never scrolls at all.
        internal static int FirstVisible(int selected, int first, int count, int pageSize)
        {
            int shown = Math.Min(pageSize, count);
            if (selected < first)
            {
                first = selected;
            }
            if (selected >= first + shown)
            {
                first = selected - shown + 1;
            }

            // Wrapping from the bottom row to the top, or a list that has had
            // rows taken out of it, can leave the window past the end.
            if (first > count - shown)
            {
                first = count - shown;
            }
            if (first < 0)
            {
                first = 0;
            }
            return first;
        }

        // Leaves one line behind saying what was picked, so the transcript
        // above the next screen still reads as a record of what happened.
        private static void Echo(MenuItem item)
        {
            Line("  > " + item.Label, ConsoleColor.Green);
        }

        private static void Erase(int lines)
        {
            int top = Math.Max(0, Console.CursorTop - lines);
            Console.SetCursorPosition(0, top);
            string blank = new string(' ', Width + 6);
            for (int i = 0; i < lines; i++)
            {
                Console.WriteLine(blank);
            }
            Console.SetCursorPosition(0, top);
        }

        // A line of the frame, padded to the full width so that drawing it
        // covers whatever the last draw left on that line. Nothing inside the
        // frame is written any other way, which is what lets a redraw skip
        // blanking the screen first.
        // Callers cut their own text to length with Fit, which trims, so the
        // indent has to be put on afterwards and cannot be cut here.
        private static void Fill(string text, ConsoleColor color)
        {
            Line(text.PadRight(Width), color);
        }

        private static void Line(string text, ConsoleColor color)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = previous;
        }

        // The highlighted row is drawn as a filled bar rather than colored
        // text, so it stays obvious on light and dark terminals alike.
        //
        // The bar is dark cyan even though the rules are bright cyan. Black on
        // bright cyan is close enough to failing a contrast check that Windows
        // Terminal and iTerm2 "fix" it for you, quietly putting the text back
        // to white; against dark cyan the black is left alone. The foreground
        // is set before the background so the terminal is told the text color
        // first and the pair lands in one state, rather than the row briefly
        // being black on black.
        private static void Highlight(string text)
        {
            ConsoleColor previousFore = Console.ForegroundColor;
            ConsoleColor previousBack = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.Write(Fit(text, RowWidth).PadRight(RowWidth));
            Console.ForegroundColor = previousFore;
            Console.BackgroundColor = previousBack;
            Console.WriteLine();
        }

        // Nothing may wrap: a wrapped row would take two console lines and
        // leave Erase winding back one line short of the top.
        private static string Fit(string text, int width)
        {
            string oneLine = string.Join(" ", text.Split('\n')).Trim();
            if (oneLine.Length <= width)
            {
                return oneLine;
            }
            return oneLine.Substring(0, Math.Max(0, width - 3)) + "...";
        }

        private static string Rule(string heading)
        {
            if (heading.Length == 0)
            {
                return new string('=', Width);
            }
            string label = " " + heading + " ";
            int left = (Width - label.Length) / 2;
            return new string('=', left) + label + new string('=', Width - left - label.Length);
        }

        internal int ChooseByTyping()
        {
            Line(Rule(title), Accent);
            foreach (MenuItem item in items)
            {
                Console.WriteLine("  " + item.Value + ") " + item.Label);
            }
            Line(Rule(""), Accent);

            while (true)
            {
                Console.Write("Make a selection: ");
                string entered = Console.ReadLine();
                if (entered == null)
                {
                    return Cancelled;
                }

                // A word that is not on the menu is checked before the numbers
                // are, since none of the numbers can spell one.
                int secret = Secret(entered);
                if (secret != Cancelled)
                {
                    return secret;
                }

                int value;
                if (int.TryParse(entered.Trim(), out value))
                {
                    foreach (MenuItem item in items)
                    {
                        if (item.Value == value)
                        {
                            return value;
                        }
                    }
                }
                Line("  That is not one of the choices.", ConsoleColor.Yellow);
            }
        }

        private int Clamp(int index)
        {
            if (index < 0)
            {
                return 0;
            }
            if (index >= items.Count)
            {
                return Math.Max(0, items.Count - 1);
            }
            return index;
        }
    }
}

