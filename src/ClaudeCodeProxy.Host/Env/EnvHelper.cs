namespace ClaudeCodeProxy.Host.Env;

public class EnvHelper
{
    // 用户初始余额配置

    // 邀请奖励配置

    /// <summary>
    ///     默认账号密码
    /// </summary>
    /// <returns></returns>
    public static string UserName { get; private set; }

    /// <summary>
    ///     默认账号密码
    /// </summary>
    public static string Password { get; private set; }

    public static string ApiVersion { get; private set; } = "2023-06-01";

    public static string BetaHeader { get; private set; }

    public static decimal InitialUserBalance { get; private set; }

    public static decimal InviterReward { get; private set; } = 10.0m;

    public static decimal InvitedReward { get; private set; } = 5.0m;

    public static int MaxInvitations { get; private set; } = 10;

    public static void Initialize(IConfiguration configuration)
    {
        var apiVersion = configuration["API_VERSION"];
        if (!string.IsNullOrEmpty(apiVersion)) ApiVersion = apiVersion;

        BetaHeader = configuration["BETA_HEADER"] ?? "oauth-2025-04-20,claude-code-20250219,interleaved-thinking-2025-05-14,fine-grained-tool-streaming-2025-05-14";

        var userName = configuration["USER_NAME"];
        var password = configuration["PASSWORD"];
        if (!string.IsNullOrEmpty(userName))
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty when UserName is provided.");

            UserName = userName;
            Password = password;
        }
        else
        {
            // 提供默认的账号密码
            UserName = "admin";
            Password = "admin123";
        }

        // 初始化用户余额配置
        var initialBalance = configuration["INITIAL_USER_BALANCE"];
        if (!string.IsNullOrEmpty(initialBalance) && decimal.TryParse(initialBalance, out var balance))
            InitialUserBalance = balance;

        // 初始化邀请奖励配置
        var inviterReward = configuration["INVITER_REWARD"];
        if (!string.IsNullOrEmpty(inviterReward) && decimal.TryParse(inviterReward, out var invReward))
            InviterReward = invReward;

        var invitedReward = configuration["INVITED_REWARD"];
        if (!string.IsNullOrEmpty(invitedReward) && decimal.TryParse(invitedReward, out var invedReward))
            InvitedReward = invedReward;

        var maxInvitations = configuration["MAX_INVITATIONS"];
        if (!string.IsNullOrEmpty(maxInvitations) && int.TryParse(maxInvitations, out var maxInv))
            MaxInvitations = maxInv;
    }
}