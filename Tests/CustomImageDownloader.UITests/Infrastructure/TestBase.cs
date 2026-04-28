using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA2;
using NUnit.Framework;
using CustomImageDownloader.UITests.Tests;

namespace CustomImageDownloader.UITests.Infrastructure;

public abstract class TestBase
{
    protected Application App { get; private set; } = null!;
    protected Window MainWindow { get; private set; } = null!;
    protected UIA2Automation Automation { get; private set; } = null!;

    /// <summary>
    /// Base URL of the local nginx test server started by <see cref="GlobalSetupFixture"/>.
    /// </summary>
    protected static string ServerBaseUrl => GlobalSetupFixture.BaseUrl;

    /// <summary>
    /// Unique temp directory for each test, cleaned up automatically in TearDown.
    /// </summary>
    protected string? TestOutputDir { get; private set; }

    private static string ResolveAppPath()
    {
        // 1. Variable de entorno (prioridad máxima)
        string? envPath = Environment.GetEnvironmentVariable("APP_UNDER_TEST");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // 2. Path relativo al directorio de ejecución de tests
        string testDir = TestContext.CurrentContext.TestDirectory;

        string debugExe = Path.GetFullPath(
            Path.Combine(testDir, "..", "..", "..", "..", "..",
                "bin", "Debug", $"net{Environment.Version.Major}.0-windows", "custom image downloader.exe"));

        string releaseExe = Path.GetFullPath(
            Path.Combine(testDir, "..", "..", "..", "..", "..",
                "bin", "Release", $"net{Environment.Version.Major}.0-windows", "custom image downloader.exe"));

        if (File.Exists(debugExe))
            return debugExe;

        if (File.Exists(releaseExe))
            return releaseExe;

        throw new FileNotFoundException(
            $"Application executable not found. Set the APP_UNDER_TEST environment variable or ensure the main project is built. Searched: {debugExe} and {releaseExe}");
    }

    [SetUp]
    public void BaseSetUp()
    {
        // Configurar directorio de descarga único para este test
        TestOutputDir = Path.Combine(Path.GetTempPath(), "UITests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TestOutputDir);
        Environment.SetEnvironmentVariable("DOWNLOAD_OUTPUT_DIR", TestOutputDir);

        Automation = new UIA2Automation();
        App = Application.Launch(ResolveAppPath());
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(10)) ??
                     throw new InvalidOperationException("Main window not found.");
    }

    [TearDown]
    public void BaseTearDown()
    {
        App.Close();
        Automation.Dispose();

        Environment.SetEnvironmentVariable("DOWNLOAD_OUTPUT_DIR", null);
        if (TestOutputDir != null && Directory.Exists(TestOutputDir))
        {
            try
            {
                Directory.Delete(TestOutputDir, true);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
