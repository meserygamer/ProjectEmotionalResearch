using System.Net;
using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Cookie = OpenQA.Selenium.Cookie;

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
    
    private const string KINOPOISK_ORIGINAL_PAGE_URL = "https://www.kinopoisk.ru/";
    private const string KINOPOISK_REVIEWS_PAGE_POSTFIX = "reviews/ord/date/status/all/perpage/200";
    private const string KINOPOISK_FILM_REVIEW_CLASS_NAME = "userReview";
    private const string KINOPOISK_FILM_REVIEW_TITLE_CLASS_NAME = "sub_title";
    private const string KINOPOISK_FILM_REVIEW_TEXT_XPATH = ".//div[2]/table/tbody/tr/td/div/p/span";
    private const string KINOPOISK_FILM_REVIEW_OPINION_XPATH = ".//div[2]";


    public List<string> LinksOnParsedFilms { get; }
    
    
    public async Task<ICollection<Review>> GetAllReviewsAsync()
    {
        List<Review> reviews = new List<Review>();
        foreach (var UrlOnFilm in LinksOnParsedFilms)
        {
            await _webDriver.Navigate().GoToUrlAsync(UrlOnFilm + KINOPOISK_REVIEWS_PAGE_POSTFIX);
            WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
            await Task.Run(() => wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview"))));
            reviews.AddRange(GetReviewsByFilms());
        }
        return reviews;
    }

    private async void SetCookieOnKinopoiskAsync(string cookies)
    {
        if(String.IsNullOrEmpty(cookies))
            return;
        
        string originalPageUrl = _webDriver.Url;
        await GoToUrlAsync(KINOPOISK_ORIGINAL_PAGE_URL);
        
        for (int i = 0; i < cookies.Split("; ").Length; i++)
        {
            string cookie = cookies.Split("; ")[i].Trim();
            _webDriver.Manage().Cookies.AddCookie(
                new Cookie
                (cookie.Split('=')[0]
                    , cookie.Split('=')[1]));
        }

        await GoToUrlAsync(originalPageUrl);
    }

    private async Task GoToUrlAsync(string url) 
        => await Task.Run(() => _webDriver.Url = url);

    private ICollection<IWebElement> GetAllReviewWebElements(bool isVerifyingPage = false)
    {
        string currentUrl = _webDriver.Url;
        if (!isVerifyingPage && !IsReviewsPage(currentUrl))
            throw new WebException("Incorrect website for parsing reviews");

        return new List<IWebElement>(_webDriver.FindElements(By.ClassName(KINOPOISK_FILM_REVIEW_CLASS_NAME)));
    }

    private IWebElement GetReviewTitleElement(IWebElement reviewWebElement)
        => reviewWebElement.FindElement(By.ClassName(KINOPOISK_FILM_REVIEW_TITLE_CLASS_NAME));

    private IWebElement GetReviewTextTitleElement(IWebElement reviewWebElement)
        => reviewWebElement.FindElement(By.XPath(KINOPOISK_FILM_REVIEW_TEXT_XPATH));

    private IWebElement GetReviewOpinionElement(IWebElement reviewWebElement)
        => reviewWebElement.FindElement(By.XPath(KINOPOISK_FILM_REVIEW_OPINION_XPATH));

    private bool IsReviewsPage(string url)
        => url.Contains(KINOPOISK_ORIGINAL_PAGE_URL) && url.Contains(KINOPOISK_REVIEWS_PAGE_POSTFIX);
    
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