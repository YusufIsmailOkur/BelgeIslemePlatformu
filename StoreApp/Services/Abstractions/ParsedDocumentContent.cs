using StoreApp.Models.Enums;

namespace StoreApp.Services.Abstractions
{
    public sealed record ParsedTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

    public sealed record ParsedDocumentContent(
        DocumentSourceFormat SourceFormat,
        string? RawText,
        IReadOnlyList<ParsedTable> Tables,
        int? PageCount,
        int? SheetCount,
        int? RowCount,
        string? Encoding,
        string? Delimiter);
}
