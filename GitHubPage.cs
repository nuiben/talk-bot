using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace talk
{
    // Reads the rendered text of a markdown file on github.com. The page body
    // is drawn by script, so FindElement needs to poll rather than look once.
    // Selenium.Support is not referenced, so this uses the implicit wait
    // instead of WebDriverWait.
    internal class GitHubPage : IDisposable
    {
        private IWebDriver _driver;

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
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            _driver.Navigate().GoToUrl(url);
        }

        public string ReadText()
        {
            IWebElement body = _driver.FindElement(By.CssSelector("article.markdown-body"));
            return body.Text;
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
}
