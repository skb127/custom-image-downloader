using System.Net;
using custom_image_downloader.Models;
using custom_image_downloader.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace CustomImageDownloader.UnitTests.Services;

[TestFixture]
public class DownloadManagerTests
{
    private ILogger _mockLogger = null!;
    private DownloadSettings _settings = null!;
    private IOptions<DownloadSettings> _mockOptions = null!;
    private MockHttpMessageHandler _mockHttp = null!;
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = Substitute.For<ILogger>();

        _settings = new DownloadSettings
        {
            AllowedExtensions = [".png", ".jpg", ".bin"],
            AllowedMimeTypes = ["image/png", "image/jpeg", "application/octet-stream"],
            MimeTypeExtensionMap = new Dictionary<string, string>
            {
                { "image/png", ".png" },
                { "image/jpeg", ".jpg" },
                { "application/octet-stream", ".bin" }
            }
        };
        _mockOptions = Options.Create(_settings);

        _mockHttp = new MockHttpMessageHandler();

        _testDir = Path.Combine(Path.GetTempPath(), "DownloadManagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private DownloadManager CreateSut()
    {
        return new DownloadManager(_mockHttp.ToHttpClient(), _mockLogger, _mockOptions);
    }

    #region ObtenerRutaSinConflicto (Tier 0)

    [Test]
    public void ObtenerRutaSinConflicto_NoExistingFile_ReturnsOriginalPath()
    {
        // Arrange
        string filename = "test.png";
        string expectedPath = Path.Combine(_testDir, filename);

        // Act
        string result = DownloadManager.ObtenerRutaSinConflicto(_testDir, filename);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Test]
    public void ObtenerRutaSinConflicto_OneConflict_AppendsSuffix2()
    {
        // Arrange
        string filename = "test.png";
        string originalPath = Path.Combine(_testDir, filename);
        File.WriteAllText(originalPath, "dummy");
        string expectedPath = Path.Combine(_testDir, "test_2.png");

        // Act
        string result = DownloadManager.ObtenerRutaSinConflicto(_testDir, filename);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Test]
    public void ObtenerRutaSinConflicto_MultipleConflicts_IncrementsUntilFree()
    {
        // Arrange
        string filename = "test.png";
        File.WriteAllText(Path.Combine(_testDir, filename), "dummy");
        File.WriteAllText(Path.Combine(_testDir, "test_2.png"), "dummy");
        string expectedPath = Path.Combine(_testDir, "test_3.png");

        // Act
        string result = DownloadManager.ObtenerRutaSinConflicto(_testDir, filename);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Test]
    public void ObtenerRutaSinConflicto_PreservesExtension()
    {
        // Arrange
        string filename = "photo.jpeg";
        File.WriteAllText(Path.Combine(_testDir, filename), "dummy");
        string expectedPath = Path.Combine(_testDir, "photo_2.jpeg");

        // Act
        string result = DownloadManager.ObtenerRutaSinConflicto(_testDir, filename);

        // Assert
        result.Should().Be(expectedPath);
    }

    #endregion

    #region Descarga Tier 1 (Extensión en URL)

    [Test]
    public async Task Download_AllowedExtension_Succeeds()
    {
        // Arrange
        string url = "https://example.com/img.png";
        _mockHttp.When(url).Respond("image/png", new MemoryStream([1, 2, 3]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Exitosas.Should().Be(1);
        result.Fallidas.Should().Be(0);
        result.Omitidas.Should().Be(0);
        _ = result.RutasDescargadas.Should().ContainSingle().Which.EndsWith("img.png");
        File.Exists(result.RutasDescargadas[0]).Should().BeTrue();
    }

    [Test]
    public async Task Download_DisallowedExtension_IsSkipped()
    {
        // Arrange
        string url = "https://example.com/virus.exe";
        // No mock HTTP because it shouldn't even make the request
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Exitosas.Should().Be(0);
        result.Omitidas.Should().Be(1);
    }

    [Test]
    public async Task Download_PreservesOriginalFilename()
    {
        // Arrange
        string url = "https://example.com/my_vacation_photo.jpg";
        _mockHttp.When(url).Respond("image/jpeg", new MemoryStream([1]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        Path.GetFileName(result.RutasDescargadas[0]).Should().Be("my_vacation_photo.jpg");
    }

    [Test]
    public async Task Download_UrlWithoutNamePart_UsesFallbackName()
    {
        // Arrange
        string url = "https://example.com/.png";
        _mockHttp.When(url).Respond("image/png", new MemoryStream([1]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "mybase", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        Path.GetFileName(result.RutasDescargadas[0]).Should().Be("mybase_001.png");
    }

    #endregion

    #region Descarga Tier 2 (Sin extensión en URL)

    [Test]
    public async Task Download_NoExtension_AllowedMimeType_InfersExtension()
    {
        // Arrange
        string url = "https://example.com/download/12345";
        _mockHttp.When(url).Respond("image/png", new MemoryStream([1]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Exitosas.Should().Be(1);
        Path.GetFileName(result.RutasDescargadas[0]).Should().Be("base_001.png");
    }

    [Test]
    public async Task Download_NoExtension_DisallowedMimeType_IsSkipped()
    {
        // Arrange
        string url = "https://example.com/download/12345";
        _mockHttp.When(url).Respond("application/zip", new MemoryStream([1]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Exitosas.Should().Be(0);
        result.Omitidas.Should().Be(1);
    }

    [Test]
    public async Task Download_NoExtension_NoContentType_IsSkipped()
    {
        // Arrange
        string url = "https://example.com/download/12345";
        _mockHttp.When(url).Respond((_) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("dummy") };
            response.Content.Headers.ContentType = null;
            return response;
        });
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Omitidas.Should().Be(1);
    }

    [Test]
    public async Task Download_NoExtension_UnmappedMimeType_UsesBinExtension()
    {
        // Arrange
        // Add a mime type that is allowed but NOT mapped in the dictionary
        _settings.AllowedMimeTypes.Add("audio/mp3");
        string url = "https://example.com/audio";
        _mockHttp.When(url).Respond("audio/mp3", new MemoryStream([1]));
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Exitosas.Should().Be(1);
        Path.GetFileName(result.RutasDescargadas[0]).Should().Be("base_001.bin");
    }

    #endregion

    #region Errores y Control de Flujo

    [Test]
    public async Task Download_HttpError_CountsAsFailed()
    {
        // Arrange
        string url = "https://example.com/img.png";
        _mockHttp.When(url).Respond(HttpStatusCode.InternalServerError);
        var sut = CreateSut();

        // Act
        var result = await sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        result.Fallidas.Should().Be(1);
        result.Exitosas.Should().Be(0);
    }

    [Test]
    public async Task Download_Cancelled_SetsFueCancelado()
    {
        // Arrange
        string url = "https://example.com/img.png";
        _mockHttp.When(url).Respond((_) =>
        {
            Thread.Sleep(2000); // Simulate long download
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateSut();

        // Act
        var task = sut.IniciarDescargaAsync([url], _testDir, "base", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());
        sut.Cancelar();
        var result = await task;

        // Assert
        result.FueCancelado.Should().BeTrue();
    }

    [Test]
    public async Task Download_MultipleUrls_RespectsConcurrency()
    {
        // Arrange
        string[] urls = ["https://a.com/1.png", "https://a.com/2.png", "https://a.com/3.png"];
        int activeRequests = 0;
        int maxActiveRequests = 0;
        object lockObj = new object();

        _mockHttp.When("*").Respond((_) =>
        {
            lock (lockObj)
            {
                activeRequests++;
                if (activeRequests > maxActiveRequests) maxActiveRequests = activeRequests;
            }

            Thread.Sleep(100); // Hold the request for a bit
            lock (lockObj)
            {
                activeRequests--;
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream([1])) };
        });

        var sut = CreateSut();

        // Act
        await sut.IniciarDescargaAsync(urls, _testDir, "base", 2, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Assert
        maxActiveRequests.Should().BeLessOrEqualTo(2, "concurrency limit was set to 2");
    }

    [Test]
    public async Task Download_ReportsProgressCorrectly()
    {
        // Arrange
        string[] urls = ["https://a.com/1.png", "https://a.com/2.png"];
        _mockHttp.When("*").Respond("image/png", new MemoryStream([1]));
        var sut = CreateSut();

        var reportedProgress = new List<int>();
        var progressBarra = new Progress<int>(p => reportedProgress.Add(p));

        // Act
        await sut.IniciarDescargaAsync(urls, _testDir, "base", 1, progressBarra, new Progress<DownloadProgressInfo>());

        // Assert
        // There are 2 URLs, so it should report 1 and then 2.
        reportedProgress.Should().ContainInOrder(1, 2);
    }

    #endregion
}
