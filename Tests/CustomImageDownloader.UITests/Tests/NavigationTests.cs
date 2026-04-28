using CustomImageDownloader.UITests.Infrastructure;
using CustomImageDownloader.UITests.Pages;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
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

        // Assert: wait for the modal to appear with a retry mechanism to handle potential delays in opening the dialog
        var retryResult = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "The folder browser doesnt appear after clicking btnSeleccionarCarpeta within the expected time"
        );

        retryResult.Result.Should()
            .NotBeNullOrEmpty("The folder browser dialog should be present after clicking btnSeleccionarCarpeta");

        var modal = retryResult.Result.First();

        modal.Should().NotBeNull("The folder browser dialog should be open and accessible");

        CloseModal(modal);
    }

    [Test]
    public void TxtCarpeta_Click_OpensFolderBrowserDialog()
    {
        // Act
        _page.TxtCarpeta.Click();

        // Assert: wait for the modal to appear with a retry mechanism to handle potential delays in opening the dialog
        var retryResult = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "The folder browser doesnt appear after clicking txtCarpeta within the expected time"
        );

        retryResult.Result.Should()
            .NotBeNullOrEmpty("The folder browser dialog should be present after clicking txtCarpeta");

        var modal = retryResult.Result.First();

        modal.Should().NotBeNull("The folder browser dialog should be open and accessible");

        CloseModal(modal);
    }

    /// <summary>
    /// Closes the modal dialog by first attempting to find a cancel button
    /// </summary>
    private static void CloseModal(Window modal)
    {
        var cancelButton = FindCancelButton(modal);
        if (cancelButton is not null)
            cancelButton.Invoke();
        else
            modal.Close();

        Retry.WhileTrue(
            () => modal.IsAvailable,
            timeout: TimeSpan.FromSeconds(5),
            throwOnTimeout: false
        );
    }

    /// <summary>
    /// Find the cancel button by AutomationId "2" or by name "Cancelar"/"Cancel". This method accounts for both the standard Windows dialog and potential localization.
    /// </summary>
    private static Button? FindCancelButton(Window modal)
    {
        // First attempt: by standard AutomationId of the Windows dialog
        var byId = modal.FindFirstDescendant(cf =>
            cf.ByAutomationId("2").And(cf.ByControlType(ControlType.Button)))
            ?.AsButton();

        if (byId is not null) return byId;

        // Second attempt: By name, taking both languages into account 
        return modal.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button)
                .And(cf.ByName("Cancelar").Or(cf.ByName("Cancel"))))
            ?.AsButton();
    }
}
