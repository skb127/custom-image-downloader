using custom_image_downloader.Models;
using custom_image_downloader.Services;

namespace custom_image_downloader;

public partial class BulkImageDownloaderForm : Form
{
    private readonly IDownloadManager _gestor;
    private readonly ILogger _logger;
    private readonly System.ComponentModel.ComponentResourceManager _res;

    public BulkImageDownloaderForm(IDownloadManager downloadManager, ILogger logger)
    {
        InitializeComponent();
        _gestor = downloadManager;
        _logger = logger;
        _res = new System.ComponentModel.ComponentResourceManager(typeof(BulkImageDownloaderForm));

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
        Text = _res.GetString("UI_Form_Text") ?? "Bulk image downloader";
        txtUrls.PlaceholderText = _res.GetString("UI_txtUrls_PlaceholderText") ?? "URLs (one URL per line)";
        txtCarpeta.PlaceholderText = _res.GetString("UI_txtCarpeta_PlaceholderText") ?? "Destination path";
        btnSeleccionarCarpeta.Text = _res.GetString("UI_btnSeleccionarCarpeta_Text") ?? "Browse...";
        txtNombreBase.PlaceholderText = _res.GetString("UI_txtNombreBase_PlaceholderText") ?? "Subfolder";
        btnDescargar.Text = _res.GetString("UI_btnDescargar_Text") ?? "Download All";
        lblEstado.Text = _res.GetString("UI_lblEstado_Text") ?? "Ready";
        btnCancelar.Text = _res.GetString("UI_btnCancelar_Text") ?? "Cancel";
        label1.Text = _res.GetString("UI_label1_Text") ?? "Simultaneous downloads:";
        btnPausar.Text = _res.GetString("PauseButtonText") ?? "Pause";
    }

    // Event to select the destination folder
    private void btnSeleccionarCarpeta_Click(object sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new FolderBrowserDialog();

        dialog.Description = _res.GetString("FolderBrowserDescription") ?? "Select the folder where files will be saved";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtCarpeta.Text = dialog.SelectedPath;
        }
    }

    // Asynchronous event to download files
    private async void btnDescargar_Click(object sender, EventArgs e)
    {
        string[] urls = [.. txtUrls.Text
            .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
            .Where(url => !string.IsNullOrWhiteSpace(url))];

        string rutaPadre = txtCarpeta.Text;

        string nombreSubcarpeta = txtNombreBase.Text;

        // Basic field validations
        if (urls.Length == 0 || string.IsNullOrWhiteSpace(rutaPadre) || !Directory.Exists(rutaPadre) || string.IsNullOrWhiteSpace(nombreSubcarpeta))
        {
            MessageBox.Show(_res.GetString("ValidationErrorMessage"));
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
            await _logger.EscribirAsync($"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}': {ex.StackTrace}");
            MessageBox.Show(string.Format(_res.GetString("ErrorCreatingFolderMessage") ?? "Error creating folder '{0}' in path '{1}'", nombreSubcarpeta, rutaPadre));
            return;
        }

        // Prepare UI
        BloquearUi(true);
        pbProgreso.Maximum = urls.Length;
        pbProgreso.Value = 0;
        
        int concurrencia = (int)numConcurrencia.Value;

        var progresoBarra = new Progress<int>(procesados => pbProgreso.Value = procesados);
        var progresoTexto = new Progress<string>(mensaje => lblEstado.Text = mensaje);

        try
        {
            DownloadResult resultado = await _gestor.IniciarDescargaAsync(
                urls, 
                rutaCompletaSubcarpeta, 
                nombreSubcarpeta, 
                concurrencia, 
                progresoBarra, 
                progresoTexto
            );

            // 5. Process the result package returned by the Manager
            MostrarMensajeFinal(resultado);
        }
        catch (Exception ex)
        {
            await _logger.EscribirAsync($"Unexpected error during download: {ex.StackTrace}");
            MessageBox.Show(_res.GetString("UnexpectedErrorMessage"), _res.GetString("FatalErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        btnPausar.Text = _gestor.EstaPausado ? _res.GetString("ResumeButtonText") : _res.GetString("PauseButtonText");
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
        btnPausar.Text = _res.GetString("PauseButtonText");
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
            lblEstado.Text = _res.GetString("DownloadCancelledStatus");
            
            if (Directory.Exists(resultado.RutaFinal)) Directory.Delete(resultado.RutaFinal, true);
            MessageBox.Show(_res.GetString("DownloadCancelledMessage"), _res.GetString("CancelledTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (resultado.FracasoTotal)
        {
            lblEstado.Text = _res.GetString("DownloadFailedStatus");
            
            if (Directory.Exists(resultado.RutaFinal)) Directory.Delete(resultado.RutaFinal, true);
            MessageBox.Show(string.Format(_res.GetString("DownloadFailedMessage") ?? "Total failure. Nothing was downloaded.\nErrors: {0}", resultado.Fallidas), _res.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            string msj = string.Format(_res.GetString("DownloadCompletedMessage") ?? "Download completed!\n\n• Successful: {0}\n• Failures: {1}\n\nDo you want to open the folder now?", resultado.Exitosas, resultado.Fallidas);
            if (MessageBox.Show(msj, _res.GetString("SuccessTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", resultado.RutaFinal);
            }
        }
        
        pbProgreso.Value = 0;
    }
}
