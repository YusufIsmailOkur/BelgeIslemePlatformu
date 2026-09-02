namespace StoreApp.Models.Enums
{
    // Roles.Code alanıyla birebir eşleşir (bkz. Data/AppDbContext.cs seed verisi).
    public enum RoleCode
    {
        Operator,
        Manager,
        SystemAdmin,
        ReadOnly
    }
}
