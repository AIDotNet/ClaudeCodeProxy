using System.Security.Cryptography;
using System.Text;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Env;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     数据库初始化服务
/// </summary>
public class DatabaseInitializationService(
    IContext context,
    ILogger<DatabaseInitializationService> logger,
    IConfiguration configuration)
{
    /// <summary>
    ///     初始化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("开始初始化数据库...");

            // 初始化默认角色
            await InitializeRolesAsync();

            // 初始化默认管理员用户
            await InitializeAdminUserAsync();

            logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    ///     初始化默认角色
    /// </summary>
    private async Task InitializeRolesAsync()
    {
        // 检查是否已存在角色
        if (context.Roles.Any())
        {
            logger.LogInformation("角色已存在，跳过初始化");
            return;
        }

        logger.LogInformation("创建默认角色...");

        var roles = new[]
        {
            new Role
            {
                Id = 1,
                Name = "Admin",
                Description = "系统管理员，拥有所有权限",
                IsSystem = true,
                Permissions =
                [
                    "user.management", "user.view", "user.create", "user.edit", "user.delete",
                    "role.management", "role.view", "role.create", "role.edit", "role.delete",
                    "apikey.management", "apikey.view", "apikey.create", "apikey.edit", "apikey.delete",
                    "account.management", "account.view", "account.create", "account.edit", "account.delete",
                    "system.settings", "system.logs", "system.metrics"
                ]
            },
            new Role
            {
                Id = 2,
                Name = "User",
                Description = "普通用户，只有基本查看权限",
                IsSystem = true,
                Permissions = ["user.view", "apikey.view", "account.view", "system.metrics"]
            }
        };

        foreach (var role in roles)
        {
            role.CreatedAt = DateTime.Now;
            role.CreatedBy = "System";
            context.Roles.Add(role);
        }

        await context.SaveAsync();
        logger.LogInformation("默认角色创建完成");
    }

    /// <summary>
    ///     初始化默认管理员用户
    /// </summary>
    private async Task InitializeAdminUserAsync()
    {
        // 检查是否已存在管理员用户
        if (context.Users.Any(u => u.RoleId == 1))
        {
            logger.LogInformation("管理员用户已存在，跳过初始化");
            return;
        }

        logger.LogInformation("创建默认管理员用户...");

        // 从环境变量或配置获取管理员用户名和密码
        var adminUsername = EnvHelper.UserName;
        var adminPassword = EnvHelper.Password;
        var adminEmail = "239573049@qq.com";

        var adminUser = new User
        {
            Username = adminUsername,
            Email = adminEmail,
            PasswordHash = HashPassword(adminPassword),
            FirstName = "系统",
            LastName = "管理员",
            RoleId = 1, // Admin角色
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        context.Users.Add(adminUser);
        await context.SaveAsync();

        // 为管理员用户创建钱包
        await CreateAdminWalletAsync(adminUser.Id);

        logger.LogInformation("默认管理员用户创建完成");
        logger.LogWarning("默认管理员账户: {Username} / {Password}，请及时修改密码！", adminUsername,
            adminPassword.Length > 0 ? new string('*', adminPassword.Length) : "未设置");
    }

    /// <summary>
    ///     为管理员用户创建钱包
    /// </summary>
    private async Task CreateAdminWalletAsync(Guid userId)
    {
        logger.LogInformation("为管理员用户创建钱包...");

        var wallet = new Wallet
        {
            UserId = userId,
            Balance = 0,
            TotalUsed = 0,
            TotalRecharged = 0,
            Status = "active",
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        context.Wallets.Add(wallet);
        await context.SaveAsync();

        logger.LogInformation("管理员钱包创建完成");
    }

    /// <summary>
    ///     哈希密码
    /// </summary>
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "ClaudeCodeProxy"));
        return Convert.ToBase64String(hashedBytes);
    }
}