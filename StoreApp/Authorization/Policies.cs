using StoreApp.Models.Enums;

namespace StoreApp.Authorization
{
    // Named policy'ler [Authorize(Roles="...")] yerine rol grupları ifade etmek için kullanılır.
    public static class Policies
    {
        public const string OperatorOrAbove = "OperatorOrAbove";
        public const string ManagerOrAbove = "ManagerOrAbove";
        public const string SystemAdminOnly = "SystemAdminOnly";

        public static readonly string[] OperatorOrAboveRoles =
        {
            nameof(RoleCode.Operator),
            nameof(RoleCode.Manager),
            nameof(RoleCode.SystemAdmin)
        };

        public static readonly string[] ManagerOrAboveRoles =
        {
            nameof(RoleCode.Manager),
            nameof(RoleCode.SystemAdmin)
        };

        public static readonly string[] SystemAdminOnlyRoles =
        {
            nameof(RoleCode.SystemAdmin)
        };
    }
}
