using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumStealth.NET.Clients;
using SeleniumStealth.NET.Clients.Enums;
using SeleniumStealth.NET.Clients.Extensions;
using SeleniumStealth.NET.Clients.Models;

namespace KinopoiskFilmReviewsParser;

public class ChromeStealthDriverBuilder
{
    private readonly Dictionary<string, string> _cookies = new Dictionary<string, string>();


    public ChromeStealthDriverBuilder AddCookie(string siteUrl, string cookies)
    {
        _cookies.TryAdd(siteUrl, cookies);
        return this;
    }
    
    public async Task<IWebDriver> BuildAsync()
    {
        ChromeDriver driver = ConfigureDriver(ConfigureDriverOptions());
        RemoveSeleniumTraces(driver);
        foreach (var cookie in _cookies)
            await SetCookieOnKinopoiskAsync(driver, cookie.Key, cookie.Value);
        return driver;
    }

    private ChromeOptions ConfigureDriverOptions()
        => new ChromeOptions().ApplyStealth(
                headless: true,
                settings: new ApplyStealthSettings
                {
                    DisableAutomationControlled = true,
                    DisableBrowserSideNavigation = true,
                    DisableDevShmUsage = true,
                    DisableExtensions = true,
                    DisableGpu = true,
                    DisableInfoBars = true,
                    DisableRendererBackgrounding = true,
                    DisableVizDisplayCompositor = true,
                    DisableWebSecurity = true,
                    NoSandBox = true
                });

    private ChromeDriver ConfigureDriver(ChromeOptions options) 
        => Stealth.Instantiate(options, new StealthInstanceSettings
        {
            FakeCanPlayType = true,
            FakeChromeApp = true,
            FakeChromeRuntime = new ChromeRuntime
            {
                FakeIt = true,
                RunOnInsercureOrigins = true
            },
            FakeLoadingTimes = true,
            FakePluginsAndMimeTypes = true,
            FakeWindowOuterDimensions = true,
            FixHairline = true,
            HideWebDriver = true,
            IFrameProxy = true,
            Mode = EStealthMode.SeleniumStealth,
            RandomUserAgent = true,
            RemoveCDCVariables = true
        });

    private void RemoveSeleniumTraces(ChromeDriver driver)
    {
        driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument",
            new Dictionary<string, object>()
            {
                {"source", "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;"}
            });
    }
    
    private async Task SetCookieOnKinopoiskAsync(ChromeDriver driver, string targetSiteUrl, string cookie)
    {
        string originalPageUrl = driver.Url;
        await driver.Navigate().GoToUrlAsync(targetSiteUrl);

        string[] cookieParameters = cookie.Split("; ");
        for (int i = 0; i < cookieParameters.Length; i++)
        {
            string cookieParameter = cookieParameters[i].Trim();
            driver.Manage().Cookies.AddCookie(
                new Cookie(
                    cookieParameter.Split('=')[0],
                    cookieParameter.Split('=')[1]));
        }
        
        await driver.Navigate().GoToUrlAsync(originalPageUrl);
    }
}