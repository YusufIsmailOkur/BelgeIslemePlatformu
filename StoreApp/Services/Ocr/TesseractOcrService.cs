using StoreApp.Services.Abstractions;
using Tesseract;

namespace StoreApp.Services.Ocr
{
    // Yerel Tesseract OCR motorunu kullanır. Not: Bu NuGet paketi yalnızca Windows native
    // ikilikleri (x86/x64) içerir; Linux tabanlı demo dağıtımında (Föy 12) sistem paketi
    // (libtesseract/libleptonica) veya farklı bir dağıtım gerekecek.
    public class TesseractOcrService : IOcrService, IDisposable
    {
        private readonly TesseractEngine _engine;
        private readonly object _engineLock = new();

        public TesseractOcrService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var configuredPath = configuration["Ocr:TessDataPath"] ?? "App_Data/tessdata";
            var tessDataPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);
            var language = configuration["Ocr:Language"] ?? "tur+eng";

            _engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
        }

        public Task<OcrPageResult> RecognizeAsync(PdfPageImage page, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TesseractEngine aynı anda tek bir Process çağrısını destekler; servis singleton
            // kaydedildiğinden eşzamanlı istekler burada sıraya alınır.
            lock (_engineLock)
            {
                using var image = Pix.LoadFromMemory(page.PngBytes);
                using var result = _engine.Process(image);

                return Task.FromResult(new OcrPageResult(page.PageNumber, result.GetText().Trim(), result.GetMeanConfidence()));
            }
        }

        public void Dispose() => _engine.Dispose();
    }
}
