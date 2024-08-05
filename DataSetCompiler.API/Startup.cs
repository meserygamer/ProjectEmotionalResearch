using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using KinopoiskFilmReviewsParser;
using OpenQA.Selenium;
using AppConfiguration = DataSetCompiler.API.AppSettings;

namespace DataSetCompiler.API;

public static class Startup
{
    public static async Task Main()
    {
        AppConfiguration.AppSettings? appSettings = await AppConfiguration.AppSettings.FromJsonFileAsync("appsettings.debug.json");
        if (appSettings is null)
            throw new JsonException("path or appsettings file was incorrect");
        
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        IWebDriver webDriver = await new ChromeStealthDriverBuilder()
            .AddCookie("https://www.kinopoisk.ru/", appSettings.KinopoiskParserSettings.Cookies)
            .BuildAsync();
        
        List<string> filmsUrls;
        if (File.Exists(appSettings.KinopoiskParserSettings.FilmsUrlsFilePath))
        {
            await using (FileStream fs = File.Open(appSettings.KinopoiskParserSettings.FilmsUrlsFilePath,
                             FileMode.Open,
                             FileAccess.Read)) 
                filmsUrls = await JsonSerializer.DeserializeAsync<List<string>>(fs) ?? [];
        }
        else
        {
            filmsUrls = await new KinopoiskFilmsLinkParser(() => webDriver)
                .GetLinksWithPrintAsync(500, jsonOptions);
        }
        
        int reviewsCount = await new KinopoiskFilmsReviewsParser(webDriver).PrintAllReviewsIntoFileAsync(
            filmsUrls,
            jsonOptions
        );
        Console.WriteLine($"Review count: {reviewsCount}");
    }
}