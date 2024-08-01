using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;
using KinopoiskFilmReviewsParser;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DataSetCompiler.API;

public static class Startup
{
    public static async Task Main()
    {
        /*string cookies = "yashr=9996311731714779545; yuidss=2683313871714518733; _ym_uid=1714779546133347217; yandex_login=meserygamer@gmail.com; yandexuid=2683313871714518733; L=UjFkXXxgQHZ+cXNTQVVecU5BVlxzQHBdXwAHCwgTXxIeMBonJlo4A11vFxw8.1719859244.15790.338191.85435f1775d612d46c5a084f12412e9c; gdpr=0; _csrf=vgxe1hzq1bXOK7CIwSqnwDfj; disable_server_sso_redirect=1; ya_sess_id=3:1722432996.5.0.1719859244185:thvjBQ:23.1.2:1|1997835441.0.2.3:1719859244|30:10227272.536633.MmWo85VzC-RqOCX_2tamel6I1Dg; sessar=1.1192.CiAwXRJ0i1p5pXq1fV1Z3EWhARRsvUtDS4oG-1SYMFKWbg.AE3lDw3st-yVdrmV6LXvbI-5EnyQdGjUqhssAnrhMtg; ys=udn.cDptZXNlcnlnYW1lckBnbWFpbC5jb20%3D#c_chck.3181503154; i=Zs8/NZpnHWLv37vHFHR8CqVWoWt+vz26HvFJkx7OC7cda5PEnFDl96CXunFaCO10yytq0JpnrBwWI0aqgg8ODTwcO6k=; mda2_beacon=1722432996390; sso_status=sso.passport.yandex.ru:synchronized; no-re-reg-required=1; _ym_isad=2; yp=1722519398.yu.2683313871714518733; ymex=1725024998.oyu.2683313871714518733; desktop_session_key=87ce490372b3ae275d1b14ec84491d78ac6d7888d83523734da69a60062bb1a114ada272b88835cb3345d791c63798f7a5e030c3987f5a520bc98316c97b642119d13f45501233b7dbaac581df21d8a9f62bb72a72c22747a7101b657353acf2; desktop_session_key.sig=65ZVJdi7zQzkz4nCajMC2XEz0tg; PHPSESSID=1042beaa8a17c3013a72faf720e4af8b; uid=177428860; _csrf_csrf_token=RpiA7Ev70ldUy1Qi6Crz-r8p0UWIjBEKmiMsECagrqs; mobile=no; mda_exp_enabled=1; _ym_visorc=b; _yasc=zZwZo3z7Jp30d2W128uufBapVhUFnDER6X5PyZxvWsVgYiStkDa0zB46KvLPnwBW; yandex_plus_metrika_cookie=true; _ym_d=1722435387";

        var edgeOptions = new EdgeOptions();
        edgeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        //edgeOptions.AddArgument("headless");
        edgeOptions.AddArgument("disable-gpu");
        
        var driver = new EdgeDriver(edgeOptions);

        driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument"
            , new Dictionary<string, object>()
            {
                {"source", "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;"}
            });
        
        await driver.Navigate().GoToUrlAsync("https://www.kinopoisk.ru/");

        for (int i = 0; i < cookies.Split("; ").Length; i++)
        {
            string cookie = cookies.Split("; ")[i].Trim();
            driver.Manage().Cookies.AddCookie(
                new Cookie
                (cookie.Split('=')[0]
                , cookie.Split('=')[1]));
        }

        await driver.Navigate().GoToUrlAsync("https://www.kinopoisk.ru/film/535341/reviews/ord/date/status/all/perpage/200/page/3/");

        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(40));
        wait.Until(ExpectedConditions.ElementExists(By.XPath("//button[@aria-label='Меню профиля']")));
        List<IWebElement> reviewsWebElements = new List<IWebElement>(driver.FindElements(By.ClassName("userReview")));
        List<Review> reviews = new List<Review>();
        for(int i = 0; i < reviewsWebElements.Count; i++)
        {
            IWebElement reviewTitle = reviewsWebElements[i].FindElement(By.ClassName("sub_title"));
            IWebElement reviewText = reviewsWebElements[i].FindElement(By.XPath(".//div[2]/table/tbody/tr/td/div/p/span"));
            IWebElement reviewStatus = reviewsWebElements[i].FindElement(By.XPath(".//div[2]"));
            var review = new Review()
            {
                ReviewTitle = reviewTitle.Text,
                ReviewOpinion = reviewStatus.GetAttribute("class").Split(' ')[1],
                ReviewText = reviewText.Text
            };
            reviews.Add(review);
        }*/

        KinopoiskFilmReviewParser parser = new KinopoiskFilmReviewParser(new KinopoiskReviewsParsingEdgeDriverFactory().CreateDriver(), "https://www.kinopoisk.ru/film/535341/");
        List<Review> reviews = new List<Review>(await parser.GetAllReviewsAsync());

        string jsonData = JsonSerializer.Serialize(reviews, new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
        using (var fs = new FileStream("reviewsData.json", FileMode.Append))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        //driver.Quit();
    }
}