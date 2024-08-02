using System.Text.Json.Serialization;

namespace DataSetCompiler.Core.DomainEntities;

[Serializable]
public class Film
{
    [JsonPropertyName("film_title")] 
    public string FilmTitle { get; set; } = null!;

    [JsonPropertyName("year_of_release")]
    public int YearOfRelease { get; set; }
    
    [JsonPropertyName("film_url")] 
    public string FilmUrl { get; set; } = null!;

    [JsonPropertyName("film_reviews")] 
    public List<Review> Reviews { get; set; } = null!;
}