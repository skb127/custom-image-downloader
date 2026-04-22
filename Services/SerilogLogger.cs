namespace custom_image_downloader.Services;

public class SerilogLogger : ILogger
{
    private readonly Serilog.ILogger _log;

    public SerilogLogger(Serilog.ILogger log)
    {
        _log = log;
    }

    public Task EscribirAsync(string mensaje)
    {
        _log.Information(mensaje);
        return Task.CompletedTask;
    }

    public Task EscribirErrorAsync(string mensaje, Exception ex)
    {
        _log.Error(ex, mensaje);
        return Task.CompletedTask;
    }
}
