using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;

namespace KinopoiskFilmReviewsParser.Parsers.LinksParsers;

public class KinopoiskTop500FilmsLinksParser : ILinkParser
{
    #region Constants

    public const string FilmsTop500KinopoiskUrl = "https://www.kinopoisk.ru/lists/movies/top500/?sort=votes";
    
    private const int NumberFilmLinksOnPage = 50;

    #endregion
    
    
    #region Fields

    private readonly IWebDriver _webDriver;

    #endregion


    #region Constructors

    public KinopoiskTop500FilmsLinksParser(Func<IWebDriver> webDriverFactory)
    {
        if (webDriverFactory is null)
            throw new ArgumentNullException(nameof(webDriverFactory));
        
        _webDriver = webDriverFactory.Invoke();
    }

    #endregion


    #region ILinkParser

    public async Task<List<string>> GetLinksAsync(int maxLinksCount)
    {
        int numberOfPages = CalculateNumberOfPagesToParse(maxLinksCount);
        List<string> filmLinks = new();
        
        for (int i = 1; i <= numberOfPages; i++)
            filmLinks.AddRange(await GetFilmLinksFromPageAsync(i));
        return (filmLinks.Count > maxLinksCount)? filmLinks[0..maxLinksCount] : filmLinks;
    }

    public async Task<List<string>> GetLinksWithPrintAsync(
        int maxLinksCount,
        JsonSerializerOptions serializerOptions,
        string outputFile = "LinksOnFilms.json")
    {
        if (String.IsNullOrEmpty(outputFile))
            throw new ArgumentException("Path to file was incorrect", nameof(outputFile));

        List<string> links = await GetLinksAsync(maxLinksCount);
        
        string linksJson = await Task.Run(() => JsonSerializer.Serialize(links, serializerOptions));
        using (var fs = new FileStream(outputFile, FileMode.Create))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(linksJson);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return links;
    }

    #endregion


    #region Methods

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