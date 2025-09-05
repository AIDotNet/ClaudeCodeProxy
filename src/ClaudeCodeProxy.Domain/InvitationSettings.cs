using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

public class InvitationSettings : Entity<int>
{
    [Required] [MaxLength(50)] public string Key { get; set; } = string.Empty;

    [Required] public string Value { get; set; } = string.Empty;

    [MaxLength(200)] public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 常用设置键名
    public static class Keys
    {
        public const string DefaultInviterReward = "default_inviter_reward";
        public const string DefaultInvitedReward = "default_invited_reward";
        public const string DefaultMaxInvitations = "default_max_invitations";
        public const string InvitationEnabled = "invitation_enabled";
    }
}