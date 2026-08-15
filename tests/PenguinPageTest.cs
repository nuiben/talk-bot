using System;

namespace talk.Tests
{
    // Checks that the browser can reach github.com and read a rendered page at
    // all. Everything else in the suite depends on that working.
    internal class PenguinPageTest : ITest
    {
        private const string ExpectedText = "penguin";

        private const string PageUrl =
            "https://github.com/nuiben/talk-bot/blob/main/penguin.md";

        public string Name
        {
            get { return "penguin page"; }
        }

        public bool Run()
        {
            string text;
            using (GitHubPage page = new GitHubPage())
            {
                page.Open(PageUrl);
                text = page.ReadText();
            }

            if (text.Contains(ExpectedText))
            {
                TestSuite.Report("PASS", "penguin.md contains \"" + ExpectedText + "\"",
                    ConsoleColor.Green);
                return true;
            }

            TestSuite.Report("FAIL", "penguin.md rendered without \"" + ExpectedText + "\"",
                ConsoleColor.Red);
            return false;
        }
    }
}
