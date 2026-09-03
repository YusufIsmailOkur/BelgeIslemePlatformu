namespace StoreApp.Services.Abstractions
{
    public interface IDocumentContentParser
    {
        bool CanParse(string fileExtension);

        Task<ParsedDocumentContent> ParseAsync(Stream content, CancellationToken cancellationToken = default);
    }
}
