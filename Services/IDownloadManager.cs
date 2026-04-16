using custom_image_downloader.Models;

namespace custom_image_downloader.Services
{
    public interface IDownloadManager
    {
        bool EstaPausado { get; }
        void AlternarPausa();
        void Cancelar();
        Task<DownloadResult> IniciarDescargaAsync(
            string[] urls,
            string rutaSubcarpeta,
            string nombreBase,
            int concurrencia,
            IProgress<int> progresoBarra,
            IProgress<DownloadProgressInfo> progresoTexto);
    }
}