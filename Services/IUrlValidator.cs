namespace custom_image_downloader.Services;

public interface IUrlValidator
{
    /// <summary>
    /// Splits the input lines into two groups: well-formed absolute http/https URLs,
    /// and lines that are not valid URLs.
    /// </summary>
    (string[] ValidUrls, string[] InvalidLines) Validate(string[] lines);
}
