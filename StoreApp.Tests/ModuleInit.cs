using System.Runtime.CompilerServices;
using System.Text;

namespace StoreApp.Tests
{
    // Program.cs'deki CodePages kaydını test host sürecinde de tekrarlar (Windows-1254 gibi
    // eski kod sayfalarının CsvDocumentParser testlerinde kullanılabilmesi için).
    internal static class ModuleInit
    {
        [ModuleInitializer]
        public static void Initialize() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
