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
        private IWebDriver _driver;

        public void Initialize()
        {
            _driver = new FirefoxDriver();
            _driver.Navigate().GoToUrl("https://github.com/nuiben/talk-bot/blob/main/README.md");
        }

        public void ExecuteTest()
        {
            IWebElement element = _driver.FindElement(By.Name("inputDiv"));
            element.SendKeys("some keys");
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
