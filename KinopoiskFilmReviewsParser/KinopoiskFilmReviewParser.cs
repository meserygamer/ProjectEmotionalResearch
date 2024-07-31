using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace KinopoiskFilmReviewsParser;

public class KinopoiskFilmReviewParser : IReviewsParser
{
    #region Public constructors

    public KinopoiskFilmReviewParser(IWebDriver webDriver, string filmPageUrl)
    {
        _webDriver = webDriver;
        LinksOnParsedFilms = new List<string>() { filmPageUrl };
    }

    public KinopoiskFilmReviewParser(IWebDriver webDriver, ICollection<string> filmPageUrls)
    {
        _webDriver = webDriver;
        LinksOnParsedFilms = new List<string>(filmPageUrls);
    }

    #endregion
    

    private readonly IWebDriver _webDriver;


    public List<string> LinksOnParsedFilms { get; }
    
    
    public async Task<ICollection<Review>> GetAllReviewsAsync()
    {
        List<Review> reviews = new List<Review>();
        foreach (var UrlOnFilm in LinksOnParsedFilms)
        {
            await _webDriver.Navigate().GoToUrlAsync(UrlOnFilm + "reviews/ord/date/status/all/perpage/200/");
            WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
            await Task.Run(() => wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview"))));
            reviews.AddRange(GetReviewsByFilms());
        }
        return reviews;
    }

    private List<Review> GetReviewsByFilms()
    {
        List<IWebElement> reviewsWebElements = new List<IWebElement>(_webDriver.FindElements(By.ClassName("userReview")));
        List<Review> reviews = new List<Review>();
        for(int i = 0; i < reviewsWebElements.Count; i++)
        {
            IWebElement reviewTitle = reviewsWebElements[i].FindElement(By.ClassName("sub_title"));
            IWebElement reviewText = reviewsWebElements[i].FindElement(By.XPath(".//div[2]/table/tbody/tr/td/div/p/span"));
            var review = new Review()
            {
                ReviewTitle = reviewTitle.Text,
                ReviewText = reviewText.Text
            };
            reviews.Add(review);
        }

        return reviews;
    }
}