namespace custom_image_downloader.Services
{
    public interface ILogger
    {
        Task EscribirAsync(string mensaje);
    }
}