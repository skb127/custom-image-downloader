namespace custom_image_downloader.Models;

public enum DownloadState
{
    Starting,
    Processing,
    Paused
}

public class DownloadProgressInfo
{
    public DownloadState State { get; set; }
    public int Processed { get; set; }
    public int Total { get; set; }
}