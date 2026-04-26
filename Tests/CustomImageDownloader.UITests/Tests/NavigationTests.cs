using CustomImageDownloader.UITests.Infrastructure;
using CustomImageDownloader.UITests.Pages;
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UITests.Tests;

[TestFixture]
public class NavigationTests : TestBase
{
    private MainFormPage _page = null!;

    [SetUp]
    public void SetUp() => _page = new MainFormPage(MainWindow);

    [Test]
    public void BtnLimpiar_ClearsAllTextFields()
    {
        // Arrange
        _page.TxtUrls.Enter("https://example.com/test.png");
        _page.TxtNombreBase.Enter("test_folder");

        // Act
        _page.BtnLimpiar.Click();

        // Assert
        _page.TxtUrls.Text.Should().BeEmpty("txtUrls should be empty after clicking Clear");
        _page.TxtNombreBase.Text.Should().BeEmpty("txtNombreBase should be empty after clicking Clear");
    }

    [Test]
    public void BtnSeleccionarCarpeta_OpensFolderBrowserDialog()
    {
        // Act
        _page.BtnSeleccionarCarpeta.Click();

        // Assert
        WaitUntil(() => MainWindow.ModalWindows.Any(), "a folder browser dialog should open", TimeSpan.FromSeconds(5));
        var modal = MainWindow.ModalWindows.First();

        // In native Windows dialogs, the Cancel button always has the AutomationId "2" (IDCANCEL)
        var closeBtn = modal.FindFirstDescendant(cf => cf.ByAutomationId("2").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))?.AsButton();

        closeBtn?.Invoke();
        if (closeBtn == null) modal.Close();
    }

    [Test]
    public void TxtCarpeta_Click_OpensFolderBrowserDialog()
    {
        // Act
        _page.TxtCarpeta.Click();

        // Assert
        WaitUntil(() => MainWindow.ModalWindows.Any(), "a folder browser dialog should open when clicking txtCarpeta", TimeSpan.FromSeconds(5));
        var modal = MainWindow.ModalWindows.First();

        var closeBtn = modal.FindFirstDescendant(cf => cf.ByAutomationId("2").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))?.AsButton();

        closeBtn?.Invoke();
        if (closeBtn == null) modal.Close();
    }
}
