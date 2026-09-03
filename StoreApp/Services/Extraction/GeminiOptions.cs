namespace StoreApp.Services.Extraction
{
    // appsettings.json "Gemini" bölümünden bağlanır. ApiKey user-secrets/ortam değişkeninden
    // okunmalıdır, koda veya appsettings.json'a gömülmemelidir (bkz. Föy 06, madde 5).
    public sealed class GeminiOptions
    {
        public bool Enabled { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public int TimeoutSeconds { get; set; } = 30;
    }
}
