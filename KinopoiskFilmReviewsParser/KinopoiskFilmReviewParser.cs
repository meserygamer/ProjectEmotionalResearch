using System.Net;
using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;
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
    private const string KINOPOISK_REVIEWS_PAGE_POSTFIX = "reviews/ord/date/status/all/perpage/200/page/";
    private const string KINOPOISK_FILM_REVIEW_CLASS_NAME = "userReview";

    private const string KINOPOISK_COOKIES = "yashr=9996311731714779545; yuidss=2683313871714518733; _ym_uid=1714779546133347217; yandex_login=meserygamer@gmail.com; yandexuid=2683313871714518733; L=UjFkXXxgQHZ+cXNTQVVecU5BVlxzQHBdXwAHCwgTXxIeMBonJlo4A11vFxw8.1719859244.15790.338191.85435f1775d612d46c5a084f12412e9c; gdpr=0; _csrf=vgxe1hzq1bXOK7CIwSqnwDfj; disable_server_sso_redirect=1; ya_sess_id=3:1722432996.5.0.1719859244185:thvjBQ:23.1.2:1|1997835441.0.2.3:1719859244|30:10227272.536633.MmWo85VzC-RqOCX_2tamel6I1Dg; sessar=1.1192.CiAwXRJ0i1p5pXq1fV1Z3EWhARRsvUtDS4oG-1SYMFKWbg.AE3lDw3st-yVdrmV6LXvbI-5EnyQdGjUqhssAnrhMtg; ys=udn.cDptZXNlcnlnYW1lckBnbWFpbC5jb20%3D#c_chck.3181503154; i=Zs8/NZpnHWLv37vHFHR8CqVWoWt+vz26HvFJkx7OC7cda5PEnFDl96CXunFaCO10yytq0JpnrBwWI0aqgg8ODTwcO6k=; mda2_beacon=1722432996390; sso_status=sso.passport.yandex.ru:synchronized; no-re-reg-required=1; _ym_isad=2; yp=1722519398.yu.2683313871714518733; ymex=1725024998.oyu.2683313871714518733; desktop_session_key=87ce490372b3ae275d1b14ec84491d78ac6d7888d83523734da69a60062bb1a114ada272b88835cb3345d791c63798f7a5e030c3987f5a520bc98316c97b642119d13f45501233b7dbaac581df21d8a9f62bb72a72c22747a7101b657353acf2; desktop_session_key.sig=65ZVJdi7zQzkz4nCajMC2XEz0tg; PHPSESSID=1042beaa8a17c3013a72faf720e4af8b; uid=177428860; _csrf_csrf_token=RpiA7Ev70ldUy1Qi6Crz-r8p0UWIjBEKmiMsECagrqs; mobile=no; mda_exp_enabled=1; _ym_visorc=b; _yasc=zZwZo3z7Jp30d2W128uufBapVhUFnDER6X5PyZxvWsVgYiStkDa0zB46KvLPnwBW; yandex_plus_metrika_cookie=true; _ym_d=1722435387";

    private const int NUMBER_OF_REVIEWS_PER_PAGE = 200;
    

    public List<string> LinksOnParsedFilms { get; }
    
    
    public async Task<ICollection<Film?>> GetAllReviewsAsync()
    {
        List<Film?> films = new List<Film?>();
        if (LinksOnParsedFilms.Count == 0)
            return films;
        
        await SetCookieOnKinopoiskAsync(KINOPOISK_COOKIES);

        foreach (var urlOnFilm in LinksOnParsedFilms)
            films.Add(await GetFilmDataAsync(urlOnFilm));
        
        _webDriver.Quit();
        return films;
    }

    private async Task SetCookieOnKinopoiskAsync(string cookies)
    {
        if(String.IsNullOrEmpty(cookies))
            return;
        
        string originalPageUrl = _webDriver.Url;
        await _webDriver.Navigate().GoToUrlAsync(KINOPOISK_ORIGINAL_PAGE_URL);
        
        for (int i = 0; i < cookies.Split("; ").Length; i++)
        {
            string cookie = cookies.Split("; ")[i].Trim();
            _webDriver.Manage().Cookies.AddCookie(
                new Cookie
                (cookie.Split('=')[0]
                    , cookie.Split('=')[1]));
        }
        
        await _webDriver.Navigate().GoToUrlAsync(originalPageUrl);
    }

    private async Task LoadFilmReviewsPageAsync(string filmUrl, int numberOfPage = 1)
    {
        await _webDriver.Navigate().GoToUrlAsync(filmUrl + KINOPOISK_REVIEWS_PAGE_POSTFIX + $"{numberOfPage}/");
        WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
        await Task.Run(() => wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview"))));
        _webDriver.SpecialWait(new Random().Next(2000, 4000));
    }

    private ICollection<IWebElement> GetAllReviewWebElements(bool isVerifyingPage = false)
    {
        string currentUrl = _webDriver.Url;
        if (!isVerifyingPage && !IsReviewsPage(currentUrl))
            throw new WebException("Incorrect website for parsing reviews");

        return new List<IWebElement>(_webDriver.FindElements(By.ClassName(KINOPOISK_FILM_REVIEW_CLASS_NAME)));
    }

    private IWebElement GetReviewTitleElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_title_" + reviewId));
    private IWebElement GetReviewTextElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("ext_text_" + reviewId));
    private IWebElement GetReviewOpinionElement(IWebElement reviewWebElement, string reviewId)
        => reviewWebElement.FindElement(By.Id("div_review_" + reviewId));

    private bool IsReviewsPage(string url)
        => url.Contains(KINOPOISK_ORIGINAL_PAGE_URL) && url.Contains(KINOPOISK_REVIEWS_PAGE_POSTFIX);
    
    private async Task<List<Review>> GetReviewsByFilmAsync(string urlOnFilm, bool isAlreadyOnFilmPage = false)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            return new List<Review>();
        
        if(!isAlreadyOnFilmPage)
            await LoadFilmReviewsPageAsync(urlOnFilm);
        
        int numberOfFilmReviews = GetNumberOfReviewsForFilm(true);
        int numberOfReviewsPage = (int)Math.Ceiling((decimal)numberOfFilmReviews / (decimal)NUMBER_OF_REVIEWS_PER_PAGE);

        List<Review> reviewsOnFilm = new List<Review>();
        for (int i = 1; i <= numberOfReviewsPage; i++)
        {
            await LoadFilmReviewsPageAsync(urlOnFilm, i);
            reviewsOnFilm.AddRange(GetReviewsByPage());
        }

        return reviewsOnFilm;
    }

    private async Task<Film?> GetFilmDataAsync(string urlOnFilm, bool isAlreadyOnFilmPage = false)
    {
        if (String.IsNullOrEmpty(urlOnFilm))
            return null;
        
        if(!isAlreadyOnFilmPage)
            await LoadFilmReviewsPageAsync(urlOnFilm);

        Film filmData = new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement filmTitleElement = _webDriver.FindElement(By.ClassName("breadcrumbs__link"));
            IWebElement filmYearOfReleaseElement = _webDriver.FindElement(By.ClassName("breadcrumbs__sub"));
            string[] yearOfReleaseAndOriginalName = filmYearOfReleaseElement.Text.Split(" ");
            return new Film()
            {
                FilmTitle = filmTitleElement.Text
                ,YearOfRelease = Convert.ToInt32(yearOfReleaseAndOriginalName[^1])
                ,FilmUrl = urlOnFilm
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
        string currentUrl = _webDriver.Url;
        if (!isVerifyingPage && !IsReviewsPage(currentUrl))
            throw new WebException($"Incorrect website for parsing reviews - {currentUrl}");

        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            IWebElement reviewsCounter = 
                _webDriver.FindElement(By.ClassName("pagesFromTo"));
            string reviewsCounterText = reviewsCounter.Text.Split(' ')[2];
            return Convert.ToInt32(reviewsCounterText);
        });
    }
}