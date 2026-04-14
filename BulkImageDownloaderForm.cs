using custom_image_downloader.Models;
using custom_image_downloader.Services;

namespace custom_image_downloader;

public partial class BulkImageDownloaderForm : Form
{
    private readonly IDownloadManager _gestor;
    private readonly ILogger _logger;

    public BulkImageDownloaderForm(IDownloadManager downloadManager, ILogger logger)
    {
        InitializeComponent();
        _gestor = downloadManager;
        _logger = logger;

        txtUrls.Enabled = true;
        txtCarpeta.Enabled = true;
        txtNombreBase.Enabled = true;
        btnSeleccionarCarpeta.Enabled = true;
        btnDescargar.Enabled = true;

        btnCancelar.Enabled = false;
    }

    // Event to select the destination folder
    private void btnSeleccionarCarpeta_Click(object sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new FolderBrowserDialog();

        dialog.Description = "Select the folder where files will be saved";
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
            MessageBox.Show("Please verify:\n1. That there is at least one URL.\n2. That the Parent Path exists.\n3. That you provided a name for the subfolder.");
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
            MessageBox.Show($"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}'");
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
            MessageBox.Show($"Unexpected error during download, check the logs for further details.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        btnPausar.Text = _gestor.EstaPausado ? "Resume" : "Pause";
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
        btnPausar.Text = "Pause";
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
            lblEstado.Text = "Download cancelled.";
            
            if (Directory.Exists(resultado.RutaFinal)) Directory.Delete(resultado.RutaFinal, true);
            MessageBox.Show("Download cancelled. Partial files were deleted.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (resultado.FracasoTotal)
        {
            lblEstado.Text = "Download failed!";
            
            if (Directory.Exists(resultado.RutaFinal)) Directory.Delete(resultado.RutaFinal, true);
            MessageBox.Show($"Total failure. Nothing was downloaded.\nErrors: {resultado.Fallidas}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            string msj = $"Download completed!\n\n• Successful: {resultado.Exitosas}\n• Failures: {resultado.Fallidas}\n\nDo you want to open the folder now?";
            if (MessageBox.Show(msj, "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", resultado.RutaFinal);
            }
        }
        
        pbProgreso.Value = 0;
    }
}
