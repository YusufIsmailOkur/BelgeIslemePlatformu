namespace StoreApp.Models.Entities
{
    // Föy 08 madde 3: belgelerden çıkarılan müşteri/firma adlarının eşleştiği basit katalog.
    public class Customer
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string NormalizedName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
