using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;
using Cookie = OpenQA.Selenium.Cookie;

namespace KinopoiskFilmReviewsParser;

public class KinopoiskFilmsReviewsParser : IReviewsParser
{
    #region Constants

    private const string KinopoiskOriginalPageUrl = "https://www.kinopoisk.ru/";
    private const string KinopoiskReviewsPagePostfix = "reviews/ord/date/status/all/perpage/200/page/";
    private const string KinopoiskFilmReviewClassName = "userReview";
    
    private const int NumberOfReviewsPerPage = 200;

    #endregion


    #region Fields
    
    private readonly IWebDriver _webDriver;

    #endregion
    
    
    #region Сonstructors

    public KinopoiskFilmsReviewsParser(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    #endregion


    #region IReviewsParser

    public async Task<ICollection<Film>> GetAllReviewsAsync(ICollection<string> filmUrls)
    {
        if (filmUrls is null)
            throw new ArgumentNullException(nameof(filmUrls));
            
        List<Film> films = new List<Film>();
        foreach (var filmUrl in filmUrls)
            films.Add(await GetFilmDataAsync(filmUrl));
        
        _webDriver.Quit();
        return films;
    }

    public async Task<int> PrintAllReviewsIntoFileAsync(ICollection<string> filmUrls,
        JsonSerializerOptions serializerOptions,
        string outputFile = "reviews.json")
    {
        if (String.IsNullOrEmpty(outputFile))
            throw new ArgumentException("Path to file was incorrect", nameof(outputFile));

        ICollection<Film> films = await GetAllReviewsAsync(filmUrls);
        
        string filmsJson = await Task.Run(() => JsonSerializer.Serialize(films, serializerOptions));
        using (var fs = new FileStream("LinksOnFilms.json", FileMode.Append))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(filmsJson);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return films.Select(film => film.Reviews.Count).Sum();
    }

    #endregion


    #region Properties

    private bool IsDriverOnReviewsPage 
        => _webDriver.Url.Contains(KinopoiskOriginalPageUrl) && _webDriver.Url.Contains(KinopoiskReviewsPagePostfix);

    #endregion


    #region Methods

    private async Task LoadFilmReviewsPageAsync(string filmUrl, int numberOfPage = 1)
    {
        await _webDriver.Navigate().GoToUrlAsync(filmUrl + KinopoiskReviewsPagePostfix + $"{numberOfPage}/");
        WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
        await Task.Run(() => wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview"))));
        _webDriver.SpecialWait(new Random().Next(2000, 3000));
    }

    private ICollection<IWebElement> GetAllReviewWebElements(bool isVerifyingPage = false)
    {
        if (!isVerifyingPage && !IsDriverOnReviewsPage)
            throw new WebException($"Incorrect website for parsing reviews - {_webDriver.Url}");

        return new List<IWebElement>(_webDriver.FindElements(By.ClassName(KinopoiskFilmReviewClassName)));
    }

    private IWebElement GetReviewTitleElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_title_" + reviewId));
    private IWebElement GetReviewTextElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_text_" + reviewId));
    private IWebElement GetReviewOpinionElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("div_review_" + reviewId));

    private async Task<List<Review>> GetReviewsByFilmAsync(string urlOnFilm, bool isAlreadyOnFilmPage = false)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            return new List<Review>();
        
        if(!isAlreadyOnFilmPage)
            await LoadFilmReviewsPageAsync(urlOnFilm);
        
        int numberOfFilmReviews = GetNumberOfReviewsForFilm(true);
        int numberOfReviewsPage = (int)Math.Ceiling((decimal)numberOfFilmReviews / (decimal)NumberOfReviewsPerPage);

        List<Review> reviewsOnFilm = new List<Review>();
        for (int i = 1; i <= numberOfReviewsPage; i++)
        {
            await LoadFilmReviewsPageAsync(urlOnFilm, i);
            reviewsOnFilm.AddRange(GetReviewsByPage());
        }

        return reviewsOnFilm;
    }

    private async Task<Film> GetFilmDataAsync(string urlOnFilm, bool isAlreadyOnFilmPage = false)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            throw new ArgumentNullException(nameof(urlOnFilm));
        
        if(!isAlreadyOnFilmPage)
            await LoadFilmReviewsPageAsync(urlOnFilm);

        Film filmData = new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement filmTitleElement = _webDriver.FindElement(By.ClassName("breadcrumbs__link"));
            IWebElement filmYearOfReleaseElement = _webDriver.FindElement(By.ClassName("breadcrumbs__sub"));
            string[] yearOfReleaseAndOriginalName = filmYearOfReleaseElement.Text.Split(" ");
            return new Film()
            {
                FilmTitle = filmTitleElement.Text,
                YearOfRelease = Convert.ToInt32(yearOfReleaseAndOriginalName[^1]),
                FilmUrl = urlOnFilm
            };
        });
        
        filmData.Reviews = await GetReviewsByFilmAsync(urlOnFilm, true);
        return filmData;
    }

    private List<Review> GetReviewsByPage()
    {
        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            List<IWebElement> reviewsWebElements = new List<IWebElement>(GetAllReviewWebElements());
            List<Review> reviews = new List<Review>();
            Parallel.For(0, reviewsWebElements.Count, (i, state) =>
            {
                string reviewId = reviewsWebElements[i].GetAttribute("data-id");
                IWebElement reviewOpinion = GetReviewOpinionElement(reviewsWebElements[i], reviewId);
                IWebElement reviewTitle = GetReviewTitleElement(reviewOpinion, reviewId);
                IWebElement reviewText = GetReviewTextElement(reviewOpinion, reviewId);
                var review = new Review()
                {
                    ReviewTitle = reviewTitle.Text,
                    ReviewText = reviewText.Text,
                    ReviewOpinion = reviewOpinion.GetAttribute("class").Split(' ')[1]
                };
                reviews.Add(review);
            });

            return reviews;
        });
    }
    
    private int GetNumberOfReviewsForFilm(bool isVerifyingPage = false)
    {
        if (!isVerifyingPage && !IsDriverOnReviewsPage)
            throw new WebException($"Incorrect website for parsing reviews - {_webDriver.Url}");

        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement reviewsCounter = 
                _webDriver.FindElement(By.ClassName("pagesFromTo"));
            string reviewsCounterText = reviewsCounter.Text.Split(' ')[2];
            return Convert.ToInt32(reviewsCounterText);
        });
    }

    #endregion
}