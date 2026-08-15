using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenQA.Selenium;

namespace talk.Tests
{
    // Runs every test in the folder. The menu no longer offers this, so it is
    // reached with "talk-bot --test" and reports through the exit code: 0 when
    // everything passed, 1 when anything failed or could not run.
    internal static class TestSuite
    {
        public static int Run()
        {
            return Run(true);
        }

        // The input tests need neither a browser nor the network, so they run
        // first and can be run on their own: a mistake in what the program does
        // with what it is typed then shows up in a second rather than after a
        // minute of page fetching.
        public static int Run(bool includePageTests)
        {
            List<ITest> tests = new List<ITest>();
            tests.Add(new VoiceSettingsTest());
            tests.Add(new SpeechArgumentTest());
            tests.Add(new PhraseLibraryTest());
            tests.Add(new UrlInputTest());
            tests.Add(new ConsoleInputTest());
            tests.Add(new TextDisplayTest());
            tests.Add(new SecretWordTest());
            tests.Add(new MenuWindowTest());
            tests.Add(new QuietTest());
            if (includePageTests)
            {
                tests.Add(new PenguinPageTest());
                tests.Add(new PengyStoryTest());
                tests.Add(new WebPageTest());
            }

            bool allPassed = true;
            foreach (ITest test in tests)
            {
                // A browser or network problem should fail the one test that
                // hit it and let the rest of the suite carry on.
                try
                {
                    allPassed = test.Run() && allPassed;
                }
                catch (WebDriverException e)
                {
                    Report("ERROR", test.Name + " could not run: " + e.Message,
                        ConsoleColor.Red);
                    allPassed = false;
                }
                catch (PageNotReadableException e)
                {
                    Report("ERROR", test.Name + " could not run: " + e.Message,
                        ConsoleColor.Red);
                    allPassed = false;
                }
            }
            return allPassed ? 0 : 1;
        }

        // Firefox still writes a couple of its own lines to this console on
        // startup and shutdown, so each result gets a banner, color and blank
        // lines around it to stay readable next to them.
        public static void Report(string outcome, string detail, ConsoleColor color)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine();
            Console.WriteLine("#============== TEST ==============#");
            Console.WriteLine("   " + outcome + ": " + detail);
            Console.WriteLine("#==================================#");
            Console.WriteLine();
            Console.ForegroundColor = previous;
        }

        // GitHub joins the soft line breaks inside a paragraph, so the page
        // text never wraps where the markdown source does. Flattening both
        // sides lets a test look for a sentence without knowing which words
        // happened to share a line in the file.
        public static string Flatten(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }

    // Collects what went wrong instead of stopping at the first thing, so one
    // run says every case that failed rather than only the earliest. A test
    // ends by handing this to Report, which is where the pass or fail is
    // decided.
    internal class Checks
    {
        private readonly List<string> failures = new List<string>();

        public void Equal(string what, object expected, object actual)
        {
            string wanted = expected == null ? "null" : expected.ToString();
            string got = actual == null ? "null" : actual.ToString();
            if (wanted != got)
            {
                failures.Add(what + ": wanted \"" + wanted + "\", got \"" + got + "\"");
            }
        }

        public void True(string what, bool condition)
        {
            if (!condition)
            {
                failures.Add(what);
            }
        }

        // Used for the inputs that should be turned away rather than acted on.
        // The message is checked too, because a refusal the user cannot read is
        // barely better than a crash.
        public void Refuses(string what, string expectedInMessage, Action action)
        {
            try
            {
                action();
                failures.Add(what + ": nothing was refused");
            }
            catch (PageNotReadableException e)
            {
                if (e.Message.IndexOf(expectedInMessage, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add(what + ": refused with \"" + e.Message +
                        "\", which does not mention " + expectedInMessage);
                }
            }
        }

        // Nothing at all is meant to come back out of some of these, so this
        // says which call blew up rather than letting the exception end the run.
        public void Survives(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                failures.Add(what + ": threw " + e.GetType().Name + " - " + e.Message);
            }
        }

        public bool Passed
        {
            get { return failures.Count == 0; }
        }

        public bool Report(string name, string summary)
        {
            if (failures.Count == 0)
            {
                TestSuite.Report("PASS", summary, ConsoleColor.Green);
                return true;
            }

            string detail = name;
            foreach (string failure in failures)
            {
                detail = detail + "\n   - " + failure;
            }
            TestSuite.Report("FAIL", detail, ConsoleColor.Red);
            return false;
        }
    }

    internal interface ITest
    {
        string Name { get; }

        // True when the test passed. Reporting is the test's own job so it can
        // say which check failed.
        bool Run();
    }
}
