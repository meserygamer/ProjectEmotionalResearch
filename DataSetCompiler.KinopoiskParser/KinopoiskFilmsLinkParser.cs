using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;

namespace KinopoiskFilmReviewsParser;

public class KinopoiskFilmsLinkParser : ILinkParser
{
    #region Constants

    public const string FilmsTop500KinopoiskUrl = "https://www.kinopoisk.ru/lists/movies/top500/?sort=votes";

    private const int NumberFilmLinksOnPage = 50;

    #endregion
    
    
    #region Fields

    private readonly IWebDriver _webDriver;

    #endregion


    #region Constructors

    public KinopoiskFilmsLinkParser(Func<IWebDriver> webDriverFactory)
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
        return filmLinks;
    }

    #endregion


    #region Methods

    private List<string> GetFilmLinksFromPage(int pageNumber)
    {
        List<string> filmLinks = new();
        return filmLinks;
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