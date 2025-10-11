// Copyright (c) 2024 Jiban Advanced Systems, web: https://www.jiban.ec/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System;

namespace Jiban.Domain;

/// <summary>
/// This is an example of how you might build a real application
/// Notice that there are lots of permissions - the idea is to have very detailed control over your software
/// These permissions are combined to create a Role, which will be more human-focused
/// </summary>
public enum DefaultPermissions : ushort //Must be ushort to work with AuthP
{
    NotSet = 0, //error condition

    [Display(GroupName = "Dummy", Name = "Dummy", Description = "Dummy Permission")]
    Dummy = 1,

    //ElectronicDocs
    [Display(GroupName = "Invoices", Name = "View invoice", Description = "Puede ver facturas")]
    InvoiceRead = 10,
    [Display(GroupName = "Invoices", Name = "Create invoice", Description = "Puede crear facturas")]
    InvoiceCreate = 11,
    [Display(GroupName = "Invoices", Name = "Modify invoice", Description = "Puede modificar facturas")]
    InvoiceUpdate = 12,
    [Display(GroupName = "Invoices", Name = "View All invoices", Description = "Puede ver todas las facturas")]
    InvoiceAll = 13,
    [Display(GroupName = "Invoices", Name = "Delete invoice", Description = "Puede eliminar facturas")]
    InvoiceDelete = 14,
    [Display(GroupName = "ReferralGuide", Name = "View Guía Remisión", Description = "Puede ver guías de remisión")]
    ReferralGuideRead = 15,
    [Display(GroupName = "ReferralGuide", Name = "Create Guía Remisión", Description = "Puede crear guías de remisión")]
    ReferralGuideCreate = 16,
    [Display(GroupName = "ReferralGuide", Name = "Modify Guía Remisión", Description = "Puede modificar guías de remisión")]
    ReferralGuideUpdate = 17,
    [Display(GroupName = "ReferralGuide", Name = "View All Guías Remisión", Description = "Puede ver todas las guías de remisión")]
    ReferralGuideAll = 18,

    //Used by tenant-level admin user
    [Obsolete]
    [Display(GroupName = "Employees", Description = "Puede leer empleados del inquilino")]
    EmployeeRead = 30,
    [Obsolete]
    [Display(GroupName = "Employees", Description = "Puede revocar o activar a un empleado del inquilino")]
    EmployeeRevokeActivate = 31,

    [Display(GroupName = "Employees", Description = "Puede invitar a nuevos usuarios a unirse al inquilino")]
    InviteUsers = 32,

    //----------------------------------------------------
    //This is an example of what to do with permission you don't used anymore.
    //You don't want its number to be reused as it could cause problems 
    //Just mark it as obsolete and the PermissionDisplay code won't show it
    [Obsolete]
    [Display(GroupName = "Old", Name = "Not used", Description = "Ejemplo de permiso antiguo que no se usa")]
    OldPermissionNotUsed = 1_000,

    //----------------------------------------------------
    // A enum member with no <see cref="DisplayAttribute"/> can be used, but its not shown in the PermissionDisplay at all
    // Useful if are working on new permissions but you don't want it to be used by anyone yet 
    AnotherPermission = 2_000,

    //Here is an orders of detailed control over some feature
    [Display(GroupName = "Orders", Name = "View order", Description = "Puede ver ordenes")]
    OrderRead = 3000,
    [Display(GroupName = "Orders", Name = "Create order", Description = "Puede crear ordenes")]
    OrderCreate = 3001,
    [Display(GroupName = "Orders", Name = "Modify order", Description = "Puede modificar ordenes")]
    OrderModify = 3002,
    [Display(GroupName = "Orders", Name = "View All orders", Description = "Puede ver todas las ordenes")]
    OrderAll = 3003,
    [Display(GroupName = "Orders", Name = "Activate order", Description = "Puede activar las ordenes")]
    OrderActivate = 3004,
    [Display(GroupName = "Orders", Name = "Process order", Description = "Puede procesar las ordenes")]
    OrderProcess = 3005,
    [Display(GroupName = "Orders", Name = "Log order", Description = "Puede registrar el log de las ordenes")]
    OrderLog = 3006,

    //Here is an articles of detailed control over some feature
    [Display(GroupName = "Articles", Name = "View article", Description = "Puede ver artículos")]
    ArticleRead = 3100,
    [Display(GroupName = "Articles", Name = "Create article", Description = "Puede crear artículos")]
    ArticleCreate = 3101,
    [Display(GroupName = "Articles", Name = "Modify article", Description = "Puede modificar artículos")]
    ArticleModify = 3102,
    [Display(GroupName = "Articles", Name = "View All articles", Description = "Puede ver todos los artículos")]
    ArticleAll = 3103,

