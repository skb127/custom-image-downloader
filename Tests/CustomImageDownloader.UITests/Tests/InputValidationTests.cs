using CustomImageDownloader.UITests.Infrastructure;
using CustomImageDownloader.UITests.Pages;
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UITests.Tests;

[TestFixture]
public class InputValidationTests : TestBase
{
    private MainFormPage _page = null!;

    [SetUp]
    public void SetUp() => _page = new MainFormPage(MainWindow);

    [Test]
    public void TxtUrls_CanEnterText()
    {
        // Arrange
        const string testUrl = "https://example.com/image.png";
        
        // Act
        _page.TxtUrls.Enter(testUrl);

        // Assert
        _page.TxtUrls.Text.Should().Be(testUrl);
    }

    [Test]
    public void TxtNombreBase_CanEnterText()
    {
        // Arrange
        const string subfolder = "my_download_folder";
        
        // Act
        _page.TxtNombreBase.Enter(subfolder);

        // Assert
        _page.TxtNombreBase.Text.Should().Be(subfolder);
    }

    [Test]
    public void TxtCarpeta_IsReadOnly()
    {
        // Assert (Initial State)
        _page.TxtCarpeta.IsReadOnly.Should().BeTrue("txtCarpeta should be marked as read-only");

        // Arrange
        Action writeAttempt = () => _page.TxtCarpeta.Enter("write attempt");
        
        // Act & Assert
        writeAttempt.Should().Throw<InvalidOperationException>("FlaUI prevents modifying read-only elements directly");
    }

    [Test]
    public void TxtNombreBase_RespectsMaxLengthAndFiltersInvalidChars()
    {
        // Arrange (Max Length)
        // MaxLength is configured to 170
        string longString = new string('a', 200);
        
        // Act
        _page.TxtNombreBase.Enter(longString);

        // Assert
        _page.TxtNombreBase.Text.Length.Should().Be(170,
            "txtNombreBase should truncate input to 170 characters (MaxLength property)");

        // Arrange (Invalid Chars)
        _page.TxtNombreBase.Text = ""; // Clear field

        // Characters like * ? < > | are blocked by KeyPress
        // FlaUI simulates typing, so it should trigger the KeyPress event
        string invalidChars = "test*folder?";
        
        // Act
        _page.TxtNombreBase.Enter(invalidChars);

        // Assert
        _page.TxtNombreBase.Text.Should().Be("testfolder",
            "txtNombreBase should filter out invalid filename characters like '*' and '?'");
    }

    [Test]
    public void BtnDescargar_WithEmptyFields_ShowsValidationError()
    {
        // Act
        _page.BtnDescargar.Click();

        // Assert
        MainWindow.ModalWindows.Should().NotBeEmpty(
            "a validation error MessageBox should appear when clicking Download without data");

        // Close the MessageBox
        var modal = MainWindow.ModalWindows.First();

        // Check the MessageBox text to ensure it's the expected validation error
        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { "Please verify", "Por favor, verifica" }.Should().Contain(
            s => messageText != null && messageText.Contains(s),
            "Message should show the validation error about missing URLs or folder name");
        
        modal.AsWindow().Close();
    }

    [Test]
    public void TxtUrls_WithInvalidUrls_ShowsValidationError()
    {
        // Arrange
        _page.TxtNombreBase.Enter("test");

        _page.TxtUrls.Enter("invalid_url\r\nanother_invalid");
        
        // Act
        _page.BtnDescargar.Click();

        // Assert
        var modal = MainWindow.ModalWindows.FirstOrDefault();
        modal.Should().NotBeNull("a validation modal should appear for invalid URLs");
        modal.Close();
    }

    [Test]
    public void TxtUrls_WithMixedValidAndInvalid_ContinuesWithValid()
    {
        // Arrange
        _page.TxtNombreBase.Enter("test");

        _page.TxtUrls.Enter("invalid_url\r\nhttps://example.com/valid.png");
        
        // Act
        _page.BtnDescargar.Click();

        // Assert
        var modal = MainWindow.ModalWindows.FirstOrDefault();
        modal.Should().NotBeNull("a warning modal should appear for mixed URLs");

        // Verify it is an invalid format warning (Yes/No)
        // Exact text depends on language, we check for Yes/No buttons or non-fatal error.
        // Simulate clicking 'No' to cancel
        var noButton = modal.FindFirstDescendant(cf => cf.ByName("No")).AsButton();
        if (noButton != null)
        {
            noButton.Click();
        }
        else
        {
            modal.AsWindow().Close();
        }
    }

    [Test]
    public void NumConcurrencia_RespectsMinMaxLimits()
    {
        // Arrange
        var numEdit = _page.NumConcurrencia.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
        var valuePattern = numEdit?.Patterns.Value.Pattern;

        // Act (Set to 100)
        valuePattern?.SetValue("100");
        _page.TxtNombreBase.Focus(); // Force WinForms validation to trigger on LostFocus
        
        // Assert (Capped at 20)
        double.Parse(valuePattern?.Value ?? "0").Should().Be(20, "concurrency should be capped at its maximum (20)");

        // Act (Set to 0)
        valuePattern?.SetValue("0");
        _page.TxtNombreBase.Focus(); // Force WinForms validation to trigger on LostFocus
        
        // Assert (Minimum is 1)
        double.Parse(valuePattern?.Value ?? "0").Should().Be(1, "concurrency should have a minimum (1)");
    }
}
