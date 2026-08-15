using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace talk
{
    // Reads the visible text of any page the user points at, not just a
    // markdown file on github.com.
    //
    // Two things vary between pages and both have to be handled here. The
    // content type decides whether there is readable text at all: a PDF or an
    // image is a browser window with nothing a synthesizer can say. The markup
    // decides where the text lives: GitHub renders markdown into
    // article.markdown-body, a plain .md or .txt file arrives as text/plain and
    // Firefox wraps it in a <pre>, and an ordinary site keeps its prose in
    // <article> or <main> with menus and banners around it.
    internal class WebPage : IDisposable
    {
        // Longest the reader waits for a page that draws its body with script.
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

        // Tried in order, first one with text wins. The GitHub selector comes
        // first so a rendered markdown page never falls through to the whole
        // body, and body is last so an unremarkable page still reads.
        private const string ExtractScript = @"
            var noise = document.querySelectorAll(
                'script, style, noscript, nav, header, footer, aside, svg, form');
            for (var i = 0; i < noise.length; i++) {
                noise[i].remove();
            }
            var selectors = ['article.markdown-body', 'article', 'main',
                             '[role=main]', 'body > pre', 'body'];
            for (var j = 0; j < selectors.length; j++) {
                var el = document.querySelector(selectors[j]);
                if (el && el.innerText && el.innerText.trim().length > 0) {
                    return el.innerText;
                }
            }
            return '';";

        private IWebDriver _driver;

        // One page, one browser: open it, take the text, close it again.
        //
        // The address is checked before the browser is quieted, so a refusal
        // is still written to a console that is listening. Everything after
        // that belongs to Firefox, which is why the two are nested this way
        // round: the browser is shut down, noisily, before the console comes
        // back.
        public static string Read(string url)
        {
            string address = Validate(url);
            using (Quiet quiet = new Quiet())
            {
                using (WebPage page = new WebPage())
                {
                    page.Open(address);
                    return page.ReadText();
                }
            }
        }

        // People type "example.com". Firefox would search for that rather than
        // visit it, so a missing scheme is filled in.
        // A colon is a scheme in "https://x" and a port in "localhost:8080",
        // and the two are told apart by what follows it: a port is digits, a
        // scheme is not. Without that, a host and port was read as a scheme of
        // its own and turned away as something other than a web address.
        private const string SchemePattern = @"^[a-zA-Z][a-zA-Z0-9+.-]*:(?![0-9])";

        public static string Normalize(string url)
        {
            string trimmed = url == null ? "" : url.Trim();
            if (trimmed.Length > 0 && !Regex.IsMatch(trimmed, SchemePattern))
            {
                return "https://" + trimmed;
            }
            return trimmed;
        }

        // Checked before a browser is started, because starting one takes the
        // best part of a minute and the answer to "" or "javascript:alert(1)"
        // is known without it. Anything other than a web address is refused
        // rather than opened: a file: or data: URL is not a page the user has
        // pointed the bot at so much as one they have handed it, and
        // javascript: would run in the browser rather than being read.
        public static string Validate(string url)
        {
            string normalized = Normalize(url);
            if (normalized.Length == 0)
            {
                throw new PageNotReadableException("no address was given");
            }

            Uri parsed;
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out parsed))
            {
                throw new PageNotReadableException(
                    "\"" + normalized + "\" is not an address a browser can open");
            }

            if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            {
                throw new PageNotReadableException(
                    parsed.Scheme + ": addresses are not read, only http and https");
            }

            // A host with no dot in it is a typed mistake far more often than
            // it is a machine on the network - a stray keystroke at the prompt
            // becomes https://7 - so it is turned back here rather than being
            // handed to a browser that will take a minute to fail. A name that
            // really has no dot can still be reached by its own address.
            //
            // The host is taken from what was typed rather than from the parsed
            // address, because a host of digits is a number to a parser: it
            // reads "7" as the address 0.0.0.7, dots and all, and the check
            // would pass the very thing it is here to catch.
            string host = Host(normalized);
            if (host.IndexOf('.') < 0 && host != "localhost"
                && !host.StartsWith("[", StringComparison.Ordinal))
            {
                throw new PageNotReadableException(
                    "\"" + host + "\" does not look like a web address");
            }
            return normalized;
        }

        // The host as it was typed: what is left of the address once the
        // scheme, any sign-in, the port and the path have been taken off.
        private static string Host(string url)
        {
            string rest = url;
            int scheme = rest.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0)
            {
                rest = rest.Substring(scheme + 3);
            }

            int end = rest.IndexOfAny(new char[] { '/', '?', '#' });
            if (end >= 0)
            {
                rest = rest.Substring(0, end);
            }

            int signIn = rest.LastIndexOf('@');
            if (signIn >= 0)
            {
                rest = rest.Substring(signIn + 1);
            }

            // An IPv6 address is written in brackets and is full of colons, so
            // the port is only what follows the closing bracket.
            if (rest.StartsWith("[", StringComparison.Ordinal))
            {
                int close = rest.IndexOf(']');
                return close < 0 ? rest : rest.Substring(0, close + 1);
            }

            int port = rest.LastIndexOf(':');
            if (port >= 0)
            {
                rest = rest.Substring(0, port);
            }
            return rest.ToLowerInvariant();
        }

        public void Open(string url)
        {
            // geckodriver and Firefox both log to the same console this app
            // draws its menu on, so both are quieted before the browser starts.
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.LogLevel = FirefoxDriverLogLevel.Fatal;
            service.SuppressInitialDiagnosticInformation = true;

            // geckodriver's throwaway profile enables both of these, and they
            // are what copy Firefox's own logging to the terminal: content
            // covers console.* from the page, chrome covers everything the
            // browser itself reports, including JavaScript error and warning
            // lines from the page's scripts.
            FirefoxOptions options = new FirefoxOptions();
            options.SetPreference("devtools.console.stdout.content", false);
            options.SetPreference("devtools.console.stdout.chrome", false);

            _driver = new FirefoxDriver(service, options);
            _driver.Navigate().GoToUrl(url);
        }

        // The content type as the server sent it, e.g. "text/html" or
        // "application/pdf".
        public string ContentType()
        {
            object type = Script("return document.contentType || '';");
            return type == null ? "" : type.ToString();
        }

        // Throws PageNotReadableException when the page holds something no
        // synthesizer can say, so the caller can put that on screen instead of
        // reading a PDF's empty viewer out loud.
        public string ReadText()
        {
            string contentType = ContentType();
            if (!IsReadable(contentType))
            {
                throw new PageNotReadableException(
                    "that page is " + contentType + ", which has no text to read");
            }

            // A body drawn by script is empty for the first moment, so the
            // extraction is retried rather than run once. Selenium.Support is
            // not referenced, so this polls instead of using WebDriverWait.
            DateTime deadline = DateTime.UtcNow + Timeout;
            string text = "";
            while (true)
            {
                object result = Script(ExtractScript);
                text = result == null ? "" : result.ToString();
                if (text.Trim().Length > 0 || DateTime.UtcNow >= deadline)
                {
                    break;
                }
                Thread.Sleep(PollInterval);
            }

            if (text.Trim().Length == 0)
            {
                throw new PageNotReadableException("that page has no readable text");
            }
            return Tidy(text);
        }

        // Anything the browser shows as text can be spoken. PDFs, images and
        // downloads cannot, and they are the common mistake: a link to a paper
        // usually points at the PDF.
        private static bool IsReadable(string contentType)
        {
            string type = contentType.ToLowerInvariant();
            return type.StartsWith("text/")
                || type.Contains("html")
                || type.Contains("xml")
                || type.Contains("json");
        }

        // innerText comes back the way the page looks, not the way it sounds.
        // Blank runs and indentation become long pauses or, with some backends,
        // nothing at all; a URL is read character by character; and a page can
        // carry lines that are not words at all, like the ascii art penguin in
        // penguin.md or a row of table borders. All three are cleaned up here
        // so what gets saved is what the synthesizer can actually say.
        private static string Tidy(string text)
        {
            text = text.Replace("\r\n", "\n");
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @" *\n *", "\n");
            text = Regex.Replace(text, @"https?://\S+", "a link");

            StringBuilder kept = new StringBuilder();
            foreach (string line in text.Split('\n'))
            {
                if (IsSpeakable(line))
                {
                    kept.Append(line);
                    kept.Append('\n');
                }
            }

            return Regex.Replace(kept.ToString(), @"\n{2,}", "\n\n").Trim();
        }

        // A line earns its place by being mostly letters and digits. Blank
        // lines are kept so paragraphs still break, and very short lines are
        // kept because they are usually headings or list markers rather than
        // decoration.
        private static bool IsSpeakable(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.Length <= 2)
            {
                return trimmed.Length == 0;
            }

            int words = 0;
            foreach (char c in trimmed)
            {
                if (char.IsLetterOrDigit(c))
                {
                    words++;
                }
            }
            return words * 100 >= trimmed.Length * 40;
        }

        private object Script(string script)
        {
            return ((IJavaScriptExecutor)_driver).ExecuteScript(script);
        }

        // Safe to call when the browser was never started or is already
        // stopped, so it can run from a finally block or a using.
        public void Dispose()
        {
            if (_driver == null)
            {
                return;
            }
            _driver.Quit();
            _driver = null;
        }
    }

    // The page loaded, but there is nothing on it worth speaking.
    internal class PageNotReadableException : Exception
    {
        public PageNotReadableException(string message) : base(message)
        {
        }
    }
}
