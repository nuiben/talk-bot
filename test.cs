using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace talk
{
    // Owns its driver: the browser starts in Initialize and stops in
    // ClearMemory, so nothing launches until the test actually runs.
    internal class Test
    {
        private const string ExpectedText = "penguin";

        private const string PageUrl =
            "https://github.com/nuiben/talk-bot/blob/main/penguin.md";

        private IWebDriver _driver;

        // The page body is rendered by script, so FindElement needs to poll
        // rather than look once. Selenium.Support is not referenced, so this
        // uses the implicit wait instead of WebDriverWait.
        public void Initialize()
        {
            // geckodriver and Firefox both log to the same console this app
            // draws its menu on, so both are quieted before the browser starts.
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.LogLevel = FirefoxDriverLogLevel.Fatal;
            service.SuppressInitialDiagnosticInformation = true;

            // geckodriver's throwaway profile enables this, and it is what
            // copies Firefox's console.error and JavaScript error lines to
            // the terminal.
            FirefoxOptions options = new FirefoxOptions();
            options.SetPreference("devtools.console.stdout.content", false);

            _driver = new FirefoxDriver(service, options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            _driver.Navigate().GoToUrl(PageUrl);
        }

        public void ExecuteTest()
        {
            IWebElement body = _driver.FindElement(By.CssSelector("article.markdown-body"));
            if (body.Text.Contains(ExpectedText))
            {
                Console.WriteLine("PASS: penguin.md contains \"" + ExpectedText + "\"");
            }
            else
            {
                Console.WriteLine("FAIL: penguin.md rendered but did not contain \""
                    + ExpectedText + "\"");
            }
        }

        // Safe to call when the browser was never started or is already
        // stopped, so it can run from a finally block.
        public void ClearMemory()
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
