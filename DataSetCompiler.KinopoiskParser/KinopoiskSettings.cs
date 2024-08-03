namespace KinopoiskFilmReviewsParser;

public class KinopoiskSettings
{
    public string? Cookies { get; set; }

    public ICollection<string> FilmsUrls { get; set; } = null!;
}