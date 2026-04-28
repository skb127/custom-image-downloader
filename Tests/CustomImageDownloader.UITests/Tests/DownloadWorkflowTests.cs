using CustomImageDownloader.UITests.Infrastructure;
using CustomImageDownloader.UITests.Pages;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FluentAssertions;
using NUnit.Framework;

namespace CustomImageDownloader.UITests.Tests;

[TestFixture]
public class DownloadWorkflowTests : TestBase
{
    private MainFormPage _page = null!;

    [SetUp]
    public void SetUp() => _page = new MainFormPage(MainWindow);

    [Test]
    public void Download_SingleValidImage_Succeeds()
    {
        // Arrange
        string testFile = "image_1.png";
        string url = $"{ServerBaseUrl}/{testFile}";

        _page.TxtUrls.Enter(url);
        _page.TxtNombreBase.Enter("single_test");

        // Act
        _page.BtnDescargar.Click();

        // Assert
        // Wait until completion modal appears with a retry mechanism to handle potential delays in processing
        var retryResult = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage:
            "Download completion modal should appear within the expected time"
        );
        
        retryResult.Result.Should()
            .NotBeNullOrEmpty("Download completion modal should be present");

        var modal = retryResult.Result.First();

        new[] { "éxito", "success" }.Should().Contain(s => modal.Name.ToLower().Contains(s),
            "Completion message should indicate success");

        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { "Successful: 1", "Exitosas: 1" }.Should().Contain(s => messageText != null && messageText.Contains(s), 
            "Message should indicate 1 successful download");

        // Close modal
        var btnNo = modal.FindFirstDescendant(cf => cf.ByName("No")).AsButton();
        btnNo?.Invoke();
        if (btnNo == null) modal.Close();

