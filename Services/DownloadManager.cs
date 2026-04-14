using custom_image_downloader.Models;

namespace custom_image_downloader.Services;

public class DownloadManager : IDownloadManager
{
    // The network client now lives here
    private static readonly HttpClient ClienteHttp;

    static DownloadManager()
    {
        ClienteHttp = new HttpClient();
        ClienteHttp.Timeout = TimeSpan.FromMinutes(12);
    }

    private readonly ILogger _logger;
    
    private CancellationTokenSource? _cts;
    private ManualResetEventSlim? _eventoPausa;

    public DownloadManager(ILogger logger)
    {
        _logger = logger;
    }
    
    // Public property so the Form can check if we are paused
    public bool EstaPausado => _eventoPausa is not null && !_eventoPausa.IsSet;

    // Method to pause/resume
    public void AlternarPausa()
    {
        if (_eventoPausa is null) return;

        if (_eventoPausa.IsSet)
        {
            _eventoPausa.Reset(); // Pause
        }
        else
        {
            _eventoPausa.Set(); // Resume
        }
    }

    // Method to cancel
    public void Cancelar()
    {
        if (_cts is not null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    // Download engine
    public async Task<DownloadResult> IniciarDescargaAsync(
        string[] urls, 
        string rutaSubcarpeta, 
        string nombreBase, 
        int concurrencia, 
        IProgress<int> progresoBarra, 
        IProgress<string> progresoTexto)
    {
        _cts = new CancellationTokenSource();
        _eventoPausa = new ManualResetEventSlim(true); // Initial state is "set" (not paused)

        var resultado = new DownloadResult { RutaFinal = rutaSubcarpeta };
        int totalUrls = urls.Length;
        
        int contadorExitosas = 0;
        int contadorFallidas = 0;
        int procesados = 0;

        using SemaphoreSlim semaforo = new SemaphoreSlim(concurrencia);
        List<Task> tareas = new List<Task>();

        try
        {
            progresoTexto?.Report($"Starting {totalUrls} downloads...");
            await _logger.EscribirAsync($"--- START OF DOWNLOADS ({totalUrls} URLs) IN: {rutaSubcarpeta} ---");

            for (int i = 0; i < totalUrls; i++)
            {
                string urlActual = urls[i].Trim();
                int indiceArchivo = i + 1;

                tareas.Add(Task.Run(async () =>
                {
                    await semaforo.WaitAsync(_cts.Token);

                    try
                    {
                        if (_cts.Token.IsCancellationRequested) return;
                        
                        // The task will efficiently block here if paused, without consuming CPU.
                        // It will automatically unblock if the task is canceled.
                        _eventoPausa.Wait(_cts.Token);
                        _cts.Token.ThrowIfCancellationRequested();
                        
                        // THE REAL DOWNLOAD
                        Uri uri = new Uri(urlActual);
                        string nombreArchivoOriginal = Path.GetFileName(uri.LocalPath); 
                        
                        if (string.IsNullOrWhiteSpace(nombreArchivoOriginal))
                        {
                            nombreArchivoOriginal = $"download_{DateTime.Now.Ticks}.bin";
                        }

                        string extension = Path.GetExtension(nombreArchivoOriginal);
                        if (string.IsNullOrEmpty(extension)) extension = ".bin";
                        
                        string nombreFinal = $"{nombreBase}_{indiceArchivo:D3}{extension}";
                        string rutaCompletaArchivo = Path.Combine(rutaSubcarpeta, nombreFinal);

                        // streaming to download the file. This is much more memory efficient than loading the entire file into a byte array.
                        using (var response = await ClienteHttp.GetAsync(urlActual, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                        {
                            response.EnsureSuccessStatusCode();
                            await using (Stream contentStream = await response.Content.ReadAsStreamAsync(_cts.Token))
                            await using (FileStream fileStream = new FileStream(rutaCompletaArchivo, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                            {
                                await contentStream.CopyToAsync(fileStream, _cts.Token);
                            }
                        }

                        Interlocked.Increment(ref contadorExitosas);
                        await _logger.EscribirAsync($"SUCCESS: [{nombreFinal}] downloaded from {urlActual}");
                    }
                    catch (OperationCanceledException)
                    {
                        // We ignore the individual cancellation to catch it above
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref contadorFallidas);
                        await _logger.EscribirAsync($"ERROR URL: {urlActual}. Reason: {ex}");
                    }
                    finally
                    {
                        semaforo.Release();
                        
                        int totalCompletados = Interlocked.Increment(ref procesados);
                        progresoBarra.Report(totalCompletados);
                        
                        if (EstaPausado)
                            progresoTexto?.Report($"Paused (Completed {totalCompletados} of {totalUrls})...");
                        else
                            progresoTexto?.Report($"Processing {totalCompletados} of {totalUrls}...");
                    }
                }, _cts.Token));
            }
        }
        finally
        {
            try
            {
                await Task.WhenAll(tareas);
            }
            catch (OperationCanceledException)
            {
                resultado.FueCancelado = true;
            }

            if (_cts is not null && _cts.Token.IsCancellationRequested)
            {
                resultado.FueCancelado = true;
            }
            
            // Clean up resources
            _cts?.Dispose();
            _cts = null;
            _eventoPausa?.Dispose();
            _eventoPausa = null;
        }
        
        resultado.Exitosas = contadorExitosas;
        resultado.Fallidas = contadorFallidas;

        return resultado;
    }
}