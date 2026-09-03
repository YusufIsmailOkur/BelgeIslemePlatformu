using System.Runtime.CompilerServices;
using StoreApp.Services.Abstractions;

namespace StoreApp.Tests.Fakes
{
    internal sealed class FakePdfPageRenderer : IPdfPageRenderer
    {
        private readonly IReadOnlyList<PdfPageImage> _pages;

        public FakePdfPageRenderer(params PdfPageImage[] pages)
        {
            _pages = pages;
        }

        public async IAsyncEnumerable<PdfPageImage> RenderPagesAsync(
            Stream pdfContent,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var page in _pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return page;
            }

            await Task.CompletedTask;
        }
    }
}
