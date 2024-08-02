using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataSetCompiler.API.AppSettings;


[Serializable]
public class AppSettings
{
    [JsonPropertyName("kinopoisk_parser_settings")]
    public KinopoiskParserSettings KinopoiskParserSettings { get; set; }


    public static async Task<AppSettings?> FromJsonFileAsync(string path)
    {
        if (String.IsNullOrEmpty(path))
            throw new ArgumentException("path was null or empty");

        await using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read)) 
            return await JsonSerializer.DeserializeAsync<AppSettings>(fs);
    }
}