namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     角色数据传输对象
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<string> Permissions { get; set; } = new();
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
///     创建角色请求
/// </summary>
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
///     更新角色请求
/// </summary>
public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
///     权限枚举
/// </summary>
public static class Permissions
{
    public const string UserManagement = "user.management";
    public const string UserView = "user.view";
    public const string UserCreate = "user.create";
    public const string UserEdit = "user.edit";
    public const string UserDelete = "user.delete";

    public const string RoleManagement = "role.management";
    public const string RoleView = "role.view";
    public const string RoleCreate = "role.create";
    public const string RoleEdit = "role.edit";
    public const string RoleDelete = "role.delete";

    public const string ApiKeyManagement = "apikey.management";
    public const string ApiKeyView = "apikey.view";
    public const string ApiKeyCreate = "apikey.create";
    public const string ApiKeyEdit = "apikey.edit";
    public const string ApiKeyDelete = "apikey.delete";

    public const string AccountManagement = "account.management";
    public const string AccountView = "account.view";
    public const string AccountCreate = "account.create";
    public const string AccountEdit = "account.edit";
    public const string AccountDelete = "account.delete";

    public const string SystemSettings = "system.settings";
    public const string SystemLogs = "system.logs";
    public const string SystemMetrics = "system.metrics";

    /// <summary>
    ///     获取所有权限
    /// </summary>
    public static List<string> GetAllPermissions()
    {
        return new List<string>
        {
            UserManagement, UserView, UserCreate, UserEdit, UserDelete,
            RoleManagement, RoleView, RoleCreate, RoleEdit, RoleDelete,
            ApiKeyManagement, ApiKeyView, ApiKeyCreate, ApiKeyEdit, ApiKeyDelete,
            AccountManagement, AccountView, AccountCreate, AccountEdit, AccountDelete,
            SystemSettings, SystemLogs, SystemMetrics
        };
    }

    /// <summary>
    ///     获取管理员默认权限
    /// </summary>
    public static List<string> GetAdminPermissions()
    {
        return GetAllPermissions();
    }

    /// <summary>
    ///     获取普通用户默认权限
    /// </summary>
    public static List<string> GetUserPermissions()
    {
        return new List<string>
        {
            UserView, ApiKeyView, AccountView, SystemMetrics
        };
    }
}