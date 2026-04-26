using System.Text.RegularExpressions;
using CustomImageDownloader.UITests.Infrastructure;
using CustomImageDownloader.UITests.Pages;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UITests.Tests;

[TestFixture]
public class AppStartupTests : TestBase
{
    private MainFormPage _page = null!;

    [SetUp]
    public void SetUp() => _page = new MainFormPage(MainWindow);

    [Test]
    public void App_LaunchesSuccessfully_WindowIsVisible()
    {
        MainWindow.Should().NotBeNull();
        MainWindow.IsOffscreen.Should().BeFalse("the main window should be visible on screen");
    }

    [Test]
    public void App_Title_ContainsVersionInfo()
    {
        MainWindow.Title.Should().Contain("image downloader", "it should have the base title");

        // We verify that there is a semantic version pattern in the title (digits separated by dots)
        var versionRegex = new Regex(@"v\d+\.\d+\.\d+");
        versionRegex.IsMatch(MainWindow.Title).Should().BeTrue($"the title '{MainWindow.Title}' should contain a semantic version pattern");
    }

    [Test]
    public void App_MinimumWindowSize_IsRespected()
    {
        var bounds = MainWindow.BoundingRectangle;
        
        bounds.Width.Should().BeGreaterThanOrEqualTo(500, "the window should have a reasonable minimum width");
        bounds.Height.Should().BeGreaterThanOrEqualTo(400, "the window should have a reasonable minimum height");
    }

    [Test]
    public void InitialState_ControlsAreCorrectlyEnabled()
    {
        _page.TxtUrls.IsEnabled.Should().BeTrue("txtUrls should be enabled on startup");
        _page.TxtCarpeta.IsEnabled.Should().BeTrue("txtCarpeta should be enabled on startup");
        _page.TxtNombreBase.IsEnabled.Should().BeTrue("txtNombreBase should be enabled on startup");
        _page.BtnDescargar.IsEnabled.Should().BeTrue("btnDescargar should be enabled on startup");
        _page.BtnLimpiar.IsEnabled.Should().BeTrue("btnLimpiar should be enabled on startup");
        _page.BtnSeleccionarCarpeta.IsEnabled.Should().BeTrue("btnSeleccionarCarpeta should be enabled on startup");
        _page.NumConcurrencia.IsEnabled.Should().BeTrue("numConcurrencia should be enabled on startup");

        _page.BtnCancelar.IsEnabled.Should().BeFalse("btnCancelar should be disabled on startup");
        _page.BtnPausar.IsEnabled.Should().BeFalse("btnPausar should be disabled on startup");
    }

    [Test]
    public void LblEstado_ShowsTextOnStart()
    {
        _page.LblEstado.Text.Should().NotBeNullOrEmpty(
            "the status label should show initial text (e.g., 'Ready')");
    }

    [Test]
    public void NumConcurrencia_ExistsInUiTree()
    {
        _page.NumConcurrencia.Should().NotBeNull("the concurrency control should exist");
    }

    [Test]
    public void PbProgreso_StartsAtZero()
    {
        _page.PbProgreso.Value.Should().Be(0, "the progress bar should start at 0");
    }
}
