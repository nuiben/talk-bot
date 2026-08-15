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
            List<ITest> tests = new List<ITest>();
            tests.Add(new PenguinPageTest());
            tests.Add(new PengyStoryTest());
            tests.Add(new WebPageTest());

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

    internal interface ITest
    {
        string Name { get; }

        // True when the test passed. Reporting is the test's own job so it can
        // say which check failed.
        bool Run();
    }
}
