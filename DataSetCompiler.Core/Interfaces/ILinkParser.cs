using System.Text.Json;

namespace DataSetCompiler.Core.Interfaces;

public interface ILinkParser
{
    Task<List<string>> GetLinksAsync(int maxLinksCount);
    
    Task<List<string>> GetLinksWithPrintAsync(
        int maxLinksCount,
        JsonSerializerOptions serializerOptions,
        string outputFile);
}