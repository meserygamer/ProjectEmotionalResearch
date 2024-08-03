using OpenQA.Selenium.Chrome;
using SeleniumStealth.NET.Clients;
using SeleniumStealth.NET.Clients.Enums;
using SeleniumStealth.NET.Clients.Extensions;
using SeleniumStealth.NET.Clients.Models;

namespace KinopoiskFilmReviewsParser;

public class ChromeStealthDriverFactory
{
    private ChromeOptions _driverOptions;

    private ChromeDriver _chromeDriver;


    public ChromeDriver CreateDriver()
    {
        ConfigureDriverOptions();
        ConfigureDriver();
        return _chromeDriver;
    }
    
    private void ConfigureDriverOptions()
    {
        _driverOptions = new ChromeOptions()
            .ApplyStealth(
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
    }

    private void ConfigureDriver()
    {
        _chromeDriver = Stealth.Instantiate(_driverOptions, new StealthInstanceSettings
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
        
        _chromeDriver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument"
            , new Dictionary<string, object>()
            {
                {"source", "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;"}
            });
    }
}