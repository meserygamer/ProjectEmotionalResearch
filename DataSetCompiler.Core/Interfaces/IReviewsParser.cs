using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;

namespace DataSetCompiler.Core.Interfaces;

public interface IReviewsParser
{
    Task<List<Film>> GetAllReviewsAsync(ICollection<string> filmUrls,
        int? frequencyOfWebDriverChanges
        );
    
    Task<List<Film>> PrintAllReviewsIntoFileAsync(ICollection<string> filmUrls,
        int? frequencyOfWebDriverChanges,
        JsonSerializerOptions serializerOptions,
        string outputFile);
}