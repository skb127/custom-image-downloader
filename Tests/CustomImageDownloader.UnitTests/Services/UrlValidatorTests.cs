using custom_image_downloader.Services;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UnitTests.Services;

[TestFixture]
public class UrlValidatorTests
{
    private UrlValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new UrlValidator();
    }

    [Test]
    public void Validate_ValidHttpUrl_ReturnsInValid()
    {
        // Arrange
        string[] input = ["http://example.com/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().ContainSingle().Which.Should().Be("http://example.com/img.png");
        result.InvalidLines.Should().BeEmpty();
    }

    [Test]
    public void Validate_ValidHttpsUrl_ReturnsInValid()
    {
        // Arrange
        string[] input = ["https://example.com/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().ContainSingle().Which.Should().Be("https://example.com/img.png");
        result.InvalidLines.Should().BeEmpty();
    }

    [Test]
    public void Validate_UrlWithSpaces_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["https://example.com/my image.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("https://example.com/my image.png");
    }

    [Test]
    public void Validate_UrlWithTab_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["https://example.com/\timg.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("https://example.com/\timg.png");
    }

    [Test]
    public void Validate_FtpScheme_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["ftp://example.com/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("ftp://example.com/img.png");
    }

    [Test]
    public void Validate_FileScheme_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["file:///C:/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("file:///C:/img.png");
    }

    [Test]
    public void Validate_RelativeUrl_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["/images/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("/images/img.png");
    }

    [Test]
    public void Validate_EmptyString_ReturnsInInvalid()
    {
        // Arrange
        string[] input = [""];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("");
    }

    [Test]
    public void Validate_GarbageString_ReturnsInInvalid()
    {
        // Arrange
        string[] input = ["not-a-url-at-all"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("not-a-url-at-all");
    }

    [Test]
    public void Validate_MixedValidAndInvalid_SplitsCorrectly()
    {
        // Arrange
        string[] input = ["https://a.com/1.png", "garbage", "http://b.com/2.jpg"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().HaveCount(2).And.ContainInOrder("https://a.com/1.png", "http://b.com/2.jpg");
        result.InvalidLines.Should().ContainSingle().Which.Should().Be("garbage");
    }

    [Test]
    public void Validate_EmptyArray_ReturnsBothEmpty()
    {
        // Arrange
        string[] input = [];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().BeEmpty();
        result.InvalidLines.Should().BeEmpty();
    }

    [Test]
    public void Validate_UrlWithQueryString_ReturnsValid()
    {
        // Arrange
        string[] input = ["https://example.com/img?id=1"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().ContainSingle().Which.Should().Be("https://example.com/img?id=1");
        result.InvalidLines.Should().BeEmpty();
    }

    [Test]
    public void Validate_UrlWithFragment_ReturnsValid()
    {
        // Arrange
        string[] input = ["https://example.com/img#section"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().ContainSingle().Which.Should().Be("https://example.com/img#section");
        result.InvalidLines.Should().BeEmpty();
    }

    [Test]
    public void Validate_UrlWithPort_ReturnsValid()
    {
        // Arrange
        string[] input = ["https://example.com:8080/img.png"];

        // Act
        var result = _sut.Validate(input);

        // Assert
        result.ValidUrls.Should().ContainSingle().Which.Should().Be("https://example.com:8080/img.png");
        result.InvalidLines.Should().BeEmpty();
    }
}
