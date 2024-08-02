using System.Text.Json.Serialization;

namespace DataSetCompiler.API.AppSettings;

public class KinopoiskParserSettings
{
    [JsonPropertyName("cookies")]
    public string? Cookies { get; set; }
    
    [JsonPropertyName("films_urls")]
    public ICollection<string> FilmsUrls { get; set; } = null!;
}