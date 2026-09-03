namespace StoreApp.Models.Fields
{
    public enum DocumentFieldScope
    {
        // Belge başına tek değer (bkz. gelecekteki document_fields, Föy 08).
        Header,

        // Kalem/satır başına tekrarlanan değer (bkz. gelecekteki document_items, Föy 08).
        LineItem
    }
}
