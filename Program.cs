using custom_image_downloader.Services;
using Microsoft.Extensions.DependencyInjection;

namespace custom_image_downloader
{
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

            var services = new ServiceCollection();
            ConfigureServices(services);
            using var serviceProvider = services.BuildServiceProvider();
            var mainForm = serviceProvider.GetRequiredService<BulkImageDownloaderForm>();
            Application.Run(mainForm);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ILogger, Logger>();
            services.AddTransient<IDownloadManager, DownloadManager>();
            services.AddTransient<BulkImageDownloaderForm>();
        }
    }
}