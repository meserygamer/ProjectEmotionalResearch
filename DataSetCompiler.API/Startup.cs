using System.Text.Encodings.Web;
using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;
using KinopoiskFilmReviewsParser.BrowserDrivers;
using KinopoiskFilmReviewsParser.Parsers.FilmsParser;
using KinopoiskFilmReviewsParser.Parsers.LinksParsers;
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
        
        List<string> filmsUrls;
        if (File.Exists(appSettings.KinopoiskParserSettings.FilmsUrlsFilePath))
        {
            await using (FileStream fs = File.Open(appSettings.KinopoiskParserSettings.FilmsUrlsFilePath,
                             FileMode.Open,
                             FileAccess.Read)) 
            filmsUrls = await JsonSerializer.DeserializeAsync<List<string>>(fs) ?? [];
        }
        else 
            filmsUrls = await new KinopoiskTop500FilmsLinksParser(() => new ChromeStealthDriverBuilder()
                    .AddCookie("https://www.kinopoisk.ru/", appSettings.KinopoiskParserSettings.Cookies)
                    .BuildAsync().Result)
                .GetLinksWithPrintAsync(500, jsonOptions);

        List<Film> films = await new KinopoiskFilmsParser(() => new ChromeStealthDriverBuilder()
                .AddCookie("https://www.kinopoisk.ru/", appSettings.KinopoiskParserSettings.Cookies)
                .BuildAsync().Result, 2)
            .PrintAllReviewsIntoFileAsync(
                filmsUrls,
                40,
                jsonOptions,
                $"FilmsReviews.json");
        
        Console.WriteLine($"Review count: {films.Select(film => film.Reviews.Count).Sum()}\n" +
                          $"Film count: {films.Count}");
    }
}