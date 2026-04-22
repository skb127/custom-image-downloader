using custom_image_downloader.Models;
using Microsoft.Extensions.Options;

namespace custom_image_downloader.Services;

public sealed class DownloadManager : IDownloadManager
{
    // The network client now lives here
    private static readonly HttpClient ClienteHttp;

    static DownloadManager()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        ClienteHttp = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(12)
        };

        // Many servers (CDNs, APIs) reject requests without a User-Agent or Accept header
        ClienteHttp.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        ClienteHttp.DefaultRequestHeaders.Add("Accept", "*/*");
    }

    private readonly ILogger _logger;
    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _allowedMimeTypes;
    private readonly Dictionary<string, string> _mimeTypeExtensionMap;

    private CancellationTokenSource? _cts;
    private ManualResetEventSlim? _eventoPausa;

    public DownloadManager(ILogger logger, IOptions<DownloadSettings> options)
    {
        _logger = logger;

        DownloadSettings settings = options.Value;
        _allowedExtensions = new HashSet<string>(settings.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
        _allowedMimeTypes = new HashSet<string>(settings.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase);
        _mimeTypeExtensionMap =
            new Dictionary<string, string>(settings.MimeTypeExtensionMap, StringComparer.OrdinalIgnoreCase);
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
        IProgress<DownloadProgressInfo> progresoTexto)
    {
        _cts = new CancellationTokenSource();
        _eventoPausa = new ManualResetEventSlim(true); // Initial state is "set" (not paused)

        var resultado = new DownloadResult { RutaFinal = rutaSubcarpeta };
        int totalUrls = urls.Length;

        int contadorExitosas = 0;
        int contadorFallidas = 0;
        int contadorOmitidas = 0;
        int procesados = 0;

        // Thread-safe bag to collect paths of files written in this session
        var archivosDescargados = new System.Collections.Concurrent.ConcurrentBag<string>();
        var archivosEnProgreso = new System.Collections.Concurrent.ConcurrentBag<string>();

        using SemaphoreSlim semaforo = new SemaphoreSlim(concurrencia);
        List<Task> tareas = new List<Task>();

        try
        {
            progresoTexto.Report(new DownloadProgressInfo
            {
                State = DownloadState.Starting,
                Total = totalUrls
            });
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

                        Uri uri = new Uri(urlActual);
                        string nombreArchivoOriginal = Path.GetFileName(uri.LocalPath);
                        string extension = Path.GetExtension(nombreArchivoOriginal);

                        // --- TWO-TIER FILE TYPE VALIDATION ---

                        // Tier 1: URL has a visible extension → validate locally, no network request needed.
                        if (!string.IsNullOrEmpty(extension))
                        {
                            if (!_allowedExtensions.Contains(extension))
                            {
                                Interlocked.Increment(ref contadorOmitidas);
                                await _logger.EscribirAsync(
                                    $"SKIPPED (unsupported extension '{extension}'): {urlActual}");
                                return;
                            }

                            // Prefer the original filename; fall back to base+index if it has no name part.
                            string nombreOriginal = Path.GetFileNameWithoutExtension(nombreArchivoOriginal);
                            string candidato = !string.IsNullOrWhiteSpace(nombreOriginal)
                                ? nombreArchivoOriginal // keep original name
                                : $"{nombreBase}_{indiceArchivo:D3}{extension}";

                            // Avoid collisions: append _N if the file already exists
                            string rutaCompletaArchivo = ObtenerRutaSinConflicto(rutaSubcarpeta, candidato);
                            string nombreFinal = Path.GetFileName(rutaCompletaArchivo);

                            using (var response = await ClienteHttp.GetAsync(urlActual,
                                       HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                            {
                                response.EnsureSuccessStatusCode();
                                await using (Stream contentStream =
                                             await response.Content.ReadAsStreamAsync(_cts.Token))
                                await using (FileStream fileStream = new FileStream(rutaCompletaArchivo,
                                                 FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920,
                                                 useAsync: true))
                                {
                                    archivosEnProgreso.Add(rutaCompletaArchivo);
                                    await contentStream.CopyToAsync(fileStream, _cts.Token);
                                }
                            }

                            archivosDescargados.Add(rutaCompletaArchivo);
                            Interlocked.Increment(ref contadorExitosas);
                            await _logger.EscribirAsync($"SUCCESS: [{nombreFinal}] downloaded from {urlActual}");
                        }
                        else
                        {
                            // Tier 2: No visible extension → make the GET request and inspect Content-Type.
                            using var response = await ClienteHttp.GetAsync(
                                urlActual, HttpCompletionOption.ResponseHeadersRead, _cts.Token);

                            response.EnsureSuccessStatusCode();

                            string? mimeType = response.Content.Headers.ContentType?.MediaType;

                            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMimeTypes.Contains(mimeType))
                            {
                                Interlocked.Increment(ref contadorOmitidas);
                                await _logger.EscribirAsync(
                                    $"SKIPPED (unsupported or absent Content-Type '{mimeType ?? "null"}'): {urlActual}");
                                return;
                            }

                            // MIME is allowed → map MIME type to extension using config (falls back to .bin)
                            string extensionFallback = _mimeTypeExtensionMap.GetValueOrDefault(mimeType, ".bin");

                            // Extensionless URL: use base+index with the inferred extension
                            string nombreFinal = $"{nombreBase}_{indiceArchivo:D3}{extensionFallback}";
                            string rutaCompletaArchivo = ObtenerRutaSinConflicto(rutaSubcarpeta, nombreFinal);
                            nombreFinal = Path.GetFileName(rutaCompletaArchivo);

                            await using (Stream contentStream = await response.Content.ReadAsStreamAsync(_cts.Token))
                            await using (FileStream fileStream = new FileStream(rutaCompletaArchivo, FileMode.Create,
                                             FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                            {
                                archivosEnProgreso.Add(rutaCompletaArchivo);
                                await contentStream.CopyToAsync(fileStream, _cts.Token);
                            }

                            archivosDescargados.Add(rutaCompletaArchivo);
                            Interlocked.Increment(ref contadorExitosas);
                            await _logger.EscribirAsync($"SUCCESS: [{nombreFinal}] downloaded from {urlActual}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // We ignore the individual cancellation to catch it above
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref contadorFallidas);
                        await _logger.EscribirErrorAsync($"ERROR URL: {urlActual}", ex);
                    }
                    finally
                    {
                        semaforo.Release();

                        int totalCompletados = Interlocked.Increment(ref procesados);
                        progresoBarra.Report(totalCompletados);

                        progresoTexto.Report(new DownloadProgressInfo
                        {
                            State = EstaPausado ? DownloadState.Paused : DownloadState.Processing,
                            Processed = totalCompletados,
                            Total = totalUrls
                        });
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
        resultado.Omitidas = contadorOmitidas;
        resultado.RutasDescargadas.AddRange(archivosDescargados);
        resultado.RutasEnProgreso.AddRange(archivosEnProgreso);

        return resultado;
    }

    /// <summary>
    /// Returns a file path that does not conflict with existing files.
    /// If <paramref name="nombreArchivo"/> already exists, appends _2, _3, … until free.
    /// </summary>
    private static string ObtenerRutaSinConflicto(string carpeta, string nombreArchivo)
    {
        string ruta = Path.Combine(carpeta, nombreArchivo);
        if (!File.Exists(ruta)) return ruta;

        string sinExt = Path.GetFileNameWithoutExtension(nombreArchivo);
        string ext = Path.GetExtension(nombreArchivo);
        int contador = 2;
        do
        {
            ruta = Path.Combine(carpeta, $"{sinExt}_{contador}{ext}");
            contador++;
        } while (File.Exists(ruta));

        return ruta;
    }
}