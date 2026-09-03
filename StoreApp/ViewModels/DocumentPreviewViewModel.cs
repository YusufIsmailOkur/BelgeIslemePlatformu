using StoreApp.Models.Entities;
using StoreApp.Services.Abstractions;

namespace StoreApp.ViewModels
{
    public sealed class DocumentPreviewViewModel
    {
        public required Document Document { get; init; }
        public IReadOnlyList<ParsedTable> Tables { get; init; } = Array.Empty<ParsedTable>();
    }
}
