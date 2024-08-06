using System.Text;
using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;
using DataSetCompiler.Core.Interfaces;
using OpenQA.Selenium;

namespace KinopoiskFilmReviewsParser.Parsers.FilmsParser;

public class KinopoiskFilmsParser : IReviewsParser
{
    #region Fields

    private readonly Func<IWebDriver> _webDriverFactory;

    private readonly KinopoiskFilmsParserWorker[] _workers; 

    #endregion
    
    
    #region Сonstructors

    public KinopoiskFilmsParser(Func<IWebDriver> webDriverFactory, int numberOfWebDriverRunning)
    {
        _webDriverFactory = webDriverFactory ?? throw new ArgumentNullException(nameof(webDriverFactory));
        _workers = new KinopoiskFilmsParserWorker[numberOfWebDriverRunning];
        for (int i = 0; i < numberOfWebDriverRunning; i++) 
            _workers[i] = new KinopoiskFilmsParserWorker(_webDriverFactory, null);
    }

    #endregion


    #region IReviewsParser

    public async Task<List<Film>> GetAllReviewsAsync(ICollection<string> filmUrls,
        int? frequencyOfWebDriverChanges)
    {
        if (filmUrls is null)
            throw new ArgumentNullException(nameof(filmUrls));

        Queue<string> filmsUrlsOnParsing = new Queue<string>(filmUrls);
        List<Film> films = new List<Film>();
        
        Task<ICollection<Film>>[] tasksOnParsing = new Task<ICollection<Film>>[_workers.Length];
        Parallel.For(0, _workers.Length, (index) =>
        {
            _workers[index].FrequencyOfWebDriverChanges = frequencyOfWebDriverChanges;
            tasksOnParsing[index] = new Task<ICollection<Film>>(() => _workers[index].ParseFilmsAsync(filmsUrlsOnParsing).Result);
            tasksOnParsing[index].Start();
        });

        Task.WaitAll(tasksOnParsing);
        foreach (var taskOnParsing in tasksOnParsing) 
            films.AddRange(taskOnParsing.Result);

        return films;
    }

    public async Task<List<Film>> PrintAllReviewsIntoFileAsync(ICollection<string> filmUrls,
        int? frequencyOfWebDriverChanges,
        JsonSerializerOptions serializerOptions,
        string outputFile = "reviews.json")
    {
        if (String.IsNullOrEmpty(outputFile))
            throw new ArgumentException("Path to file was incorrect", nameof(outputFile));

        List<Film> films = (await GetAllReviewsAsync(filmUrls, frequencyOfWebDriverChanges));
        
        string filmsJson = await Task.Run(() => JsonSerializer.Serialize(films, serializerOptions));
        using (var fs = new FileStream(outputFile, FileMode.Create))
        {
            byte[] buffer = Encoding.UTF8.GetBytes(filmsJson);
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return films;
    }

    #endregion
}