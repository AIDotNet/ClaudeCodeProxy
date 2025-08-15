using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
/// 用户服务
/// </summary>
public class UserService(IContext context,IMapper mapper, UserAccountBindingService? bindingService = null)
{
    /// <summary>
    /// 获取所有用户（分页）
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersAsync(int pageIndex = 1, int pageSize = 20)
    {
        // 获取总数
        var totalCount = await context.Users.CountAsync();
        
        // 获取分页数据
        var users = await context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var userDtos = mapper.Map<List<UserDto>>(users);
        
        return new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        return await context.Users
            .Include(u => u.Role)
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Avatar = u.Avatar,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                LastLoginAt = u.LastLoginAt,
                Provider = u.Provider,
                ProviderId = u.ProviderId,
                RoleId = u.RoleId,
                RoleName = u.Role.Name,
                CreatedAt = u.CreatedAt,
                ModifiedAt = u.ModifiedAt
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// 根据第三方登录信息获取用户
    /// </summary>
    public async Task<User?> GetUserByProviderAsync(string provider, string providerId)
    {
        return await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderId == providerId);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // 检查用户名是否已存在
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
        {
            throw new ArgumentException("用户名已存在");
        }

        // 检查邮箱是否已存在
        var existingEmail = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingEmail != null)
        {
            throw new ArgumentException("邮箱已存在");
        }

        // 检查角色是否存在
        var role = await context.Roles.FindAsync(request.RoleId);
        if (role == null)
        {
            throw new ArgumentException("角色不存在");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = request.RoleId,
            IsActive = true,
            EmailConfirmed = false,
            InvitationCode = GenerateInvitationCode()
        };

        context.Users.Add(user);
        await context.SaveAsync();

        return (await GetUserByIdAsync(user.Id))!;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task<UserDto> RegisterUserAsync(RegisterUserRequest request, IInvitationService? invitationService = null)
    {
        // 检查用户名是否已存在
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
        {
            throw new ArgumentException("用户名已存在");
        }

        // 检查邮箱是否已存在
        var existingEmail = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingEmail != null)
        {
            throw new ArgumentException("邮箱已存在");
        }

