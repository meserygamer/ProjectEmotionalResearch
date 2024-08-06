using System.Text;
using DataSetCompiler.Core.DomainEntities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;

namespace KinopoiskFilmReviewsParser.Parsers.FilmsParser;

public class KinopoiskFilmsParserWorker
{
    private const string KinopoiskReviewsPagePostfix = "reviews/ord/date/status/all/perpage/200/page/";
    private const string KinopoiskFilmReviewClassName = "userReview";
    
    private const int NumberOfReviewsPerPage = 200;
    
    
    private readonly Func<IWebDriver> _webDriverFactory;

    private IWebDriver _currentWebDriver;
    
    
    public KinopoiskFilmsParserWorker(Func<IWebDriver> webDriverFactory,
        int? frequencyOfWebDriverChanges)
    {
        _webDriverFactory = webDriverFactory ?? throw new ArgumentNullException(nameof(webDriverFactory));
        FrequencyOfWebDriverChanges = frequencyOfWebDriverChanges;
        _currentWebDriver = _webDriverFactory.Invoke();
    }


    public int? FrequencyOfWebDriverChanges { get; set; }


    public async Task<ICollection<Film>> ParseFilmsAsync(Queue<string> filmsUrls)
    {
        if (filmsUrls is null)
            throw new ArgumentNullException(nameof(filmsUrls));

        List<Film> films = new List<Film>();
        int? filmsNumberBeforeDriverChange = FrequencyOfWebDriverChanges;

        try
        {
            while (filmsUrls.TryDequeue(out string? filmUrl))
            {
                films.Add(await GetFilmDataAsync(filmUrl));
                filmsNumberBeforeDriverChange = (filmsNumberBeforeDriverChange is null)?
                    null : filmsNumberBeforeDriverChange - 1;
                if (filmsNumberBeforeDriverChange is not null && filmsNumberBeforeDriverChange <= 0)
                {
                    ChangeWebDriver();
                    filmsNumberBeforeDriverChange = FrequencyOfWebDriverChanges;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        _currentWebDriver.Quit();
        return films;
    }
    
    private async Task<Film> GetFilmDataAsync(string urlOnFilm)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            throw new ArgumentNullException(nameof(urlOnFilm));
        
        await LoadFilmReviewsPageAsync(urlOnFilm);
        
        Film filmData = new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement filmTitleElement = _currentWebDriver.FindElement(By.ClassName("breadcrumbs__link"));
            IWebElement filmYearOfReleaseElement = _currentWebDriver.FindElement(By.ClassName("breadcrumbs__sub"));
            string[] yearOfReleaseAndOriginalName = filmYearOfReleaseElement.Text.Split(" ");
            return new Film()
            {
                FilmTitle = filmTitleElement.Text,
                YearOfRelease = Convert.ToInt32(yearOfReleaseAndOriginalName[^1]),
                FilmUrl = urlOnFilm
            };
        });
        
        filmData.Reviews = await GetReviewsByFilmAsync(urlOnFilm);
        return filmData;
    }
    
    private async Task LoadFilmReviewsPageAsync(string filmUrl, int numberOfPage = 1)
    {
        string reviewPageUrl = new StringBuilder(filmUrl).Append(KinopoiskReviewsPagePostfix)
            .Append(numberOfPage)
            .Append("/")
            .ToString();
        await _currentWebDriver.Navigate().GoToUrlAsync(reviewPageUrl);
        WebDriverWait wait = new WebDriverWait(_currentWebDriver, TimeSpan.FromSeconds(40));
        await Task.Run(() => wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview"))));
        _currentWebDriver.SpecialWait(new Random().Next(1500, 2000));
    }
    
    private async Task<List<Review>> GetReviewsByFilmAsync(string urlOnFilm)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            return new List<Review>();
        
        int numberOfFilmReviews = GetNumberOfReviewsForFilm();
        int numberOfReviewsPage = (int)Math.Ceiling((decimal)numberOfFilmReviews / (decimal)NumberOfReviewsPerPage);

        List<Review> reviewsOnFilm = new List<Review>();
        for (int i = 1; i <= numberOfReviewsPage; i++)
        {
            await LoadFilmReviewsPageAsync(urlOnFilm, i);
            reviewsOnFilm.AddRange(GetReviewsByPage());
        }

        return reviewsOnFilm;
    }
    
    private int GetNumberOfReviewsForFilm()
    {
        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement reviewsCounter = 
                _currentWebDriver.FindElement(By.ClassName("pagesFromTo"));
            string reviewsCounterText = reviewsCounter.Text.Split(' ')[2];
            return Convert.ToInt32(reviewsCounterText);
        });
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
    
    private ICollection<IWebElement> GetAllReviewWebElements(bool isVerifyingPage = false)
        => new List<IWebElement>(_currentWebDriver.FindElements(By.ClassName(KinopoiskFilmReviewClassName)));
    private IWebElement GetReviewTitleElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_title_" + reviewId));
    private IWebElement GetReviewTextElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_text_" + reviewId));
    private IWebElement GetReviewOpinionElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("div_review_" + reviewId));

    private void ChangeWebDriver()
    {
        _currentWebDriver?.Quit();
        _currentWebDriver = _webDriverFactory.Invoke();
    }
}