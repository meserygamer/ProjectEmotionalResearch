using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace YoutubeCommentsParser;

public static class Test
{
    public static async Task Main()
    {
        var edgeOptions = new EdgeOptions();
        edgeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        edgeOptions.AddArgument("headless");
        edgeOptions.AddArgument("disable-gpu");
        
        var driver = new EdgeDriver(edgeOptions);

        driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument"
            , new Dictionary<string, object>()
            {
                {"source", "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Array;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Promise;\n" +
                           "delete window.cdc_adoQpoasnfa76pfcZLmcfl_Symbol;"}
            });
        
        driver.Url = "https://www.kinopoisk.ru/film/535341/reviews/ord/date/status/all/perpage/200/";
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(40));
        wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("userReview")));
        List<IWebElement> reviewsWebElements = new List<IWebElement>(driver.FindElements(By.ClassName("userReview")));
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

        string jsonData = JsonSerializer.Serialize(reviews, new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
        using (var fs = new FileStream("reviewsData.json", FileMode.Append))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
            fs.WriteAsync(buffer, 0, buffer.Length);
        }

        driver.Quit();
    }
}