        // 获取默认的用户角色
        var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole == null)
        {
            throw new InvalidOperationException("系统未配置默认用户角色");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = defaultRole.Id,
            IsActive = true,
            EmailConfirmed = false,
            InvitationCode = GenerateInvitationCode()
        };

        try
        {
            context.Users.Add(user);
            await context.SaveAsync();

            // 创建用户钱包，使用环境变量配置的初始余额
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                Status = "active"
            };
            context.Wallets.Add(wallet);
            await context.SaveAsync();

            // 记录初始余额充值记录
            if (ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance > 0)
            {
                var initialTransaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    TransactionType = "initial_bonus",
                    Amount = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                    BalanceBefore = 0,
                    BalanceAfter = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                    Description = "新用户注册奖励",
                    Status = "completed"
                };
                context.WalletTransactions.Add(initialTransaction);
                await context.SaveAsync();
            }

        }
        catch
        {
            throw;
        }

        // 处理邀请码
        if (!string.IsNullOrEmpty(request.InvitationCode) && invitationService != null)
        {
            await invitationService.ProcessInvitationAsync(request.InvitationCode, user.Id);
        }

        return (await GetUserByIdAsync(user.Id))!;
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return null;
        }

        // 检查角色是否存在
        var role = await context.Roles.FindAsync(request.RoleId);
        if (role == null)
        {
            throw new ArgumentException("角色不存在");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Avatar = request.Avatar;
        user.IsActive = request.IsActive;
        user.EmailConfirmed = request.EmailConfirmed;
        user.RoleId = request.RoleId;

        await context.SaveAsync();

        // 如果提供了账户绑定信息，则更新绑定关系
        if (request.AccountBindings != null && bindingService != null)
        {
            await bindingService.UpdateUserAccountBindingsAsync(id, request.AccountBindings);
        }

        return await GetUserByIdAsync(id);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return false;
        }

        // 不能删除系统管理员
        if (user.RoleId == 1) // 假设1是admin角色ID
        {
            throw new ArgumentException("不能删除系统管理员");
        }

        context.Users.Remove(user);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // 验证当前密码
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new ArgumentException("当前密码不正确");
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    /// 重置密码（管理员功能）
    /// </summary>
    public async Task<bool> ResetPasswordAsync(int userId, ResetPasswordRequest request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    /// 验证用户登录
    /// </summary>
    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        // 更新最后登录时间
        user.LastLoginAt = DateTime.Now;
        await context.SaveAsync();

        return user;
    }

    /// <summary>
    /// 创建或更新第三方登录用户
    /// </summary>
    public async Task<User> CreateOrUpdateOAuthUserAsync(string provider, string providerId, string email, string? name, string? avatar, string? invitationCode = null, IInvitationService? invitationService = null)
    {
        var user = await GetUserByProviderAsync(provider, providerId);
        
        if (user == null)
        {
            // 检查邮箱是否已被其他用户使用
            var existingUser = await GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                // 如果邮箱已存在，绑定第三方登录信息
                existingUser.Provider = provider;
                existingUser.ProviderId = providerId;
                if (!string.IsNullOrEmpty(avatar))
                {
                    existingUser.Avatar = avatar;
                }
                existingUser.LastLoginAt = DateTime.Now;
                await context.SaveAsync();
                return existingUser;
            }

            // 创建新用户，默认角色为普通用户
            var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            user = new User
            {
                Username = GenerateUsernameFromEmail(email),
                Email = email,
                FirstName = name,
                Provider = provider,
                ProviderId = providerId,
                Avatar = avatar,
                RoleId = defaultRole?.Id ?? 2, // 默认用户角色
                IsActive = true,
                EmailConfirmed = true, // 第三方登录默认邮箱已验证
                LastLoginAt = DateTime.Now,
                InvitationCode = GenerateInvitationCode()
            };

            context.Users.Add(user);
            await context.SaveAsync();

            // 创建用户钱包，使用环境变量配置的初始余额
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                Status = "active"
            };
            context.Wallets.Add(wallet);
            await context.SaveAsync();

            // 记录初始余额充值记录
            if (ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance > 0)
            {
                var initialTransaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    TransactionType = "initial_bonus",
                    Amount = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                    BalanceBefore = 0,
                    BalanceAfter = ClaudeCodeProxy.Host.Env.EnvHelper.InitialUserBalance,
                    Description = "新用户注册奖励",
                    Status = "completed"
                };
                context.WalletTransactions.Add(initialTransaction);
                await context.SaveAsync();
            }

            // 处理邀请码
            if (!string.IsNullOrEmpty(invitationCode) && invitationService != null)
            {
                try
                {
                    await invitationService.ProcessInvitationAsync(invitationCode, user.Id);
                }
                catch (Exception ex)
                {
                    // 邀请码处理失败不影响注册，记录日志即可
                    Console.WriteLine($"OAuth邀请码处理失败: {ex.Message}");
                }
            }
        }
        else
        {
            // 更新用户信息
            if (!string.IsNullOrEmpty(avatar))
            {
                user.Avatar = avatar;
            }
            user.LastLoginAt = DateTime.Now;
            await context.SaveAsync();
        }

        return user;
    }

    /// <summary>
    /// 获取用户登录历史
    /// </summary>
    public async Task<List<UserLoginHistoryDto>> GetUserLoginHistoryAsync(Guid userId, int pageIndex = 0, int pageSize = 20)
    {
        return await context.UserLoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(h => new UserLoginHistoryDto
            {
                Id = h.Id,
                UserId = h.UserId,
                LoginType = h.LoginType,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent,
                City = h.City,
                Country = h.Country,
                Success = h.Success,
                FailureReason = h.FailureReason,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// 记录登录历史
    /// </summary>
    public async Task RecordLoginHistoryAsync(Guid userId, string loginType, string? ipAddress, string? userAgent, bool success, string? failureReason = null)
    {
        var history = new UserLoginHistory
        {
            UserId = userId,
            LoginType = loginType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            FailureReason = failureReason
        };

        context.UserLoginHistories.Add(history);
        await context.SaveAsync();
    }

    /// <summary>
    /// 哈希密码
    /// </summary>
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "ClaudeCodeProxy"));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    /// <summary>
    /// 从邮箱生成用户名
    /// </summary>
    private string GenerateUsernameFromEmail(string email)
    {
        var username = email.Split('@')[0];
        var counter = 1;
        var originalUsername = username;

        // 确保用户名唯一
        while (context.Users.Any(u => u.Username == username))
        {
            username = $"{originalUsername}{counter}";
            counter++;
        }

        return username;
    }

    /// <summary>
    /// 生成邀请码
    /// </summary>
    private string GenerateInvitationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 排除易混淆字符
        var random = new Random();
        var code = new char[8];
        
        for (int i = 0; i < 8; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        
        var generatedCode = new string(code);
        
        // 确保邀请码唯一
        while (context.Users.Any(u => u.InvitationCode == generatedCode))
        {
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            generatedCode = new string(code);
        }
        
        return generatedCode;
    }
}