using System.Text.Json.Serialization;

namespace DataSetCompiler.Core.DomainEntities;

[Serializable]
public class Review
{
    [JsonPropertyName("title")] 
    public string ReviewTitle { get; set; } = null!;

    [JsonPropertyName("review_opinion")]
    public string ReviewOpinion { get; set; } = null!;
    
    [JsonPropertyName("text")] 
    public string ReviewText { get; set; } = null!;
}