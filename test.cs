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
    internal class Test(IWebDriver driver)
    {
        private IWebDriver _driver = driver;

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

        public void ClearMemory()
        {
            _driver.Quit();
            _driver = null;
        }
    }
}
