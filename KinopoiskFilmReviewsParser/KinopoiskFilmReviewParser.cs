using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;

namespace KinopoiskFilmReviewsParser;

public class KinopoiskFilmReviewParser : IReviewsParser
{
    public KinopoiskFilmReviewParser(IWebDriver webDriver, string filmPageUrl)
    {
        _webDriver = webDriver;
        LinksOnParsedFilms = new List<string>() { filmPageUrl };
    }


    private readonly IWebDriver _webDriver;


    public List<string> LinksOnParsedFilms { get; }
    
    
    public ICollection<Review> GetAllReviews()
    {
        throw new NotImplementedException();
    }
}