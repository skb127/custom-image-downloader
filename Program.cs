using custom_image_downloader.Models;
using custom_image_downloader.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace custom_image_downloader;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();

        // Ensure Serilog is flushed on exit
        Application.ApplicationExit += (_, _) => Log.CloseAndFlush();

        var services = new ServiceCollection();
        ConfigureServices(services);
        using var serviceProvider = services.BuildServiceProvider();
        var mainForm = serviceProvider.GetRequiredService<BulkImageDownloaderForm>();
        Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Build configuration from appsettings.json (copied to output directory)
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // Initialize Serilog from configuration
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        // Options Pattern: bind AppSettings section and make it available as IOptions<DownloadSettings>
        services.Configure<DownloadSettings>(config);

        services.AddSingleton<Services.ILogger>(new SerilogLogger(Log.Logger));

        services.AddSingleton<IUrlValidator, UrlValidator>();

        services.AddTransient<IDownloadManager, DownloadManager>();
        services.AddTransient<BulkImageDownloaderForm>();
    }
}