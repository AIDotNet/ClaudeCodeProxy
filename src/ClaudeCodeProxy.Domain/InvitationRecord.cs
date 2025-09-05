using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

public class InvitationRecord : Entity<Guid>
{
    public Guid InviterUserId { get; set; }

    public Guid InvitedUserId { get; set; }

    [MaxLength(8)] public string InvitationCode { get; set; } = string.Empty;

    public decimal InviterReward { get; set; } // 邀请人获得的奖励

    public decimal InvitedReward { get; set; } // 被邀请人获得的奖励

    public DateTime InvitedAt { get; set; } = DateTime.Now;

    public bool RewardProcessed { get; set; } = false; // 奖励是否已处理

    [MaxLength(200)] public string? Notes { get; set; }

    // 导航属性
    public virtual User InviterUser { get; set; } = null!;
    public virtual User InvitedUser { get; set; } = null!;
}