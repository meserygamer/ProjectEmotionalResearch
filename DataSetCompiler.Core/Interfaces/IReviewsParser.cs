using DataSetCompiler.Core.DomainEntities;

namespace DataSetCompiler.Core.Interfaces;

public interface IReviewsParser
{
    Task<ICollection<Review>> GetAllReviewsAsync();
}