    //Here is an deposits of detailed control over some feature
    [Display(GroupName = "Deposits", Name = "View deposit", Description = "Puede ver depósitos")]
    DepositRead = 3200,
    [Display(GroupName = "Deposits", Name = "Create deposit", Description = "Puede crear depósitos")]
    DepositCreate = 3201,
    [Display(GroupName = "Deposits", Name = "Modify deposit", Description = "Puede modificar depósitos")]
    DepositUpdate = 3202,
    [Display(GroupName = "Deposits", Name = "View All deposits", Description = "Puede ver todos los depósitos")]
    DepositAll = 3203,
    [Display(GroupName = "Deposits", Name = "Delete deposit", Description = "Puede eliminar depósitos")]
    DepositDelete = 3204,

    //Here is an customers of detailed control over some feature
    [Display(GroupName = "Customers", Name = "View customer", Description = "Puede ver clientes")]
    CustomerRead = 3300,
    [Display(GroupName = "Customers", Name = "Create customer", Description = "Puede crear clientes")]
    CustomerCreate = 3301,
    [Display(GroupName = "Customers", Name = "Modify customer", Description = "Puede modificar clientes")]
    CustomerModify = 3302,
    [Display(GroupName = "Customers", Name = "View All customers", Description = "Puede ver todos los clientes")]
    CustomerAll = 3303,
    [Display(GroupName = "Customers", Name = "Delete customer", Description = "Puede eliminar clientes")]
    CustomerDelete = 3304,

    //Here is an payments of detailed control over some feature
    [Display(GroupName = "DocumentSri", Name = "View document SRI", Description = "Puede ver documentos SRI")]
    DocumentSriRead = 3400,
    [Display(GroupName = "DocumentSri", Name = "Create document SRI", Description = "Puede crear documentos SRI")]
    DocumentSriCreate = 3401,
    [Display(GroupName = "DocumentSri", Name = "Update document SRI", Description = "Puede modificar documentos SRI")]
    DocumentSriUpdate = 3402,
    [Display(GroupName = "DocumentSri", Name = "View All documents", Description = "Puede ver todos los documentos SRI")]
    DocumentAll = 3403,

    [Display(GroupName = "Appointment", Name = "View appointment", Description = "Puede ver cita")]
    AppointmentRead = 3500,
    [Display(GroupName = "Appointment", Name = "Create appointment", Description = "Puede crear cita")]
    AppointmentCreate = 3501,
    [Display(GroupName = "Appointment", Name = "Modify appointment", Description = "Puede modificar cita")]
    AppointmentModify = 3502,
    [Display(GroupName = "Appointment", Name = "View All appointment", Description = "Puede ver todas las cita")]
    AppointmentAll= 3503,
    [Display(GroupName = "Appointment", Name = "Delete appointment", Description = "Puede eliminar la cita")]
    AppointmentDelete = 3504,

    //Subscriptions
    [Display(GroupName = "Subscriptions", Name = "View subscription", Description = "Puede ver suscripciones")]
    SubscriptionRead = 3600,
    [Display(GroupName = "Subscriptions", Name = "Create subscription", Description = "Puede crear suscripciones")]
    SubscriptionCreate = 3601,
    [Display(GroupName = "Subscriptions", Name = "Modify subscription", Description = "Puede modificar suscripciones")]
    SubscriptionModify = 3602,
    [Display(GroupName = "Subscriptions", Name = "View All subscription", Description = "Puede ver todas las suscripciones")]
    SubscriptionAll = 3603,
    [Display(GroupName = "Subscriptions", Name = "Delete subscription", Description = "Puede eliminar suscripciones")]
    SubscriptionDelete = 3604,

    //Productos
    [Display(GroupName = "Products", Name = "View product", Description = "Puede ver productos")]
    ProductRead = 3700,
    [Display(GroupName = "Products", Name = "Create product", Description = "Puede crear productos")]
    ProductCreate = 3701,
    [Display(GroupName = "Products", Name = "Modify product", Description = "Puede modificar productos")]
    ProductModify = 3702,
    [Display(GroupName = "Products", Name = "View All products", Description = "Puede ver todos los productos")]
    ProductAll = 3703,
    [Display(GroupName = "Products", Name = "Activate product", Description = "Puede activar los productos")]
    ProductActivate = 3704,

    //Cuentas y Transferencias
    [Display(GroupName = "Accounts", Name = "Create transfer", Description = "Puede crear transferencias")]
    AccountTransferCreate = 3800,

