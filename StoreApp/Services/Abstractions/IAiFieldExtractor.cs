namespace StoreApp.Services.Abstractions
{
    // Kural tabanlı çıkarımın boş/zayıf kaldığı alanları doldurmak için kullanılan AI destekli
    // çıkarım (bkz. Föy 06, madde 5). Yapılandırılmamışsa, devre dışıysa veya çağrı başarısız
    // olursa null döner; bu durumda pipeline sadece kural tabanlı sonuçla devam eder.
    public interface IAiFieldExtractor
    {
        Task<ExtractionResult?> ExtractAsync(string? rawText, IReadOnlyList<ParsedTable> tables, CancellationToken cancellationToken = default);
    }
}
