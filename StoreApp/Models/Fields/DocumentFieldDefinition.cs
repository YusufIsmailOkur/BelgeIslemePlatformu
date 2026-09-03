namespace StoreApp.Models.Fields
{
    public sealed record DocumentFieldDefinition(
        string Key,
        string Label,
        DocumentFieldDataType DataType,
        DocumentFieldScope Scope,
        bool IsRequired = false);
}
