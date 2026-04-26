using custom_image_downloader.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UnitTests.Models;

[TestFixture]
public class DownloadResultTests
{
    [Test]
    public void FracasoTotal_NoExitosasConFallidas_ReturnsTrue()
    {
        // Arrange
        var sut = new DownloadResult { Exitosas = 0, Fallidas = 3, FueCancelado = false };
        
        // Act & Assert
        sut.FracasoTotal.Should().BeTrue();
    }

    [Test]
    public void FracasoTotal_AlgunaExitosa_ReturnsFalse()
    {
        // Arrange
        var sut = new DownloadResult { Exitosas = 1, Fallidas = 2, FueCancelado = false };
        
        // Act & Assert
        sut.FracasoTotal.Should().BeFalse();
    }

    [Test]
    public void FracasoTotal_CanceladoSinExitosas_ReturnsFalse()
    {
        // Arrange
        var sut = new DownloadResult { Exitosas = 0, Fallidas = 3, FueCancelado = true };
        
        // Act & Assert
        sut.FracasoTotal.Should().BeFalse();
    }

    [Test]
    public void FracasoTotal_TodoOk_ReturnsFalse()
    {
        // Arrange
        var sut = new DownloadResult { Exitosas = 5, Fallidas = 0, FueCancelado = false };
        
        // Act & Assert
        sut.FracasoTotal.Should().BeFalse();
    }

    [Test]
    public void FracasoTotal_SinDescargas_ReturnsFalse()
    {
        // Arrange
        var sut = new DownloadResult { Exitosas = 0, Fallidas = 0, FueCancelado = false };
        
        // Act & Assert
        sut.FracasoTotal.Should().BeFalse();
    }

    [Test]
    public void DefaultState_CountersAreZero()
    {
        // Arrange & Act
        var sut = new DownloadResult();
        
        // Assert
        sut.Exitosas.Should().Be(0);
        sut.Fallidas.Should().Be(0);
        sut.Omitidas.Should().Be(0);
        sut.FueCancelado.Should().BeFalse();
    }

    [Test]
    public void DefaultState_ListsAreEmptyNotNull()
    {
        // Arrange & Act
        var sut = new DownloadResult();
        
        // Assert
        sut.RutasDescargadas.Should().NotBeNull().And.BeEmpty();
        sut.RutasEnProgreso.Should().NotBeNull().And.BeEmpty();
    }
}
