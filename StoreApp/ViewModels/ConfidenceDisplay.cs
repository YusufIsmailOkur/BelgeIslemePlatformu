namespace StoreApp.ViewModels
{
    // Föy 07 madde 4: düşük güvenli alanlar renk/ikon ile işaretlenir. Eşikler kesin bir bilim
    // değil, MVP için makul bir varsayılan: %50 altı "güvenilmez" (kırmızı+ikon), %50-%80 arası
    // "kontrol edilmeli" (sarı), %80 üzeri "güvenilir" (yeşil).
    public static class ConfidenceDisplay
    {
        public const double LowThreshold = 0.5;
        public const double MediumThreshold = 0.8;

        public static string BadgeClass(double confidence) => confidence switch
        {
            < LowThreshold => "text-bg-danger",
            < MediumThreshold => "text-bg-warning",
            _ => "text-bg-success"
        };

        public static string InputBorderClass(double confidence) => confidence switch
        {
            < LowThreshold => "border-danger border-2",
            < MediumThreshold => "border-warning border-2",
            _ => ""
        };

        public static string? WarningIcon(double confidence) => confidence < LowThreshold ? "⚠" : null;
    }
}
