using System.Diagnostics;

namespace custom_image_downloader;

public partial class BulkImageDownloader : Form
{
    private static readonly HttpClient clienteHttp = new HttpClient();

    private CancellationTokenSource? cts;

    private bool estaPausado = false;

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

        string rutaPadre = txtCarpeta.Text.Trim();

        string nombreSubcarpeta = txtNombreBase.Text.Trim();

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
            MessageBox.Show($"Error creating folder '{nombreSubcarpeta}' in path '{rutaPadre}':\n{ex.Message}");
            return;
        }

        // Prepare UI
        btnDescargar.Enabled = false;
        btnCancelar.Enabled = true;
        btnPausar.Enabled = true;
        estaPausado = false;
        btnPausar.Text = "Pause";
        btnPausar.Image = Properties.Resources.pause;
        btnPausar.Padding = new Padding(20, 0, 0, 0);

        txtUrls.Enabled = false;
        txtCarpeta.Enabled = false;
        btnSeleccionarCarpeta.Enabled = false;
        txtNombreBase.Enabled = false;
        numConcurrencia.Enabled = false;

        int totalUrls = urls.Length;
        pbProgreso.Maximum = totalUrls;
        pbProgreso.Value = 0;

        int descargasExitosas = 0;
        int descargasFallidas = 0;
        bool fueCancelado = false;

        cts = new CancellationTokenSource();

        await EscribirLogAsync($"--- DOWNLOADS START IN FOLDER: [{nombreSubcarpeta}] ({totalUrls} URLs) ---");

        // Set the concurrent downloads limit
        int maxDescargasSimultaneas = (int)numConcurrencia.Value;
        using SemaphoreSlim semaforo = new SemaphoreSlim(maxDescargasSimultaneas);

        // List to store all tasks that we will launch
        List<Task> tareas = new List<Task>();

        // Safe system to update the Graphical Interface (UI) from multiple tasks
        int archivosProcesados = 0;
        var progresoUI = new Progress<int>(procesados =>
        {
            pbProgreso.Value = procesados;

            if (estaPausado)
            {
                lblEstado.Text = $"Paused (Completed {procesados} of {totalUrls})...";
            }
            else
            {
                lblEstado.Text = $"Processing {procesados} of {totalUrls}...";
            }
        });

        // Launch all tasks (the semaphore will pause them if there are more than 5)
        for (int i = 0; i < totalUrls; i++)
        {
            string urlActual = urls[i].Trim();
            int indiceArchivo = i + 1; 

            // Create an asynchronous task for each URL and add it to the list
            tareas.Add(Task.Run(async () =>
            {
                // Wait for turn if there are already downloads in progress (more than the limit defined by the user)
                await semaforo.WaitAsync(cts.Token);

                try
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        fueCancelado = true;
                        return;
                    }

                    while (estaPausado)
                    {
                        // While paused, the task "sleeps" half a second and checks again.
                        // By passing the cts.Token, if the user clicks Cancel while paused,
                        // Task.Delay will throw immediately and cancel everything cleanly.
                        await Task.Delay(500, cts.Token);
                    }

                    // Just in case they clicked cancel in the microsecond it resumed
                    cts.Token.ThrowIfCancellationRequested();

                    Uri uri = new(urlActual);
                    string nombreArchivoOriginal = Path.GetFileName(uri.LocalPath);
                    if (string.IsNullOrWhiteSpace(nombreArchivoOriginal))
                    {
                        nombreArchivoOriginal = $"downloaded_file_{DateTime.Now.Ticks}.bin";
                    }

                    string extension = Path.GetExtension(nombreArchivoOriginal);
                    if (string.IsNullOrEmpty(extension)) extension = ".bin";
                    string nombreFinal = $"{nombreSubcarpeta}_{indiceArchivo.ToString("D3")}{extension}";

                    string rutaCompletaArchivo = Path.Combine(rutaCompletaSubcarpeta, nombreFinal);

                    // Concurrent download and write
                    byte[] datosArchivo = await clienteHttp.GetByteArrayAsync(urlActual, cts.Token);
                    await File.WriteAllBytesAsync(rutaCompletaArchivo, datosArchivo, cts.Token);

                    Interlocked.Increment(ref descargasExitosas);
                    await EscribirLogAsync($"SUCCESS: [{nombreFinal}] downloaded.");
                }
                catch (OperationCanceledException)
                {
                    fueCancelado = true;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref descargasFallidas);
                    await EscribirLogAsync($"ERROR URL: {urlActual}. Reason: {ex.Message}");
                }
                finally
                {
                    // Release the space for the next download to enter
                    semaforo.Release();

                    // Safely increment processed files and update UI
                    int totalCompletados = Interlocked.Increment(ref archivosProcesados);
                    if (!fueCancelado)
                    {
                        // Report progress back to the graphical interface safely
                        ((IProgress<int>)progresoUI).Report(totalCompletados);
                    }
                }
            }, cts.Token));
        }

        try
        {
            // Wait for ALL tasks in the list to complete
            await Task.WhenAll(tareas);
        }
        catch (OperationCanceledException)
        {
            fueCancelado = true;
        }

        // Finish and restore UI
        btnDescargar.Enabled = true;
        btnCancelar.Enabled = false;
        btnPausar.Enabled = false;

        txtUrls.Enabled = true;
        txtCarpeta.Enabled = true;
        btnSeleccionarCarpeta.Enabled = true;
        txtNombreBase.Enabled = true;
        numConcurrencia.Enabled = true;

        cts.Dispose();
        cts = null;

        if (fueCancelado)
        {
            pbProgreso.Value = 0;

            lblEstado.Text = "Cancelling and cleaning files...";

            bool carpetaEliminada = false;

            // Try to delete the folder and all its contents
            try
            {
                if (Directory.Exists(rutaCompletaSubcarpeta))
                {
                    Directory.Delete(rutaCompletaSubcarpeta, true);
                    carpetaEliminada = true;
                }
            }
            catch (Exception ex)
            {
                await EscribirLogAsync($"WARNING: Could not delete cancelled folder. Reason: {ex.Message}");
            }

            // Build final message depending on whether we could delete or not
            string msjCancelado = "The download was cancelled.\n\n";

            if (carpetaEliminada)
            {
                msjCancelado += $"The folder '{nombreSubcarpeta}' and all partial files have been deleted successfully.";
                await EscribirLogAsync($"--- END (CANCELLED). Partial files were deleted safely. ---");
            }
            else
            {
                msjCancelado += $"We could not delete the folder '{nombreSubcarpeta}' automatically. You may need to delete it manually.";
                await EscribirLogAsync($"--- END (CANCELLED). Error cleaning up partial folder. ---");
            }

            lblEstado.Text = "Ready";
            MessageBox.Show(msjCancelado, "Process Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            if (descargasExitosas == 0)
            {
                lblEstado.Text = "Download failed!";
                string msjFallo = $"Could not download any files from the list.\nTotal failures: {descargasFallidas}\n\nCheck the log file to see the reason for the errors.";

                try
                {
                    if (Directory.Exists(rutaCompletaSubcarpeta))
                    {
                        Directory.Delete(rutaCompletaSubcarpeta, true);
                    }
                }
                catch {  }

                await EscribirLogAsync($"--- END OF DOWNLOADS. Total failure: {descargasFallidas} errors. Empty folder deleted. ---");

                MessageBox.Show(msjFallo, "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else
            {
                string mensajeFinal = $"Download completed!\n\n" +
                                      $"• Successful files: {descargasExitosas}\n" +
                                      $"• Failed: {descargasFallidas}\n\n" +
                                      $"Do you want to open the folder '{nombreSubcarpeta}' to view the images now?";
                lblEstado.Text = "Ready";

                await EscribirLogAsync($"--- END OF DOWNLOADS. Success: {descargasExitosas} | Failed: {descargasFallidas} | Folder: {nombreSubcarpeta} ---");

                DialogResult respuesta = MessageBox.Show(mensajeFinal, "Results", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (respuesta == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", rutaCompletaSubcarpeta);
                }
            }
        }
    }

    private async Task EscribirLogAsync(string mensaje)
    {
        try
        {
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string hora = DateTime.Now.ToString("HH:mm:ss");

            string carpetaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            // Create the "logs" folder if it doesn't exist yet
            if (!Directory.Exists(carpetaLogs))
            {
                Directory.CreateDirectory(carpetaLogs);
            }

            string rutaLog = Path.Combine(carpetaLogs, $"log_{fecha}.txt");

            string lineaLog = $"[{hora}] {mensaje}{Environment.NewLine}";

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

    private void btnPausar_Click(object sender, EventArgs e)
    {
        estaPausado = !estaPausado;

        if (estaPausado)
        {
            btnPausar.Text = "Resume";
            btnPausar.Image = Properties.Resources.resume;
            btnPausar.Padding = new Padding(18, 0, 0, 0);

            lblEstado.Text = "Paused (waiting for current downloads to finish)...";
        }
        else
        {
            btnPausar.Text = "Pause";
            btnPausar.Image = Properties.Resources.pause;
            btnPausar.Padding = new Padding(20, 0, 0, 0);

            lblEstado.Text = "Download resumed...";
        }
    }
}
