using NUnit.Framework;
using System.Net;
using System.Net.Sockets;

namespace CustomImageDownloader.UITests.Tests;

/// <summary>
/// Global setup for all UI tests. Initializes required infrastructure such as a local HTTP server.
/// Access the server URL from any test via <see cref="BaseUrl"/>.
/// </summary>
[SetUpFixture]
public class GlobalSetupFixture
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    /// <summary>
    /// The base URL of the running server, e.g. "http://localhost:49320".
    /// Available after <see cref="StartServerAsync"/> completes.
    /// </summary>
    public static string BaseUrl { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public Task StartServerAsync()
    {
        string assetsPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Assets");

        if (!Directory.Exists(assetsPath))
            throw new DirectoryNotFoundException(
                $"Assets directory not found at: {assetsPath}. " +
                "Ensure the Assets/ folder and its contents are set to CopyToOutputDirectory=PreserveNewest.");

        int hostPort = GetRandomUnusedPort();
        BaseUrl = $"http://localhost:{hostPort}";

        _listener = new HttpListener();
        _listener.Prefixes.Add($"{BaseUrl}/");
        _listener.Start();

        _cts = new CancellationTokenSource();

        _serverTask = Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context, assetsPath, _cts.Token);
                }
            }
            catch (HttpListenerException)
            {
                // Expected when listener is stopped
            }
        });

        return Task.CompletedTask;
    }

    private async Task ProcessRequestAsync(HttpListenerContext context, string assetsPath, CancellationToken token)
    {
        try
        {
            string urlPath = context.Request.Url?.AbsolutePath.TrimStart('/') ?? string.Empty;
            string localFilePath = Path.Combine(assetsPath, urlPath);

            if (!File.Exists(localFilePath))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            // Simple throttling for /large/ route
            bool throttle = urlPath.StartsWith("large/");

            // Limit rate: 4MB/s
            int bufferSize = throttle ? 4 * 1024 * 1024 : 81920;

            context.Response.StatusCode = 200;
            byte[] fileBytes = await File.ReadAllBytesAsync(localFilePath, token);
            context.Response.ContentLength64 = fileBytes.Length;

            using Stream output = context.Response.OutputStream;
            int offset = 0;

            while (offset < fileBytes.Length)
            {
                if (token.IsCancellationRequested)
                    break;

                int count = Math.Min(bufferSize, fileBytes.Length - offset);
                await output.WriteAsync(fileBytes.AsMemory(offset, count), token);
                offset += count;

                if (throttle && offset < fileBytes.Length)
                {
                    await Task.Delay(1000, token); // Throttle 4MB per second
                }
            }
            context.Response.Close();
        }
        catch (Exception)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { }
        }
    }

    private int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [OneTimeTearDown]
    public async Task StopServerAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch { }
        }

        _listener?.Close();
    }
}
