using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

public class Role : Entity<int>
{
    [Required] [MaxLength(50)] public string Name { get; set; } = string.Empty;

    [MaxLength(200)] public string? Description { get; set; }

    public bool IsSystem { get; set; } = false;

    // 权限配置 (JSON格式存储)
    public List<string> Permissions { get; set; } = new();

    // 导航属性
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}