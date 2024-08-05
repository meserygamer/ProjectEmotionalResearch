using System.Text.Json.Serialization;

namespace DataSetCompiler.API.AppSettings;

public class KinopoiskParserSettings
{
    [JsonPropertyName("films_urls_file_path")]
    public string? FilmsUrlsFilePath { get; set; }

    [JsonPropertyName("cookies")]
    public string? Cookies { get; set; }
}