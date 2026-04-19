namespace custom_image_downloader.Models;

public sealed class DownloadResult
{
    public int Exitosas { get; set; }
    public int Fallidas { get; set; }

    public int Omitidas { get; set; }

    public bool FueCancelado { get; set; }
    public string RutaFinal { get; set; } = "";

    public List<string> RutasDescargadas { get; } = new();

    public bool FracasoTotal => Exitosas == 0 && Fallidas > 0 && !FueCancelado;
}