        // Verify file on disk
        var expectedPath = Path.Combine(TestOutputDir!, "single_test", testFile);
        File.Exists(expectedPath).Should().BeTrue($"File {testFile} should have been downloaded");
    }

    [Test]
    public void Download_MultipleValidImages_AllSucceed()
    {
        // Arrange
        string[] testFiles = ["image_1.png", "image_2.png", "image_3.jpg"];
        string urls = string.Join("\r\n", testFiles.Select(f => $"{ServerBaseUrl}/{f}"));

        _page.TxtUrls.Enter(urls);
        _page.TxtNombreBase.Enter("multi_test");

        // Act
        _page.BtnDescargar.Click();

        // Assert
        var retryResult = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "Download completion modal should appear within the expected time"
        );

        retryResult.Result.Should()
            .NotBeNullOrEmpty("Download completion modal should be present");

        var modal = retryResult.Result.First();

        new[] { "éxito", "success" }.Should().Contain(s => modal.Name.ToLower().Contains(s),
            "Completion message should indicate success");

        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { $"Successful: {testFiles.Length}", $"Exitosas: {testFiles.Length}" }.Should().Contain(s => messageText != null && messageText.Contains(s), 
            "Message should indicate the correct number of successful downloads");

        var btnNo = modal.FindFirstDescendant(cf => cf.ByName("No")).AsButton();
        btnNo?.Invoke();
        if (btnNo == null) modal.Close();

        foreach (var file in testFiles)
        {
            var expectedPath = Path.Combine(TestOutputDir!, "multi_test", file);
            File.Exists(expectedPath).Should().BeTrue($"File {file} should have been downloaded");
        }
    }

    [Test]
    public void Download_DisallowedExtension_IsSkipped()
    {
        // Arrange
        // .exe is not in the allowed extensions list in appsettings.json
        string[] urls = [$"{ServerBaseUrl}/not_allowed.exe"];

        _page.TxtUrls.Enter(String.Join("\r\n", urls));
        _page.TxtNombreBase.Enter("skipped_test");

        // Act
        _page.BtnDescargar.Click();

        // Assert
        // The download manager will process it, skip it due to extension, and show the Success modal
        var retryResult = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "Completion modal should appear within the expected time even if file is skipped"
        );
        
        retryResult.Result.Should()
            .NotBeNullOrEmpty("Completion modal should be present even if file is skipped");
    
        var modal = retryResult.Result.First();
        
        // Validate the title supporting both Spanish (Éxito) and English (Success)
        new[] { "éxito", "success" }.Should().Contain(s => modal.Name.ToLower().Contains(s),
            "Modal title should indicate success even if the file was skipped");

        // Validate the modal text contains skipped count
        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { $"Skipped (unsupported type): {urls.Length}", $"Omitidas (tipo no admitido): {urls.Length}" }.Should().Contain(s => messageText != null && messageText.Contains(s), 
            "Message should indicate the correct number of files skipped");

        var btnNo = modal.FindFirstDescendant(cf => cf.ByName("No")).AsButton();
        btnNo?.Invoke();
        if (btnNo == null) modal.Close();

        var expectedFile = Path.Combine(TestOutputDir!, "skipped_test", "not_allowed.exe");
        File.Exists(expectedFile).Should().BeFalse("Unsupported file extensions should not be downloaded to disk");
    }

    [Test]
    public void BtnCancelar_EnabledDuringDownload_DisabledAfter()
    {
        // Arrange
        // Use 15 files and set concurrency to 1 to ensure we have time to check the UI state
        var urlsList = Enumerable.Range(1, 10).Concat(Enumerable.Range(1, 5)).Select(i => $"{ServerBaseUrl}/large/large_{i:D2}.png");
        var urls = string.Join("\r\n", urlsList);

        _page.TxtUrls.Patterns.Value.Pattern.SetValue(urls);
        _page.TxtNombreBase.Enter("cancel_state_test");
        var numEdit = _page.NumConcurrencia.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
        numEdit?.Patterns.Value.Pattern.SetValue("1");

        _page.BtnCancelar.IsEnabled.Should().BeFalse("Cancel button should be disabled before starting");

        // Act (Start Download)
        _page.BtnDescargar.Click();

        // Wait until progress bar moves, indicating download has started
        _ = Retry.WhileFalse(
            () => _page.PbProgreso.Value > 2,
            timeout: TimeSpan.FromSeconds(6),
            throwOnTimeout: true,
            timeoutMessage: "Progress bar should start moving within the expected time, indicating download has started"
        );

        // Assert (During Download)
        _page.BtnCancelar.IsEnabled.Should().BeTrue("Cancel button should be enabled during download");

        // Act (Cancel Download)
        // Let it finish or cancel
        _page.BtnCancelar.Click();

        // Assert (After Cancellation)
        var retryResultModal = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "Cancellation modal should appear within the expected time"
        );

        retryResultModal.Result.Should()
            .NotBeNullOrEmpty("Cancellation modal should be present");

        var modal = retryResultModal.Result.First();

        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { "Partial files were deleted", "ficheros parciales han sido eliminados" }.Should().Contain(s => messageText != null && messageText.Contains(s), 
            "Message should indicate partial files were deleted");

        var btnOk = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button))?.AsButton();
        btnOk?.Invoke();
        if (btnOk == null) modal.Close();

        _page.BtnCancelar.IsEnabled.Should().BeFalse("Cancel button should be disabled after cancellation");
    }

    [Test]
    public void Download_DuringProgress_PauseAndResumeWorks()
    {
        // Arrange
        var urlsList = Enumerable.Range(1, 10).Concat(Enumerable.Range(1, 5)).Select(i => $"{ServerBaseUrl}/large/large_{i:D2}.png").ToArray();
        var urls = string.Join("\r\n", urlsList);
        _page.TxtUrls.Patterns.Value.Pattern.SetValue(urls);
        _page.TxtNombreBase.Enter("pause_resume_test");
        var numEdit = _page.NumConcurrencia.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
        numEdit?.Patterns.Value.Pattern.SetValue("1");

        // Act (Start Download)
        _page.BtnDescargar.Click();

        var retryResultStart = Retry.WhileFalse(
            () => _page.PbProgreso.Value > 2,
            timeout: TimeSpan.FromSeconds(6),
            throwOnTimeout: true,
            timeoutMessage: "Progress bar should start moving within the expected time, indicating download has started"
        );

        retryResultStart.Result.Should()
            .BeTrue("Progress bar should have started moving, indicating download has started");
        
        // Act (Pause Download)
        // Pause
        _page.BtnPausar.Click();

        // Assert (Paused State)
        // Wait for the Label text to reflect "Paused"
        var retryResultLabel = Retry.WhileFalse(
            () => _page.LblEstado.Text.Contains("Pausado", StringComparison.OrdinalIgnoreCase) ||
                        _page.LblEstado.Text.Contains("Paused", StringComparison.OrdinalIgnoreCase),
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "Status label should indicate Paused within the expected time"
        );

        retryResultLabel.Result.Should()
            .BeTrue("Status label should indicate Paused/Pausado");

        // Check the Pause button text changes to "Resume" or "Reanudar"
        _page.BtnPausar.Name.Should().Match(s => s.Contains("Resume", StringComparison.OrdinalIgnoreCase) || s.Contains("Reanudar", StringComparison.OrdinalIgnoreCase),
            "Pause button text should change to indicate it can resume");

        double pausedValue = _page.PbProgreso.Value;

        // Wait 1 second and verify that the progress has not advanced
        Thread.Sleep(1500);
        _page.PbProgreso.Value.Should().Be(pausedValue, "Progress should not advance while paused");

        // Act (Resume Download)
        // Resume
        _page.BtnPausar.Click();

        // Assert (Resumed State & Completion)
        // Wait for the Label text to reflect "Processing" or "Procesando"
        var retryResultResume = Retry.WhileFalse(
            () => _page.LblEstado.Text.Contains("Procesando", StringComparison.OrdinalIgnoreCase) ||
                  _page.LblEstado.Text.Contains("Processing", StringComparison.OrdinalIgnoreCase),
            timeout: TimeSpan.FromSeconds(6),
            throwOnTimeout: true,
            timeoutMessage: "Progress bar should advance after resuming within the expected time, Status label should indicate Procesando/Processing"
        );

        retryResultResume.Result.Should()
            .BeTrue("Status label should indicate Procesando/Processing after resuming");
        
        // Check the Pause button text changes to "Pause" or "Pausar"
        _page.BtnPausar.Name.Should().Match(s => s.Contains("Pause", StringComparison.OrdinalIgnoreCase) || s.Contains("Pausar", StringComparison.OrdinalIgnoreCase),
            "Pause button text should change back to indicate it can pause");

        // Wait for it to finish
        var retryResultCompletion = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(30),
            throwOnTimeout: true,
            timeoutMessage: "Download completion modal should appear within the expected time after resuming"
        );

        retryResultCompletion.Result.Should()
            .NotBeNullOrEmpty("Download completion modal should be present after resuming");
        
        var modal = retryResultCompletion.Result.First();

        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { $"Successful: {urlsList.Length}", $"Exitosas: {urlsList.Length}" }.Should().Contain(s => messageText != null && messageText.Contains(s),
            $"Message should indicate {urlsList.Length} successful downloads");

        var btnNo = modal.FindFirstDescendant(cf => cf.ByName("No")).AsButton();
        btnNo?.Invoke();
        if (btnNo == null) modal.Close();

        var dirInfo = new DirectoryInfo(Path.Combine(TestOutputDir!, "pause_resume_test"));
        dirInfo.GetFiles().Length.Should().Be(urlsList.Length, $"All {urlsList.Length} files should have been downloaded after resuming");
    }

    [Test]
    public void Download_DuringProgress_CancelStops()
    {
        // Arrange
        var urlsList = Enumerable.Range(1, 10).Concat(Enumerable.Range(1, 5)).Select(i => $"{ServerBaseUrl}/large/large_{i:D2}.png").ToArray();
        var urls = string.Join("\r\n", urlsList);
        _page.TxtUrls.Patterns.Value.Pattern.SetValue(urls);
        _page.TxtNombreBase.Enter("cancel_stop_test");
        var numEdit = _page.NumConcurrencia.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
        numEdit?.Patterns.Value.Pattern.SetValue("1");

        // Act (Start Download)
        _page.BtnDescargar.Click();

        var retryResult = Retry.WhileFalse(
            () => _page.PbProgreso.Value > 2,
            timeout: TimeSpan.FromSeconds(6),
            throwOnTimeout: true,
            timeoutMessage: "Progress bar should start moving within the expected time, indicating download has started"
        );

        retryResult.Result.Should()
            .BeTrue("Progress bar should have started moving, indicating download has started");

        // Act (Cancel Download)
        // Cancel
        _page.BtnCancelar.Click();

        // Assert (After Cancellation)
        var retryResultCancel = Retry.WhileEmpty(
            () => MainWindow.ModalWindows,
            timeout: TimeSpan.FromSeconds(3),
            throwOnTimeout: true,
            timeoutMessage: "Cancellation modal should appear within the expected time"
        );

        retryResultCancel.Result.Should()
            .NotBeNullOrEmpty("Cancellation modal should be present");
        
        var modal = retryResultCancel.Result.First();

        new[] { "cancelad", "cancel" }.Should().Contain(s => modal.Name.ToLower().Contains(s),
            "Message should indicate cancellation");

        var messageText = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)).AsLabel()?.Text;
        new[] { "Partial files were deleted", "ficheros parciales han sido eliminados" }.Should().Contain(s => messageText != null && messageText.Contains(s), 
            "Message should indicate partial files were deleted");

        var btnOk = modal.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button))?.AsButton();
        btnOk?.Invoke();
        if (btnOk == null) modal.Close();

        // Ensure no extra files remain, wait a bit
        Thread.Sleep(1500);
        var dirInfo = new DirectoryInfo(Path.Combine(TestOutputDir!, "cancel_stop_test"));
        if (dirInfo.Exists)
        {
            dirInfo.GetFiles().Length.Should().BeLessThan(15, "Not all files should have been downloaded");
        }
    }
}
