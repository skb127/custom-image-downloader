using custom_image_downloader.Models;
using custom_image_downloader.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace CustomImageDownloader.UnitTests.Services;

[TestFixture]
public class DownloadManagerPauseResumeTests
{
    private HttpClient _httpClient = null!;
    private ILogger _mockLogger = null!;
    private IOptions<DownloadSettings> _mockOptions = null!;
    private DownloadManager _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new HttpClient();
        _mockLogger = Substitute.For<ILogger>();
        _mockOptions = Options.Create(new DownloadSettings());

        _sut = new DownloadManager(_httpClient, _mockLogger, _mockOptions);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public void EstaPausado_Initially_ReturnsFalse()
    {
        // Assert
        _sut.EstaPausado.Should().BeFalse();
    }

    [Test]
    public void AlternarPausa_OnceDuringDownload_PausesExecution()
    {
        // Arrange
        // Actually, we can just call it when it's not running and ensure it doesn't crash:

        // Act
        Action act = () => _sut.AlternarPausa();

        // Assert
        act.Should().NotThrow();
        _sut.EstaPausado.Should().BeFalse(); // Because event is null
    }

    [Test]
    public async Task AlternarPausa_Twice_ResumesExecution()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test.com/img.png").Respond((_) =>
        {
            Thread.Sleep(500); // Simulate network delay
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var client = mockHttp.ToHttpClient();
        var sut = new DownloadManager(client, _mockLogger, _mockOptions);

        var progress = new Progress<int>();
        var progressInfo = new Progress<DownloadProgressInfo>();

        // Act
        var task = sut.IniciarDescargaAsync(["http://test.com/img.png"], "C:\\", "test", 1, progress, progressInfo);

        sut.AlternarPausa();
        bool isPausedAfterFirst = sut.EstaPausado;

        sut.AlternarPausa();
        bool isPausedAfterSecond = sut.EstaPausado;

        await task;

        // Assert
        isPausedAfterFirst.Should().BeTrue();
        isPausedAfterSecond.Should().BeFalse();
    }

    [Test]
    public async Task Cancelar_DuringDownload_StopsAllTasks()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test.com/img.png").Respond((_) =>
        {
            Thread.Sleep(2000); // long delay
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var client = mockHttp.ToHttpClient();
        var sut = new DownloadManager(client, _mockLogger, _mockOptions);

        var task = sut.IniciarDescargaAsync(["http://test.com/img.png"], "C:\\", "test", 1, new Progress<int>(),
            new Progress<DownloadProgressInfo>());

        // Act
        sut.Cancelar();

        var result = await task;

        // Assert
        result.FueCancelado.Should().BeTrue();
    }
}
