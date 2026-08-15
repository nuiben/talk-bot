using System;

namespace talk.Tests
{
    // The reader has to cope with more than GitHub's rendered markdown, so this
    // checks the other two shapes a user is likely to paste in: a raw file the
    // server sends as plain text, and an ordinary HTML page.
    internal class WebPageTest : ITest
    {
        private const string PlainTextUrl =
            "https://raw.githubusercontent.com/nuiben/talk-bot/main/pengy.md";

        private const string HtmlUrl = "https://example.com";

        public string Name
        {
            get { return "web page reader"; }
        }

        public bool Run()
        {
            // A raw .md is text/plain, so Firefox wraps it in a <pre> and there
            // is no article element to find.
            string plain = TestSuite.Flatten(WebPage.Read(PlainTextUrl));
            if (!plain.Contains(PengyStory.Title))
            {
                TestSuite.Report("FAIL",
                    "plain text page did not return the story", ConsoleColor.Red);
                return false;
            }

            string html = TestSuite.Flatten(WebPage.Read(HtmlUrl));
            if (!html.Contains("Example Domain"))
            {
                TestSuite.Report("FAIL",
                    "HTML page did not return its prose", ConsoleColor.Red);
                return false;
            }

            TestSuite.Report("PASS",
                "read plain text and HTML pages as well as rendered markdown",
                ConsoleColor.Green);
            return true;
        }
    }
}
