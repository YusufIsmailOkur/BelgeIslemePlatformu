namespace StoreApp.Models.Entities
{
    // Föy 08 madde 3: kalem satırlarındaki ürün/hizmet açıklamalarının eşleştiği basit katalog.
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string NormalizedName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
