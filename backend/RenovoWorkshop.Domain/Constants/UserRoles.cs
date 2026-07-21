namespace RenovoWorkshop.Domain.Constants;

public static class UserRoles
{
    public const string Administrator = "Administrador";
    public const string Manager = "Gerente";
    public const string Reception = "Recepção";
    public const string Mechanic = "Mecânico";
    public const string Warehouse = "Almoxarifado";
    public const string Client = "Cliente";

    public static IReadOnlyList<string> All => new[]
    {
        Administrator,
        Manager,
        Reception,
        Mechanic,
        Warehouse,
        Client
    };
}

public static class UserPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string CustomersRead = "customers.read";
    public const string CustomersWrite = "customers.write";
    public const string VehiclesRead = "vehicles.read";
    public const string VehiclesWrite = "vehicles.write";
    public const string OrdersRead = "orders.read";
    public const string OrdersWrite = "orders.write";
    public const string InventoryRead = "inventory.read";
    public const string InventoryWrite = "inventory.write";
    public const string UsersManage = "users.manage";

    public static IReadOnlyList<string> ForRole(string role) => role switch
    {
        UserRoles.Administrator => new[]
        {
            DashboardView,
            CustomersRead,
            CustomersWrite,
            VehiclesRead,
            VehiclesWrite,
            OrdersRead,
            OrdersWrite,
            InventoryRead,
            InventoryWrite,
            UsersManage
        },
        UserRoles.Manager => new[]
        {
            DashboardView,
            CustomersRead,
            CustomersWrite,
            VehiclesRead,
            VehiclesWrite,
            OrdersRead,
            OrdersWrite,
            InventoryRead,
            InventoryWrite
        },
        UserRoles.Reception => new[]
        {
            DashboardView,
            CustomersRead,
            CustomersWrite,
            VehiclesRead,
            VehiclesWrite,
            OrdersRead,
            OrdersWrite
        },
        UserRoles.Mechanic => new[]
        {
            DashboardView,
            VehiclesRead,
            OrdersRead,
            OrdersWrite
        },
        UserRoles.Warehouse => new[]
        {
            InventoryRead,
            InventoryWrite,
            OrdersRead
        },
        UserRoles.Client => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };
}
