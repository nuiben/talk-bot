using System;

namespace talk.Tests
{
    // The library is walked by the remove and speak menus, and both of them
    // used to hand it whatever the user picked without either side checking
    // that it was still there. These are the cases a user reaches by removing
    // a phrase, backing out of a menu, or getting to the end of a piped input.
    internal class PhraseLibraryTest : ITest
    {
        public string Name
        {
            get { return "phrase library"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();
            PhraseLibrary library = new PhraseLibrary();

            // An empty library is what the program starts with, so nothing may
            // throw before the first phrase is added.
            checks.Equal("an empty library lists nothing", 0, library.ListPhrases().Length);
            checks.Equal("removing from an empty library says so", false,
                library.RemovePhrase(1));
            checks.Equal("playing from an empty library says so", false,
                library.PlayPhrase(1));

            library.AddPhrase(new Phrase(1, "first"));
            library.AddPhrase(new Phrase(2, "second"));
            checks.Equal("two phrases were kept", 2, library.ListPhrases().Length);

            // An ID that is not in the list is what the menu returns after a
            // phrase has been removed in another pass. It used to end the
            // program; now it is reported.
            checks.Equal("removing an ID that is not there says so", false,
                library.RemovePhrase(99));
            checks.Equal("playing an ID that is not there says so", false,
                library.PlayPhrase(99));
            checks.Equal("a negative ID is not there either", false,
                library.PlayPhrase(Menu.Cancelled));

            // Removing twice is one keystroke away: the menu is still open and
            // the row is still on the screen.
            checks.Equal("the first removal worked", true, library.RemovePhrase(1));
            checks.Equal("the second removal of the same ID says so", false,
                library.RemovePhrase(1));
            checks.Equal("the other phrase is still there", 1, library.ListPhrases().Length);
            checks.Equal("and it is the one that was not removed", 2,
                library.ListPhrases()[0].GetId());

            // Nothing typed used to be saved as a phrase of nothing, which the
            // list then showed as an empty row.
            library.AddPhrase(null);
            checks.Equal("a phrase that is not there is not added", 1,
                library.ListPhrases().Length);

            // Text off a page arrives with newlines in it, and the menu row is
            // one line high, so it has to be cut down rather than wrapped.
            Phrase page = new Phrase(3, "a heading\nand a paragraph that goes on and on " +
                "for far longer than a row of a menu could ever be");
            library.AddPhrase(page);
            checks.Equal("a phrase keeps the text it was given",
                page.GetPhrase(), library.ListPhrases()[1].GetPhrase());

            return checks.Report(Name, "missing IDs and empty input are reported, not acted on");
        }
    }
}
