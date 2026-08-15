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
    // The highlighted row opens to show its detail line, accordion style, and
    // closes again when the highlight moves on, so the screen only ever
    // explains the one choice the user is looking at.
    //
    // Redrawing works by counting the lines written and winding the cursor back
    // over them, so the menu updates in place instead of scrolling a fresh copy
    // down the terminal on every keystroke.
    internal class Menu
    {
        // Returned when the user presses escape rather than choosing.
        public const int Cancelled = -1;

        private const int Width = 46;

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

            while (true)
            {
                int lines = Draw();
                ConsoleKeyInfo key = Console.ReadKey(true);
                Erase(lines);

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
                    Echo(items[selected]);
                    return items[selected].Value;
                }
                else if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q')
                {
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
                        return secret;
                    }
                }
            }
        }

        // Returns the number of lines written, which is what Erase winds back.
        private int Draw()
        {
            int lines = 0;
            Line(Rule(title), Accent);
            lines++;

            for (int i = 0; i < items.Count; i++)
            {
                MenuItem item = items[i];
                if (i == selected)
                {
                    Highlight("  > " + Fit(item.Label, Width - 4));
                    lines++;
                    if (item.Detail.Length > 0)
                    {
                        Line("      " + Fit(item.Detail, Width - 6), ConsoleColor.DarkGray);
                        lines++;
                    }
                }
                else
                {
                    Line("    " + Fit(item.Label, Width - 4), ConsoleColor.Gray);
                    lines++;
                }
            }

            Line(Rule(""), Accent);
            Line("  up/down move    enter select    esc back", ConsoleColor.DarkGray);
            return lines + 2;
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
            Console.Write(Fit(text, Width).PadRight(Width));
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

