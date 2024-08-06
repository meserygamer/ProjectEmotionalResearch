using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SeleniumStealth.NET.Clients.Extensions;

namespace KinopoiskFilmReviewsParser.Parsers.LinksParsers;

public class KinopoiskTopСontroversialFilmsLinksParser : ILinkParser
{
    private const string KinopoiskTopControversialFilmsPrimaryUrl = 
        "https://www.kinopoisk.ru/top/navigator/m_act[num_vote]/1000/m_act[rating]/1%3A6/m_act[tomat_rating]/%3A60/order/num_vote/perpage/200/#results";
    private const string KinopoiskTopControversialFilmsPageUrlFormat =
        "https://www.kinopoisk.ru/top/navigator/m_act[num_vote]/1000/m_act[rating]/1:6/m_act[tomat_rating]/:60/order/num_vote/page/";

    private const int NumberFilmLinksOnPage = 200;
    
    
    private IWebDriver _webDriver;
    
    
    public KinopoiskTopСontroversialFilmsLinksParser(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }
    
    
    public async Task<List<string>> GetLinksAsync(int maxLinksCount)
    {
        int numberOfPages = await CalculateNumberOfPagesToParseAsync(maxLinksCount);
        List<string> filmLinks = new();
        
        for (int i = 1; i <= numberOfPages; i++)
            filmLinks.AddRange(await GetFilmLinksFromPageAsync(i));
        return (filmLinks.Count > maxLinksCount)? filmLinks[0..maxLinksCount] : filmLinks;
    }

    public async Task<List<string>> GetLinksWithPrintAsync(int maxLinksCount,
        JsonSerializerOptions serializerOptions,
        string outputFile = "TopControversialFilmsLinks.json")
    {
        if (String.IsNullOrEmpty(outputFile))
            throw new ArgumentException("Path to file was incorrect", nameof(outputFile));

        List<string> links = await GetLinksAsync(maxLinksCount);
        
        string linksJson = await Task.Run(() => JsonSerializer.Serialize(links, serializerOptions));
        using (var fs = new FileStream(outputFile, FileMode.Create))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(linksJson);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return links;
    }
    
    
    private async Task<int> CalculateNumberOfPagesToParseAsync(int maxLinksCount)
    {
        if (maxLinksCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLinksCount));

        maxLinksCount = Math.Min(await GetFilmsNumberAsync(), maxLinksCount);
        return (int)Math.Ceiling((decimal)maxLinksCount / NumberFilmLinksOnPage);
    }

    private async Task<List<string>> GetFilmLinksFromPageAsync(int pageNumber)
    {
        List<string> filmLinks = new();
        
        await GoToFilmsTopPageAsync(pageNumber);
        return new SeleniumDomExceptionHandler().MakeManyRequestsForDom(() =>
        {
            List<string> filmLinks = new List<string>();
            ReadOnlyCollection<IWebElement> filmsLinksElements = _webDriver.FindElement(By.Id("itemList"))
                .FindElements(By.ClassName("js-ott-widget"));
            foreach (var filmLinkElement in filmsLinksElements)
                filmLinks.Add($"https://www.kinopoisk.ru/film/{filmLinkElement.GetAttribute("data-kp-film-id")}/");
            return filmLinks;
        });
    }
    
    private async Task GoToFilmsTopPageAsync(int pageNumber = 0)
    {
        if(pageNumber <= 0)
            await _webDriver.Navigate().GoToUrlAsync(KinopoiskTopControversialFilmsPrimaryUrl);
        else
            await _webDriver.Navigate().GoToUrlAsync(KinopoiskTopControversialFilmsPageUrlFormat + pageNumber);
        
        WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(40));
        wait.Until(ExpectedConditions.ElementIsVisible(By.Id("itemList")));
        _webDriver.SpecialWait(new Random().Next(2000, 3000));
    }

    private async Task<int> GetFilmsNumberAsync()
    {
        await GoToFilmsTopPageAsync();

        string pageData = new SeleniumDomExceptionHandler()
            .MakeManyRequestsForDom(() => _webDriver.FindElement(By.ClassName("pagesFromTo")).Text);
        return Convert.ToInt32(pageData.Split(" ")[^1]);
    }
}