using custom_image_downloader.Models;
using custom_image_downloader.Resources;
using custom_image_downloader.Services;
using System.Reflection;

namespace custom_image_downloader;

public partial class BulkImageDownloaderForm : Form
{
    private readonly IDownloadManager _gestor;
    private readonly ILogger _logger;
    private readonly IUrlValidator _validador;

    public BulkImageDownloaderForm(IDownloadManager downloadManager, ILogger logger, IUrlValidator validador)
    {
        InitializeComponent();
        _gestor = downloadManager;
        _logger = logger;
        _validador = validador;

        AplicarTextosUi();

        txtUrls.Enabled = true;
        txtCarpeta.Enabled = true;
        txtNombreBase.Enabled = true;
        btnSeleccionarCarpeta.Enabled = true;
        btnDescargar.Enabled = true;
        btnLimpiar.Enabled = true;

        btnCancelar.Enabled = false;

        string? testEnvDir = Environment.GetEnvironmentVariable("DOWNLOAD_OUTPUT_DIR");
        if (!string.IsNullOrEmpty(testEnvDir))
        {
            txtCarpeta.Text = testEnvDir;
        }
    }

    private void AplicarTextosUi()
    {
        string? versionInfo = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        string titleVersion = string.IsNullOrWhiteSpace(versionInfo) ? "" : $" (v{versionInfo})";
        Text = $"{Strings.UI_Form_Text}{titleVersion}";
        txtUrls.PlaceholderText = Strings.UI_txtUrls_PlaceholderText;
        txtCarpeta.PlaceholderText = Strings.UI_txtCarpeta_PlaceholderText;
        btnSeleccionarCarpeta.Text = Strings.UI_btnSeleccionarCarpeta_Text;
        txtNombreBase.PlaceholderText = Strings.UI_txtNombreBase_PlaceholderText;
        btnDescargar.Text = Strings.UI_btnDescargar_Text;
        lblEstado.Text = Strings.UI_lblEstado_Text;
        btnCancelar.Text = Strings.UI_btnCancelar_Text;
        btnLimpiar.Text = Strings.UI_btnLimpiar_Text;
        label1.Text = Strings.UI_label1_Text;
        btnPausar.Text = Strings.PauseButtonText;
    }