    //Admin section
    //40_000 - User admin
    [Display(GroupName = "UserAdmin", Name = "Read users", Description = "Puede listar usuarios")]
    UserRead = 40_000,
    [Display(GroupName = "UserAdmin", Name = "Sync users", Description = "Puede sincronizar el proveedor de autorización con AuthUsers")]
    UserSync = 40_001,
    [Display(GroupName = "UserAdmin", Name = "Alter users", Description = "Puede acceder a la actualización del usuario")]
    UserChange = 40_002,
    [Display(GroupName = "UserAdmin", Name = "Alter user's roles", Description = "Puede agregar o quitar roles de un usuario")]
    UserRolesChange = 40_003,
    [Display(GroupName = "UserAdmin", Name = "Move a user to another tenant", Description = "Puede controlar a qué tenant pertenece el usuario")]
    UserChangeTenant = 40_004,
    [Display(GroupName = "UserAdmin", Name = "Remove user", Description = "Puede eliminar el usuario")]
    UserRemove = 40_005,
    [Display(GroupName = "UserAdmin", Name = "Alter email", Description = "Puede modificar el correo electrónico del usuario")]
    UserEmailUpdate = 40_006,
    [Display(GroupName = "UserAdmin", Name = "Read administration", Description = "Puede leer la administración")]
    AdministrationRead = 40_007,
    [Display(GroupName = "UserAdmin", Name = "Maintenance Tenant", Description = "Mantenimiento Empresa")]
    MaintenanceTenant = 40_008,
    [Display(GroupName = "UserAdmin", Name = "View All users", Description = "Puede ver todos los usuarios")]
    UserAll = 40_009,
    [Display(GroupName = "UserAdmin", Name = "Change Password User", Description = "Puede cambiar password de usuario")]
    UserPassword= 40_010,
    [Display(GroupName = "UserAdmin", Name = "Read Taxes", Description = "Leer impuestos globales del sistema")]
    ReadTaxes= 40_011,

    //41_000 - Roles permissions
    [Display(GroupName = "RolesAdmin", Name = "Read Roles", Description = "Puede ver Roles")]
    RoleRead = 41_000,
    [Display(GroupName = "RolesAdmin", Name = "Change Role", Description = "Puede actualizar or eliminar un Rol", AutoGenerateFilter = true)]
    RoleChange = 41_001,
    [Display(GroupName = "RolesAdmin", Name = "Create Role", Description = "Puede crear un Rol", AutoGenerateFilter = true)]
    RoleCreate = 41_002,
    [Display(GroupName = "RolesAdmin", Name = "See permissions", Description = "Puede mostrar la lista de permisos", AutoGenerateFilter = true)]
    PermissionRead = 41_003,
    [Display(GroupName = "RolesAdmin", Name = "See all permissions", Description = "Puede mostrar la lista de permisos (incluirá el permiso filtrado)", AutoGenerateFilter = true)]
    IncludeFilteredPermissions = 41_004,

    //42_000 - tenant admin
    [Display(GroupName = "TenantAdmin", Name = "Read Tenants", Description = "Listar Empresas")]
    TenantList = 42_000,
    [Display(GroupName = "TenantAdmin", Name = "Create new Tenant", Description = "Crear Empresas", AutoGenerateFilter = true)]
    TenantCreate = 42_001,
    [Display(GroupName = "TenantAdmin", Name = "Alter Tenants info", Description = "Actualizar Empresas", AutoGenerateFilter = true)]
    TenantUpdate = 42_002,
    [Display(GroupName = "TenantAdmin", Name = "Move tenant to another parent", Description = "Puede mover el inquilino a un padre diferente  (WARNING)", AutoGenerateFilter = true)]
    TenantMove = 42_003,
    [Display(GroupName = "TenantAdmin", Name = "Delete tenant", Description = "Borrar Empresas (WARNING)", AutoGenerateFilter = true)]
    TenantDelete = 42_004,
    [Display(GroupName = "TenantAdmin", Name = "Access other tenant data", Description = "Cambiar de Empresa", AutoGenerateFilter = true)]
    TenantAccessData = 42_005,
    [Display(GroupName = "TenantAdmin", Name = "Clear Cache Tenant", Description = "Limpiar Cache Empresa", AutoGenerateFilter = true)]
    TenantCache = 42_006,
    [Display(GroupName = "TenantAdmin", Name = "Cloud Storage Tenant", Description = "Cloud Storage Personalizado Empresa", AutoGenerateFilter = true)]
    TenantCloudStorage = 42_007,
    [Display(GroupName = "TenantAdmin", Name = "Smtp Tenant", Description = "Smtp Personalizado Empresa", AutoGenerateFilter = true)]
    TenantSmtp = 42_008,
    [Display(GroupName = "TenantAdmin", Name = "Sesiones Ilimitadas", Description = "Sesiones Ilimitadas", AutoGenerateFilter = true)]
    TenantUnlimitedSessions = 42_009,
    [Display(GroupName = "TenantAdmin", Name = "Favicon Tenant", Description = "Favicon Empresa", AutoGenerateFilter = true)]
    TenantFavicon = 42_010,

    //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
    //to admin users who aren't allowed alter this permissions
    //Useful for multi-tenant applications where you can set up company-level admin users where you can hide some higher-level permissions
    [Display(GroupName = "SuperAdmin", Name = "AccessAll",
        Description = "Esto permite que el usuario acceda a todas las funciones.", AutoGenerateFilter = true)]
    AccessAll = ushort.MaxValue,
}