namespace StoreApp.Models.Enums
{
    public static class DocumentEnumExtensions
    {
        public static string ToDisplayName(this DocumentType documentType) => documentType switch
        {
            DocumentType.Invoice => "Fatura",
            DocumentType.Quote => "Teklif",
            DocumentType.Order => "Sipariş",
            DocumentType.DispatchNote => "İrsaliye",
            DocumentType.Specification => "Şartname",
            DocumentType.ShipmentRequest => "Sevkiyat Talep Formu",
            DocumentType.ContractAppendix => "Çerçeve Sözleşme Eki",
            DocumentType.TechnicalAppendix => "Teknik Şartname Eki",
            DocumentType.BulkOrderList => "Toplu Sipariş/Liste",
            DocumentType.Other => "Genel Belge",
            _ => documentType.ToString()
        };

        public static string ToDisplayName(this DocumentStatus status) => status switch
        {
            DocumentStatus.Uploaded => "Yüklendi",
            DocumentStatus.Processing => "İşleniyor",
            DocumentStatus.WaitingValidation => "Doğrulama Bekliyor",
            DocumentStatus.Approved => "Onaylandı",
            DocumentStatus.Rejected => "Reddedildi",
            DocumentStatus.SavedToSql => "SQL'e Kaydedildi",
            DocumentStatus.ExportedToTarget => "Hedef Sisteme Aktarıldı",
            DocumentStatus.FailedRetry => "Hatalı / Tekrar Denenecek",
            _ => status.ToString()
        };

        public static string ToBadgeClass(this DocumentStatus status) => status switch
        {
            DocumentStatus.Uploaded => "text-bg-secondary",
            DocumentStatus.Processing => "text-bg-info",
            DocumentStatus.WaitingValidation => "text-bg-warning",
            DocumentStatus.Approved => "text-bg-success",
            DocumentStatus.Rejected => "text-bg-danger",
            DocumentStatus.SavedToSql => "text-bg-primary",
            DocumentStatus.ExportedToTarget => "text-bg-success",
            DocumentStatus.FailedRetry => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }
}
