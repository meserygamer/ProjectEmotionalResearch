using OpenQA.Selenium.Edge;

namespace KinopoiskFilmReviewsParser.BrowserDrivers;

public class KinopoiskReviewsParsingEdgeDriverFactory
{
    private EdgeOptions _driverOptions;

    private EdgeDriver _edgeDriver;


    public EdgeDriver CreateDriver()
    {
        ConfigureDriverOptions();
        ConfigureDriver();
        return _edgeDriver;
    }
    
    private void ConfigureDriverOptions()
    {
        _driverOptions = new EdgeOptions();
        _driverOptions.AddArguments("--disable-blink-features=AutomationControlled"
            , "headless"
            , "disable-gpu");
    }

    private void ConfigureDriver()
    {
        _edgeDriver = new EdgeDriver(_driverOptions);
        
        _edgeDriver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument"
            , new Dictionary<string, object>()
            {
                {"source", "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;"}
            });
    }
}