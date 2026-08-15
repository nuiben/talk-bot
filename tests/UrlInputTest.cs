using System;

namespace talk.Tests
{
    // Whatever is typed at "Which page should Talk Bot read?" goes to a
    // browser, and starting one takes the best part of a minute. These are the
    // answers that can be given without starting one at all: a blank line, a
    // stray keystroke, and an address that is not a web page.
    internal class UrlInputTest : ITest
    {
        public string Name
        {
            get { return "url input"; }
        }

        public bool Run()
        {
            Checks checks = new Checks();

            // What people actually type: no scheme, and spaces from a paste.
            checks.Equal("a bare host gets https", "https://example.com",
                WebPage.Validate("example.com"));
            checks.Equal("a pasted address is trimmed",
                "https://example.com/page", WebPage.Validate("  example.com/page  "));
            checks.Equal("an address that has a scheme keeps it",
                "http://example.com", WebPage.Validate("http://example.com"));
            checks.Equal("https is left alone", "https://example.com/a/b?c=d#e",
                WebPage.Validate("https://example.com/a/b?c=d#e"));
            checks.Equal("a host that is only a subdomain still works",
                "https://raw.githubusercontent.com/a/b",
                WebPage.Validate("raw.githubusercontent.com/a/b"));

            // Nothing typed, and the end of a piped input, which arrives as a
            // null rather than an empty line.
            checks.Refuses("an empty address", "no address",
                delegate { WebPage.Validate(""); });
            checks.Refuses("an address of spaces", "no address",
                delegate { WebPage.Validate("   "); });
            checks.Refuses("no address at all", "no address",
                delegate { WebPage.Validate(null); });

            // A stray keystroke at the prompt. This is not theory: a digit left
            // over from a menu was answered by a browser spending a minute
            // failing to reach https://7.
            checks.Refuses("a single digit", "does not look like",
                delegate { WebPage.Validate("7"); });
            checks.Refuses("a word", "does not look like",
                delegate { WebPage.Validate("pengy"); });

            // Addresses a browser would open but a bot should not: script that
            // would run rather than be read, and files from the machine it is
            // running on.
            checks.Refuses("a javascript address", "javascript",
                delegate { WebPage.Validate("javascript:alert(1)"); });
            checks.Refuses("a file address", "file",
                delegate { WebPage.Validate("file:///etc/passwd"); });
            checks.Refuses("a data address", "data",
                delegate { WebPage.Validate("data:text/html,hello"); });
            checks.Refuses("an about address", "about",
                delegate { WebPage.Validate("about:config"); });

            // A machine on the network is a real answer, so it is not caught by
            // the check that turns away a bare word.
            checks.Survives("localhost is allowed",
                delegate { WebPage.Validate("localhost:8080/page"); });
            checks.Survives("an address is allowed",
                delegate { WebPage.Validate("127.0.0.1:8080"); });
            checks.Survives("an IPv6 address is allowed",
                delegate { WebPage.Validate("http://[::1]:8080/page"); });
            checks.Survives("a host and port is allowed",
                delegate { WebPage.Validate("example.com:8080/page"); });

            // A host and port reads as a scheme if the colon is taken at face
            // value, which turned "localhost:8080" into a refusal.
            checks.Equal("a port is not read as a scheme",
                "https://localhost:8080/page", WebPage.Validate("localhost:8080/page"));

            return checks.Report(Name,
                "blank, mistyped and non-web addresses are answered without a browser");
        }
    }
}
