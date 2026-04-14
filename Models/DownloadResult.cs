namespace custom_image_downloader.Models;

public class DownloadResult
{
    public int Exitosas { get; set; }
    public int Fallidas { get; set; }
    public bool FueCancelado { get; set; }
    public string RutaFinal { get; set; } = "";
        
    // A useful shortcut to know if everything failed
    public bool FracasoTotal => Exitosas == 0 && Fallidas > 0 && !FueCancelado;
}