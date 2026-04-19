namespace custom_image_downloader.Models;

public sealed class DownloadSettings
{
    public List<string> AllowedExtensions { get; set; } = new();

    public List<string> AllowedMimeTypes { get; set; } = new();

    public Dictionary<string, string> MimeTypeExtensionMap { get; set; } = new();
}
