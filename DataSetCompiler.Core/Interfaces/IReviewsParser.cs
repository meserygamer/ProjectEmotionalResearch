using DataSetCompiler.Core.DomainEntities;

namespace DataSetCompiler.Core.Interfaces;

public interface IReviewsParser
{
    ICollection<Review> GetAllReviews();
}