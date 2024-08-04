using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;
using KinopoiskFilmReviewsParser;
using Mapster;
using OpenQA.Selenium;
using AppConfiguration = DataSetCompiler.API.AppSettings;

namespace DataSetCompiler.API;

public static class Startup
{
    public static async Task Main()
    {
        AppConfiguration.AppSettings? appSettings = await AppConfiguration.AppSettings.FromJsonFileAsync("appsettings.debug.json");
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        if (appSettings is null)
            throw new JsonException("path or appsettings file was incorrect");
        
        IWebDriver webDriver = new ChromeStealthDriverFactory().CreateDriver();
        KinopoiskFilmsLinkParser linkParser = new KinopoiskFilmsLinkParser(() => webDriver,
            appSettings.KinopoiskParserSettings.Adapt<KinopoiskSettings>());
        List<string> linksOnFilms = await linkParser.GetLinksAsync(500);
        
        string links = JsonSerializer.Serialize(linksOnFilms, jsonOptions);
        using (var fs = new FileStream("LinksOnFilms.json", FileMode.Append))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(links);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
        
        KinopoiskFilmsReviewsParser parser = new KinopoiskFilmsReviewsParser(webDriver,
            appSettings.KinopoiskParserSettings.Adapt<KinopoiskSettings>());
        List<Film?> reviews = new List<Film?>(await parser.GetAllReviewsAsync());

        string jsonData = JsonSerializer.Serialize(reviews, jsonOptions);
        using (var fs = new FileStream("ReviewsData.json", FileMode.Append))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}