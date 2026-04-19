using custom_image_downloader.Models;
using custom_image_downloader.Resources;
using custom_image_downloader.Services;

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

        btnCancelar.Enabled = false;
    }

    private void AplicarTextosUi()
    {
        Text = Strings.UI_Form_Text;
        txtUrls.PlaceholderText = Strings.UI_txtUrls_PlaceholderText;
        txtCarpeta.PlaceholderText = Strings.UI_txtCarpeta_PlaceholderText;
        btnSeleccionarCarpeta.Text = Strings.UI_btnSeleccionarCarpeta_Text;
        txtNombreBase.PlaceholderText = Strings.UI_txtNombreBase_PlaceholderText;
        btnDescargar.Text = Strings.UI_btnDescargar_Text;
        lblEstado.Text = Strings.UI_lblEstado_Text;
        btnCancelar.Text = Strings.UI_btnCancelar_Text;
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
                .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .Where(url => !string.IsNullOrWhiteSpace(url))
        ];

        string rutaPadre = txtCarpeta.Text;
        string nombreSubcarpeta = txtNombreBase.Text;

        // Basic field validations
        if (urls.Length == 0 || string.IsNullOrWhiteSpace(rutaPadre) || !Directory.Exists(rutaPadre) ||
            string.IsNullOrWhiteSpace(nombreSubcarpeta))
        {
            MessageBox.Show(Strings.ValidationErrorMessage);
            return;
        }

        // --- URL FORMAT VALIDATION ---
        var (urlsValidas, urlsInvalidas) = _validador.Validate(urls);

        if (urlsValidas.Length == 0)
        {
            MessageBox.Show(Strings.Validation_NoValidUrls, Strings.Validation_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (urlsInvalidas.Length > 0)
        {
            string mensaje = string.Format(Strings.Validation_InvalidFormatWarning,
                urlsInvalidas.Length, urlsValidas.Length);

            if (MessageBox.Show(mensaje, Strings.Validation_Title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return;
        }

        // Combine the parent path with the subfolder name to get the final full path
        string rutaCompletaSubcarpeta = Path.Combine(rutaPadre, nombreSubcarpeta);

        try
        {
            Directory.CreateDirectory(rutaCompletaSubcarpeta);
        }
        catch (Exception ex)
        {
            await _logger.EscribirAsync(
                $"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}': {ex.StackTrace}");
            MessageBox.Show(string.Format(Strings.ErrorCreatingFolderMessage, nombreSubcarpeta, rutaPadre));
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
            await _logger.EscribirAsync($"Unexpected error during download: {ex.StackTrace}");
            MessageBox.Show(Strings.UnexpectedErrorMessage, Strings.FatalErrorTitle, MessageBoxButtons.OK,
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
    }

    // Helper method to read the DownloadResult and show the MessageBox
    private void MostrarMensajeFinal(DownloadResult resultado)
    {
        if (resultado.FueCancelado)
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            // Delete only files downloaded in this session, not the whole folder
            foreach (string ruta in resultado.RutasDescargadas)
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }

            MessageBox.Show(Strings.DownloadCancelledMessage, Strings.CancelledTitle, MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else if (resultado.FracasoTotal)
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            MessageBox.Show(string.Format(Strings.DownloadFailedMessage, resultado.Fallidas), Strings.ErrorTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            lblEstado.Text = Strings.UI_lblEstado_Text;

            string msj = string.Format(Strings.DownloadCompletedMessage,
                resultado.Exitosas, resultado.Fallidas, resultado.Omitidas);

            if (MessageBox.Show(msj, Strings.SuccessTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", resultado.RutaFinal);
            }
        }

        pbProgreso.Value = 0;
    }
}
