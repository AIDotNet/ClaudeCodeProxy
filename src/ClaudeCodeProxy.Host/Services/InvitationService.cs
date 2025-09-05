using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Env;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

public interface IInvitationService
{
    Task<string> GetUserInvitationLinkAsync(Guid userId);
    Task<InvitationRecordDto[]> GetUserInvitationRecordsAsync(Guid userId);
    Task<bool> ProcessInvitationAsync(string invitationCode, Guid newUserId);
    Task<decimal> GetInviterRewardAsync();
    Task<decimal> GetInvitedRewardAsync();
    Task<int> GetMaxInvitationsAsync();
    Task<bool> CanUserInviteMoreAsync(Guid userId);
    Task<int> GetUserInvitationCountAsync(Guid userId);
    Task SetInvitationSettingsAsync(string key, string value);
    Task<string?> GetInvitationSettingAsync(string key);
}

public class InvitationService : IInvitationService
{
    private readonly IContext _context;

    public InvitationService(IContext context)
    {
        _context = context;
    }

    public async Task<string> GetUserInvitationLinkAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new ArgumentException("用户不存在");

        // 如果用户还没有邀请码，生成一个
        if (string.IsNullOrEmpty(user.InvitationCode))
        {
            user.InvitationCode = GenerateInvitationCode();
            await _context.SaveAsync();
        }

        // 返回完整的邀请链接
        return $"/register?inviteCode={user.InvitationCode}";
    }

    public async Task<InvitationRecordDto[]> GetUserInvitationRecordsAsync(Guid userId)
    {
        var records = await _context.InvitationRecords
            .Where(r => r.InviterUserId == userId)
            .Include(r => r.InvitedUser)
            .OrderByDescending(r => r.InvitedAt)
            .Select(r => new InvitationRecordDto
            {
                Id = r.Id,
                InvitedUsername = r.InvitedUser.Username,
                InvitedEmail = r.InvitedUser.Email,
                InvitedAt = r.InvitedAt,
                InviterReward = r.InviterReward,
                InvitedReward = r.InvitedReward,
                RewardProcessed = r.RewardProcessed,
                Notes = r.Notes
            })
            .ToArrayAsync();

        return records;
    }

    public async Task<bool> ProcessInvitationAsync(string invitationCode, Guid newUserId)
    {
        if (string.IsNullOrEmpty(invitationCode))
            return false;

        // 查找邀请人
        var inviter = await _context.Users.FirstOrDefaultAsync(u => u.InvitationCode == invitationCode);
        if (inviter == null)
            return false;

        // 检查邀请人是否还能邀请更多人
        if (!await CanUserInviteMoreAsync(inviter.Id))
            return false;

        // 获取奖励配置
        var inviterReward = await GetInviterRewardAsync();
        var invitedReward = await GetInvitedRewardAsync();

        // 更新被邀请用户的邀请关系
        var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == newUserId);
        if (invitedUser != null) invitedUser.InvitedByUserId = inviter.Id;

        // 创建邀请记录
        var invitationRecord = new InvitationRecord
        {
            InviterUserId = inviter.Id,
            InvitedUserId = newUserId,
            InvitationCode = invitationCode,
            InviterReward = inviterReward,
            InvitedReward = invitedReward,
            InvitedAt = DateTime.Now
        };

        _context.InvitationRecords.Add(invitationRecord);

        // 处理奖励
        await ProcessInvitationRewardsAsync(inviter.Id, newUserId, inviterReward, invitedReward);

        invitationRecord.RewardProcessed = true;
        await _context.SaveAsync();

        return true;
    }

    public async Task<decimal> GetInviterRewardAsync()
    {
        var setting = await GetInvitationSettingAsync(InvitationSettings.Keys.DefaultInviterReward);
        if (decimal.TryParse(setting, out var reward)) return reward;

        // 优先使用环境变量配置
        return EnvHelper.InviterReward;
    }

    public async Task<decimal> GetInvitedRewardAsync()
    {
        var setting = await GetInvitationSettingAsync(InvitationSettings.Keys.DefaultInvitedReward);
        if (decimal.TryParse(setting, out var reward)) return reward;

        // 优先使用环境变量配置
        return EnvHelper.InvitedReward;
    }

    public async Task<int> GetMaxInvitationsAsync()
    {
        var setting = await GetInvitationSettingAsync(InvitationSettings.Keys.DefaultMaxInvitations);
        if (int.TryParse(setting, out var max)) return max;

        // 优先使用环境变量配置
        return EnvHelper.MaxInvitations;
    }

    public async Task<bool> CanUserInviteMoreAsync(Guid userId)
    {
        var maxInvitations = await GetMaxInvitationsAsync();
        var currentCount = await GetUserInvitationCountAsync(userId);
        return currentCount < maxInvitations;
    }

    public async Task<int> GetUserInvitationCountAsync(Guid userId)
    {
        return await _context.InvitationRecords.CountAsync(r => r.InviterUserId == userId);
    }

    public async Task SetInvitationSettingsAsync(string key, string value)
    {
        var setting = await _context.InvitationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new InvitationSettings
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.Now
            };
            _context.InvitationSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.Now;
        }

        await _context.SaveAsync();
    }

    public async Task<string?> GetInvitationSettingAsync(string key)
    {
        var setting = await _context.InvitationSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    private string GenerateInvitationCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 排除易混淆字符
        var random = new Random();
        var code = new char[8];

        for (var i = 0; i < 8; i++) code[i] = chars[random.Next(chars.Length)];

        return new string(code);
    }

    private async Task ProcessInvitationRewardsAsync(Guid inviterId, Guid invitedId, decimal inviterReward,
        decimal invitedReward)
    {
        // 给邀请人发放奖励
        if (inviterReward > 0)
        {
            var inviterWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == inviterId);
            if (inviterWallet != null)
            {
                var inviterTransaction = new WalletTransaction
                {
                    WalletId = inviterWallet.Id,
                    TransactionType = "invitation_reward",
                    Amount = inviterReward,
                    BalanceBefore = inviterWallet.Balance,
                    BalanceAfter = inviterWallet.Balance + inviterReward,
                    Description = "邀请好友奖励",
                    Status = "completed"
                };

                inviterWallet.Balance += inviterReward;
                inviterWallet.TotalRecharged += inviterReward;

                _context.WalletTransactions.Add(inviterTransaction);
            }
        }

        // 给被邀请人发放奖励
        if (invitedReward > 0)
        {
            var invitedWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == invitedId);
            if (invitedWallet != null)
            {
                var invitedTransaction = new WalletTransaction
                {
                    WalletId = invitedWallet.Id,
                    TransactionType = "invitation_bonus",
                    Amount = invitedReward,
                    BalanceBefore = invitedWallet.Balance,
                    BalanceAfter = invitedWallet.Balance + invitedReward,
                    Description = "新用户邀请奖励",
                    Status = "completed"
                };

                invitedWallet.Balance += invitedReward;
                invitedWallet.TotalRecharged += invitedReward;

                _context.WalletTransactions.Add(invitedTransaction);
            }
        }

        await _context.SaveAsync();
    }
}