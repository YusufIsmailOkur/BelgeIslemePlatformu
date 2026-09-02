namespace StoreApp.Models.Enums
{
    public enum DocumentType
    {
        // Other ilk sırada (değer 0) tutulur; belge türü sınıflandırması henüz
        // yapılmamış kayıtlar için güvenli varsayılan budur (bkz. Föy 06).
        Other,
        Invoice,
        Quote,
        Order,
        DispatchNote,
        Specification,
        ShipmentRequest,
        ContractAppendix,
        TechnicalAppendix,
        BulkOrderList
    }
}
