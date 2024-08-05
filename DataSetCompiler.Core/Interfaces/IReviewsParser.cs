using System.Text.Json;
using DataSetCompiler.Core.DomainEntities;

namespace DataSetCompiler.Core.Interfaces;

public interface IReviewsParser
{
    Task<ICollection<Film>> GetAllReviewsAsync(ICollection<string> filmUrls);
    
    Task<int> PrintAllReviewsIntoFileAsync(ICollection<string> filmUrls,
        JsonSerializerOptions serializerOptions,
        string outputFile);
}