    // Event to select the destination folder
    private void btnSeleccionarCarpeta_Click(object sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new FolderBrowserDialog();

        dialog.Description = Strings.FolderBrowserDescription;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtCarpeta.Text = dialog.SelectedPath;
        }
    }

    // Asynchronous event to download files
    private async void btnDescargar_Click(object sender, EventArgs e)
    {
        string[] urls =
        [
            .. txtUrls.Text
                .Trim()
                .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .Where(url => !string.IsNullOrWhiteSpace(url))
        ];

        string rutaPadre = txtCarpeta.Text;

        // Field validations
        if (urls.Length == 0 || string.IsNullOrWhiteSpace(rutaPadre) || !Directory.Exists(rutaPadre))
        {
            MessageBox.Show(this, Strings.ValidationErrorMessage);
            return;
        }

        // --- URL FORMAT VALIDATION ---
        var (urlsValidas, urlsInvalidas) = _validador.Validate(urls);

        if (urlsValidas.Length == 0)
        {
            MessageBox.Show(this, Strings.Validation_NoValidUrls, Strings.Validation_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (urlsInvalidas.Length > 0)
        {
            string mensaje = string.Format(Strings.Validation_InvalidFormatWarning,
                urlsInvalidas.Length, urlsValidas.Length);

            if (MessageBox.Show(this, mensaje, Strings.Validation_Title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return;
        }

        string nombreSubcarpeta =
            string.IsNullOrWhiteSpace(txtNombreBase.Text) ? "Downloads" : txtNombreBase.Text.Trim();

        // Sanitize the subfolder name by replacing invalid characters with underscores
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            nombreSubcarpeta = nombreSubcarpeta.Replace(c, '_');
        }

        // Combine the parent path with the subfolder name to get the final full path
        string rutaCompletaSubcarpeta = Path.Combine(rutaPadre, nombreSubcarpeta);

        try
        {
            rutaCompletaSubcarpeta = Directory.CreateDirectory(rutaCompletaSubcarpeta).FullName;
        }
        catch (Exception ex)
        {
            await _logger.EscribirErrorAsync($"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}'", ex);
            MessageBox.Show(this, string.Format(Strings.ErrorCreatingFolderMessage, nombreSubcarpeta, rutaPadre));
            return;
        }

        // Prepare UI
        BloquearUi(true);
        pbProgreso.Maximum = urlsValidas.Length;
        pbProgreso.Value = 0;

        int concurrencia = (int)numConcurrencia.Value;

        var progresoBarra = new Progress<int>(procesados => pbProgreso.Value = procesados);
        var progresoTexto = new Progress<DownloadProgressInfo>(info =>
        {
            lblEstado.Text = info.State switch
            {
                DownloadState.Starting => string.Format(Strings.Status_Starting, info.Total),
                DownloadState.Processing => string.Format(Strings.Status_Processing, info.Processed, info.Total),
                DownloadState.Paused => string.Format(Strings.Status_Paused, info.Processed, info.Total),
                _ => lblEstado.Text
            };
        });

        try
        {
            DownloadResult resultado = await _gestor.IniciarDescargaAsync(
                urlsValidas,
                rutaCompletaSubcarpeta,
                nombreSubcarpeta,
                concurrencia,
                progresoBarra,
                progresoTexto
            );

            MostrarMensajeFinal(resultado);
        }
        catch (Exception ex)
        {
            await _logger.EscribirErrorAsync("Unexpected error during download", ex);
            MessageBox.Show(this, Strings.UnexpectedErrorMessage, Strings.FatalErrorTitle, MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            BloquearUi(false);
        }
    }

    private void txtCarpeta_Click(object sender, EventArgs e)
    {
        btnSeleccionarCarpeta_Click(sender, e);
    }

    private void btnPausar_Click(object sender, EventArgs e)
    {
        _gestor.AlternarPausa();

        btnPausar.Image = _gestor.EstaPausado ? Properties.Resources.resume : Properties.Resources.pause;
        btnPausar.Padding = _gestor.EstaPausado ? new Padding(18, 0, 0, 0) : new Padding(20, 0, 0, 0);
        btnPausar.Text = _gestor.EstaPausado ? Strings.ResumeButtonText : Strings.PauseButtonText;
    }

    private void btnCancelar_Click(object sender, EventArgs e)
    {
        _gestor.Cancelar(); // We tell the brain to abort everything
        btnCancelar.Enabled = false;
        btnPausar.Enabled = false;
    }

    private void BloquearUi(bool bloqueado)
    {
        btnDescargar.Enabled = !bloqueado;
        btnCancelar.Enabled = bloqueado;
        btnPausar.Enabled = bloqueado;
        btnPausar.Text = Strings.PauseButtonText;
        btnPausar.Image = Properties.Resources.pause;
        btnPausar.Padding = new Padding(20, 0, 0, 0);

        txtUrls.Enabled = !bloqueado;
        txtCarpeta.Enabled = !bloqueado;
        txtNombreBase.Enabled = !bloqueado;
        numConcurrencia.Enabled = !bloqueado;
        btnLimpiar.Enabled = !bloqueado;
    }

    // Helper method to read the DownloadResult and show the MessageBox
    private void MostrarMensajeFinal(DownloadResult resultado)
    {
        if (resultado.FueCancelado)
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            // Delete ALL files that were opened for writing in this session (complete + partial)
            foreach (string ruta in resultado.RutasEnProgreso)
            {
                try
                {
                    if (File.Exists(ruta)) File.Delete(ruta);
                }
                catch
                {
                    /* ignored */
                }
            }

            MessageBox.Show(this, Strings.DownloadCancelledMessage, Strings.CancelledTitle, MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else if (resultado.FracasoTotal)
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            MessageBox.Show(this, string.Format(Strings.DownloadFailedMessage, resultado.Fallidas), Strings.ErrorTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            string msj = string.Format(Strings.DownloadCompletedMessage,
                resultado.Exitosas, resultado.Fallidas, resultado.Omitidas);

            if (MessageBox.Show(this, msj, Strings.SuccessTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", resultado.RutaFinal);
            }
        }

        pbProgreso.Value = 0;
    }

    private void btnLimpiar_Click(object sender, EventArgs e)
    {
        txtUrls.Clear();
        txtCarpeta.Clear();
        txtNombreBase.Clear();
    }

    private void txtNombreBase_KeyPress(object sender, KeyPressEventArgs e)
    {
        // Get the official list of characters prohibited by Windows
        char[] caracteresInvalidos = Path.GetInvalidFileNameChars();

        // We block invalid characters, but we must allow control characters (like backspace)
        if (caracteresInvalidos.Contains(e.KeyChar) && !char.IsControl(e.KeyChar))
        {
            e.Handled = true;
        }
    }
}
