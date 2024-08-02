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
        IWebDriver webDriver = new ChromeStealthDriverFactory().CreateDriver();
        KinopoiskFilmReviewParser parser = new KinopoiskFilmReviewParser(webDriver
            , ["https://www.kinopoisk.ru/film/535341/"
                , "https://www.kinopoisk.ru/film/462682/"
                , "https://www.kinopoisk.ru/film/397667/"]);
        List<Film?> reviews = new List<Film?>(await parser.GetAllReviewsAsync());

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
    }
}