namespace StoreApp.Services.Abstractions
{
    // RelativePath, depolama kökünden itibaren yyyy/MM/dd/{uuid}{ext} biçimindedir; ileride
    // S3'e taşınırsa doğrudan object key olarak kullanılabilir.
    public sealed record StoredFile(string RelativePath, string OriginalFileName, long SizeBytes);
}
