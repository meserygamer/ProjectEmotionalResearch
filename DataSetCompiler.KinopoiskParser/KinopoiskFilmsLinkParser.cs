using System.Collections.ObjectModel;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;

namespace KinopoiskFilmReviewsParser;

public class KinopoiskFilmsLinkParser : ILinkParser
{
    #region Constants

    public const string FilmsTop500KinopoiskUrl = "https://www.kinopoisk.ru/lists/movies/top500/?sort=votes";
    public const string KinopoiskOriginalPageUrl = "https://www.kinopoisk.ru/";
    
    private const int NumberFilmLinksOnPage = 50;

    #endregion
    
    
    #region Fields

    private readonly IWebDriver _webDriver;

    #endregion


    #region Constructors

    public KinopoiskFilmsLinkParser(Func<IWebDriver> webDriverFactory, KinopoiskSettings settings)
    {
        if (webDriverFactory is null)
            throw new ArgumentNullException(nameof(webDriverFactory));
        
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));
        
        _webDriver = webDriverFactory.Invoke();
        Settings = settings;
    }

    #endregion


    #region ILinkParser

    public async Task<List<string>> GetLinksAsync(int maxLinksCount)
    {
        int numberOfPages = CalculateNumberOfPagesToParse(maxLinksCount);
        List<string> filmLinks = new();

        await SetCookieOnKinopoiskAsync();
        for (int i = 1; i <= numberOfPages; i++)
            filmLinks.AddRange(await GetFilmLinksFromPageAsync(i));
        return filmLinks;
    }

    #endregion


    #region Properties

    public KinopoiskSettings Settings { get; }

    #endregion


    #region Methods
    
    private async Task SetCookieOnKinopoiskAsync()
    {
        string originalPageUrl = _webDriver.Url;
        await _webDriver.Navigate().GoToUrlAsync(KinopoiskOriginalPageUrl);
        
        for (int i = 0; i < Settings.Cookies.Split("; ").Length; i++)
        {
            string cookie = Settings.Cookies.Split("; ")[i].Trim();
            _webDriver.Manage().Cookies.AddCookie(
                new Cookie
                (cookie.Split('=')[0]
                    , cookie.Split('=')[1]));
        }
        
        await _webDriver.Navigate().GoToUrlAsync(originalPageUrl);
    }

    private async Task<List<string>> GetFilmLinksFromPageAsync(int pageNumber)
    {
        List<string> filmLinks = new();
        
        await GoToFilmsTopPageAsync(pageNumber);

        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            List<string> filmLinks = new List<string>();
            ReadOnlyCollection<IWebElement> filmsLinksElements = _webDriver.FindElements(By.ClassName("styles_root__wgbNq"));
            
            foreach (var filmLinkElement in filmsLinksElements)
                filmLinks.Add(filmLinkElement.GetAttribute("href"));
            return filmLinks;
        });
    }

    private async Task GoToFilmsTopPageAsync(int pageNumber)
    {
        await _webDriver.Navigate().GoToUrlAsync($"{FilmsTop500KinopoiskUrl}&page={pageNumber}");
        WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
        wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("styles_root__ti07r")));
        _webDriver.SpecialWait(new Random().Next(2000, 3000));
    }
    
    private int CalculateNumberOfPagesToParse(int maxLinksCount)
    {
        if (maxLinksCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLinksCount));

        maxLinksCount = Math.Min(500, maxLinksCount);
        return (int)Math.Ceiling((decimal)maxLinksCount / NumberFilmLinksOnPage);
    }

    #endregion
    
}