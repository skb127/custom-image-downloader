using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

namespace CustomImageDownloader.UITests.Tests;

/// <summary>
/// Global setup for all UI tests. Initializes required infrastructure such as the Nginx container.
/// Access the server URL from any test via <see cref="BaseUrl"/>.
/// </summary>
[SetUpFixture]
public class GlobalSetupFixture
{
    private IContainer? _container;

    /// <summary>
    /// The base URL of the running nginx container, e.g. "http://localhost:49320".
    /// Available after <see cref="StartNginxAsync"/> completes.
    /// </summary>
    public static string BaseUrl { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task StartNginxAsync()
    {
        string assetsPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Assets");

        if (!Directory.Exists(assetsPath))
            throw new DirectoryNotFoundException(
                $"Assets directory not found at: {assetsPath}. " +
                "Ensure the Assets/ folder and its contents are set to CopyToOutputDirectory=PreserveNewest.");

        // Create a custom Nginx configuration to throttle the download speed
        string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "default.conf");
        await File.WriteAllTextAsync(configPath, @"
server {
    listen 80;
    server_name localhost;
    location / {
        root /usr/share/nginx/html;
        index index.html index.htm;
    }
    location /large/ {
        root /usr/share/nginx/html;
        limit_rate 4m; # Throttle speed to 4 MB/s for large files
    }
}");

        try
        {
            _container = new ContainerBuilder("nginx:alpine")
                // Bind container port 80 to a random free host port to avoid conflicts
                .WithPortBinding(80, assignRandomHostPort: true)
                // Inject the custom configuration to throttle the speed
                .WithResourceMapping(configPath, "/etc/nginx/conf.d")
                // Copy all local test images into the nginx web root
                .WithResourceMapping(assetsPath, "/usr/share/nginx/html")
                // Wait until nginx is accepting TCP connections before proceeding
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(80))
                .Build();

            await _container.StartAsync();

            int hostPort = _container.GetMappedPublicPort(80);
            BaseUrl = $"http://localhost:{hostPort}";
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to start the Nginx container. Ensure that Docker Desktop or the Docker engine is running.\nError detail: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public async Task StopNginxAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}