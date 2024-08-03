namespace DataSetCompiler.Core.Interfaces;

public interface ILinkParser
{
    Task<List<string>> GetLinksAsync(int maxLinksCount);
}