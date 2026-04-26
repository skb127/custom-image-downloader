using custom_image_downloader.Services;
using NSubstitute;
using NUnit.Framework;

namespace CustomImageDownloader.UnitTests.Services;

[TestFixture]
public class SerilogLoggerTests
{
    private Serilog.ILogger _mockSerilog = null!;
    private SerilogLogger _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSerilog = Substitute.For<Serilog.ILogger>();
        _sut = new SerilogLogger(_mockSerilog);
    }

    [Test]
    public async Task EscribirAsync_DelegatesToSerilogInformation()
    {
        // Arrange
        string message = "Test message";

        // Act
        await _sut.EscribirAsync(message);

        // Assert
        _mockSerilog.Received(1).Information(message);
    }

    [Test]
    public async Task EscribirErrorAsync_DelegatesToSerilogError()
    {
        // Arrange
        string message = "Test error message";
        var exception = new Exception("Boom");

        // Act
        await _sut.EscribirErrorAsync(message, exception);

        // Assert
        _mockSerilog.Received(1).Error(exception, message);
    }
}
