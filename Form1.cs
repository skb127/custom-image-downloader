using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace custom_image_downloader;

public partial class BulkImageDownloader : Form
{
    private static readonly HttpClient clienteHttp = new HttpClient();

    private CancellationTokenSource? cts;

    public BulkImageDownloader()
    {
        InitializeComponent();

        txtUrls.Enabled = true;
        txtCarpeta.Enabled = true;
        txtNombreBase.Enabled = true;
        btnSeleccionarCarpeta.Enabled = true;
        btnDescargar.Enabled = true;

        btnCancelar.Enabled = false;
    }

    private void textBox1_TextChanged(object sender, EventArgs e)
    {

    }

    // Event to select the destination folder
    private void btnSeleccionarCarpeta_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "Select the folder where files will be saved";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtCarpeta.Text = dialog.SelectedPath;
            }
        }
    }

    // Asynchronous event to download files
    private async void btnDescargar_Click(object sender, EventArgs e)
    {
        string[] urls = txtUrls.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        // --- REDEFINE VARIABLES BASED ON NEW REQUIREMENTS ---
        // txtCarpeta is now the PARENT PATH (e.g.: C:\Downloads)
        string rutaPadre = txtCarpeta.Text.Trim();
        // txtNombreBase is now the SUBFOLDER NAME (e.g.: cm91)
        string nombreSubcarpeta = txtNombreBase.Text.Trim();

        // Basic field validations
        if (urls.Length == 0 || string.IsNullOrWhiteSpace(rutaPadre) || !Directory.Exists(rutaPadre) || string.IsNullOrWhiteSpace(nombreSubcarpeta))
        {
            MessageBox.Show("Please verify:\n1. That there is at least one URL.\n2. That the Parent Path exists.\n3. That you provided a name for the subfolder.");
            return;
        }

        // --- MAGIC #1: AUTOMATIC FOLDER CREATION ---
        // Combine the parent path with the subfolder name to get the final full path
        string rutaCompletaSubcarpeta = Path.Combine(rutaPadre, nombreSubcarpeta);

        try
        {
            // CreateDirectory ensures the folder exists. If it already exists, it does nothing.
            Directory.CreateDirectory(rutaCompletaSubcarpeta);
        }
        catch (Exception ex)
        {
            // For example: no write permissions on the parent path.
            MessageBox.Show($"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}':\n{ex.Message}");
            return;
        }

        // Prepare UI
        btnDescargar.Enabled = false;
        btnCancelar.Enabled = true;

        int totalUrls = urls.Length;
        pbProgreso.Maximum = totalUrls;
        pbProgreso.Value = 0;

        int descargasExitosas = 0;
        int descargasFallidas = 0;
        bool fueCancelado = false;

        // NEW: initialize cancellation token
        cts = new CancellationTokenSource();

        await EscribirLogAsync($"--- DOWNLOADS START IN FOLDER: [{nombreSubcarpeta}] ({totalUrls} URLs) ---");

        for (int i = 0; i < totalUrls; i++)
        {
            if (cts.Token.IsCancellationRequested)
            {
                fueCancelado = true;
                break;
            }

            string urlActual = urls[i].Trim();
            lblEstado.Text = $"Processing {i + 1} of {totalUrls}...";

            try
            {
                // Use Uri class to parse the URL safely and avoid issues with query parameters (?query=...)
                Uri uri = new Uri(urlActual);
                // Extract only the file name from the Uri's LocalPath. e.g.: 050.jpg
                string nombreArchivoOriginal = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrWhiteSpace(nombreArchivoOriginal))
                {
                    // Safety fallback in case the URL is odd (e.g. ends with /)
                    nombreArchivoOriginal = $"downloaded_file_{DateTime.Now.Ticks}.bin";
                }

                // The full path where we will save the file will be: Parent/Subfolder/OriginalName.ext
                string rutaCompletaArchivo = Path.Combine(rutaCompletaSubcarpeta, nombreArchivoOriginal);

                // --- MAGIC #3 (REPLACE): WriteAllBytesAsync does it by default. No extra logic needed. ---
                // Simply download and overwrite if it already exists.
                byte[] datosArchivo = await clienteHttp.GetByteArrayAsync(urlActual, cts.Token);
                await File.WriteAllBytesAsync(rutaCompletaArchivo, datosArchivo, cts.Token);

                descargasExitosas++;
                await EscribirLogAsync($"SUCCESS: [{nombreArchivoOriginal}] saved in subfolder [{nombreSubcarpeta}]");
            }
            catch (OperationCanceledException)
            {
                fueCancelado = true;
                await EscribirLogAsync($"CANCELLED: Download of {urlActual} was interrupted by the user.");
                break;
            }
            catch (UriFormatException ex)
            {
                descargasFallidas++;
                await EscribirLogAsync($"INVALID URL ERROR: {urlActual}. Reason: {ex.Message}");
            }
            catch (Exception ex)
            {
                descargasFallidas++;
                await EscribirLogAsync($"ERROR: Failed to save file from {urlActual}. Reason: {ex.Message}");
            }
            finally
            {
                if (!fueCancelado)
                {
                    pbProgreso.Value = i + 1;
                }
            }
        }

        // 5. Finish and restore UI
        btnDescargar.Enabled = true;
        btnCancelar.Enabled = false;

        cts.Dispose();
        cts = null;

        if (fueCancelado)
        {
            string msjCancelado = $"Process cancelled.\nSuccessful: {descargasExitosas}\nFailed: {descargasFallidas}\nIn folder: {nombreSubcarpeta}";
            lblEstado.Text = "Download cancelled by user.";
            await EscribirLogAsync($"--- END (CANCELLED). Successful: {descargasExitosas} | Failed: {descargasFallidas} ---");
            MessageBox.Show(msjCancelado, "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            string mensajeFinal = $"Download completed.\nSubfolder '{nombreSubcarpeta}' created successfully.\nSuccessful files: {descargasExitosas}\nFailed: {descargasFallidas}\n\nExisting files have been replaced.";
            lblEstado.Text = "Process complete!";
            await EscribirLogAsync($"--- END OF DOWNLOADS. Success: {descargasExitosas} | Failed: {descargasFallidas} | Folder: {nombreSubcarpeta} ---");
            MessageBox.Show(mensajeFinal, "Results", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Process.Start("explorer.exe", rutaCompletaSubcarpeta);
        }
    }

    // ==========================================
    // NEW METHOD: Logging system
    // ==========================================
    private async Task EscribirLogAsync(string mensaje)
    {
        try
        {
            // 1. Get current date and time
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string hora = DateTime.Now.ToString("HH:mm:ss");

            // 2. Define the path for the "logs" subfolder (next to the .exe)
            string carpetaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            // 3. Create the "logs" folder if it doesn't exist yet
            if (!Directory.Exists(carpetaLogs))
            {
                Directory.CreateDirectory(carpetaLogs);
            }

            // 4. Define the full path of the log file inside the logs folder
            string rutaLog = Path.Combine(carpetaLogs, $"log_{fecha}.txt");

            // 5. Format the line to be written
            string lineaLog = $"[{hora}] {mensaje}{Environment.NewLine}";

            // 6. Append the text to the file
            await File.AppendAllTextAsync(rutaLog, lineaLog);
        }
        catch
        {
            // If it fails due to lack of permissions, ignore it so the app doesn't crash
        }
    }

    // Event for the Cancel button
    private void btnCancelar_Click(object sender, EventArgs e)
    {
        if (cts != null)
        {
            cts.Cancel(); // Send the cancellation signal
            btnCancelar.Enabled = false; // Disable the button to avoid multiple clicks
            lblEstado.Text = "Cancelling... waiting for current file to finish.";
        }
    }

    private void txtCarpeta_Click(object sender, EventArgs e)
    {
        btnSeleccionarCarpeta_Click(sender, e);
    }
}
