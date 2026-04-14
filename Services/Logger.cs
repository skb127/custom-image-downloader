namespace custom_image_downloader.Services;

public class Logger : ILogger
{
    private readonly string _carpetaLogs;

    public Logger()
    {
        // We calculate and create the base folder only once when the Logger is instantiated
        _carpetaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
        if (!Directory.Exists(_carpetaLogs))
        {
            Directory.CreateDirectory(_carpetaLogs);
        }
    }

    public async Task EscribirAsync(string mensaje)
    {
        try
        {
            // We only calculate the filename for the current day
            string rutaLog = Path.Combine(_carpetaLogs, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
            string lineaLog = $"[{DateTime.Now:HH:mm:ss}] {mensaje}{Environment.NewLine}";
                
            await File.AppendAllTextAsync(rutaLog, lineaLog);
        }
        catch 
        { 
            // If it fails (e.g. full disk or locked file), we silence it to avoid crashing the app
        }
    }
}