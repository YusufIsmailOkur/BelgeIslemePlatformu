namespace StoreApp.Services.Abstractions
{
    public interface IDocumentProcessingService
    {
        // Yüklendi -> İşleniyor -> Doğrulama Bekliyor (başarı) veya Hatalı/Tekrar Denenecek (hata) geçişini yürütür.
        Task ProcessAsync(int documentId, CancellationToken cancellationToken = default);
    }
}
