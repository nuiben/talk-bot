using System;

namespace talk
{
    // The story is fetched from GitHub rather than read off disk so the bot
    // recites the same copy the test suite checks.
    internal static class PengyStory
    {
        public const string Title = "Pengy and his Fish Sticks";

        public const string PageUrl =
            "https://github.com/nuiben/talk-bot/blob/main/pengy.md";

        public static string Fetch()
        {
            return WebPage.Read(PageUrl);
        }
    }
}
