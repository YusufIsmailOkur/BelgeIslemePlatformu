namespace StoreApp.Models.Fields
{
    // Belge türünden bağımsız ortak alan kataloğu (bkz. Föy 06, madde 1).
    // Her belge türü için ayrı alan seti yerine tek ortak şema kullanılır; kural tabanlı ve
    // AI destekli çıkarım (Föy 06, madde 4-5) sonuçlarını bu alanlara eşler.
    public static class DocumentFieldSchema
    {
        public static IReadOnlyList<DocumentFieldDefinition> HeaderFields { get; } = new[]
        {
            new DocumentFieldDefinition("document_number", "Belge No", DocumentFieldDataType.Text, DocumentFieldScope.Header, IsRequired: true),
            new DocumentFieldDefinition("document_date", "Tarih", DocumentFieldDataType.Date, DocumentFieldScope.Header),
            new DocumentFieldDefinition("company_name", "Firma", DocumentFieldDataType.Text, DocumentFieldScope.Header),
            new DocumentFieldDefinition("customer_name", "Müşteri", DocumentFieldDataType.Text, DocumentFieldScope.Header),
            new DocumentFieldDefinition("due_date", "Termin", DocumentFieldDataType.Date, DocumentFieldScope.Header),
            new DocumentFieldDefinition("description", "Açıklama", DocumentFieldDataType.Text, DocumentFieldScope.Header),
        };

        // Kalem tablosundaki her satır için tekrarlanan alanlar (bkz. Föy 07 "Kalem tablosu düzenleme").
        public static IReadOnlyList<DocumentFieldDefinition> LineItemFields { get; } = new[]
        {
            new DocumentFieldDefinition("item_description", "Ürün/Hizmet", DocumentFieldDataType.Text, DocumentFieldScope.LineItem, IsRequired: true),
            new DocumentFieldDefinition("quantity", "Miktar", DocumentFieldDataType.Decimal, DocumentFieldScope.LineItem),
            new DocumentFieldDefinition("unit", "Birim", DocumentFieldDataType.Text, DocumentFieldScope.LineItem),
            new DocumentFieldDefinition("unit_price", "Birim Fiyat", DocumentFieldDataType.Decimal, DocumentFieldScope.LineItem),
            new DocumentFieldDefinition("note", "Not", DocumentFieldDataType.Text, DocumentFieldScope.LineItem),
        };

        public static IReadOnlyList<DocumentFieldDefinition> All { get; } =
            HeaderFields.Concat(LineItemFields).ToArray();
    }
}
