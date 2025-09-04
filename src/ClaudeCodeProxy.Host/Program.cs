using System.Net.Http.Headers;
using ClaudeCodeProxy.Abstraction.Chats;
using ClaudeCodeProxy.Host.Env;
using ClaudeCodeProxy.Host.Services;
using ClaudeCodeProxy.Host.Endpoints;
using ClaudeCodeProxy.Host.Filters;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Core.AI;
using ClaudeCodeProxy.Core.Extensions;
using ClaudeCodeProxy.EntityFrameworkCore.Sqlite;
using ClaudeCodeProxy.Host.Helper;
using Making.Jwt.Extensions;
using Making.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Scalar.AspNetCore;
using Serilog;
using System.Runtime.InteropServices;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.EntityFrameworkCore.PostgreSQL;
using ClaudeCodeProxy.Host.Middlewares;
using Mapster;

namespace ClaudeCodeProxy.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // 处理命令行参数
        if (await HandleCommandLineArgumentsAsync(args))
        {
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
        });

        // 如果是Windows系统，配置为Windows服务
        if (WindowsServiceHelper.IsWindows())
        {
            builder.Host.UseWindowsService();
        }

        EnvHelper.Initialize(builder.Configuration);

        // 配置服务
        ConfigureServices(builder.Services, builder.Configuration);
        ConfigureSerilog(builder.Services, builder.Configuration);

        var app = builder.Build();

        // 配置中间件管道
        ConfigureMiddleware(app);

        // 配置端点
        ConfigureEndpoints(app);

        await MigrateDatabaseAsync(app);
        await InitializeModelPricingAsync(app);
        await InitializeInvitationSettingsAsync(app);

        // 如果是Windows且未安装服务，提示用户
        if (WindowsServiceHelper.IsWindows() && !await WindowsServiceHelper.IsServiceInstalledAsync())
        {
            Log.Information("检测到Windows系统，建议安装为Windows服务。");
            Log.Information("运行 '{ExecutableName} --install-service' 以管理员身份安装服务",
                Environment.ProcessPath ?? "ClaudeCodeProxy.Host.exe");
        }

        await app.RunAsync();
    }

    private static async Task<bool> HandleCommandLineArgumentsAsync(string[] args)
    {
        if (args.Length == 0)
            return false;

        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "--install-service":
            case "/install-service":
                await WindowsServiceHelper.InstallServiceAsync();
                return true;

            case "--uninstall-service":
            case "/uninstall-service":
                await WindowsServiceHelper.UninstallServiceAsync();
                return true;

            case "--start-service":
            case "/start-service":
                await WindowsServiceHelper.StartServiceAsync();
                return true;

            case "--stop-service":
            case "/stop-service":
                await WindowsServiceHelper.StopServiceAsync();
                return true;

            case "--service-status":
            case "/service-status":
                await WindowsServiceHelper.ShowServiceStatusAsync();
                return true;

            case "--help":
            case "/help":
            case "-h":
            case "/h":
                ShowHelp();
                return true;

            default:
                return false;
        }
    }

    private static void ShowHelp()
    {
        var executableName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "ClaudeCodeProxy.Host";

        Console.WriteLine("Claude Code Proxy Host");
        Console.WriteLine("用法:");
        Console.WriteLine($"  {executableName}                    启动应用程序");
        Console.WriteLine($"  {executableName} --help             显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("Windows服务管理 (需要管理员权限):");
        Console.WriteLine($"  {executableName} --install-service   安装为Windows服务");
        Console.WriteLine($"  {executableName} --uninstall-service 卸载Windows服务");
        Console.WriteLine($"  {executableName} --start-service     启动Windows服务");
        Console.WriteLine($"  {executableName} --stop-service      停止Windows服务");
        Console.WriteLine($"  {executableName} --service-status    显示Windows服务状态");
        Console.WriteLine();
        Console.WriteLine("注意: 在Windows系统上，建议安装为系统服务以获得更好的稳定性和自动启动功能。");
    }

    private static async Task MigrateDatabaseAsync(WebApplication app)
    {
        // 获取配置，判断是否需要迁移
        var runMigrationsAtStartup = app.Configuration.GetValue<bool>("RunMigrationsAtStartup");

        await using var scope = app.Services.CreateAsyncScope();
        if (runMigrationsAtStartup)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IContext>();

            await dbContext.MigrateAsync();
        }

        var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        await dbInitService.InitializeAsync();
    }

    private static async Task InitializeModelPricingAsync(WebApplication app)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var initService = scope.ServiceProvider.GetRequiredService<ModelPricingInitService>();

            // 初始化模型定价数据
            await initService.InitializeModelPricingAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "初始化模型定价数据失败");
            // 不抛出异常，允许应用程序继续启动
        }
    }

    private static async Task InitializeInvitationSettingsAsync(WebApplication app)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var invitationService = scope.ServiceProvider.GetRequiredService<IInvitationService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // 优先使用环境变量，其次使用配置文件
            var inviterReward = EnvHelper.InviterReward;
            var invitedReward = EnvHelper.InvitedReward;
            var maxInvitations = EnvHelper.MaxInvitations;

            // 只有当数据库中没有设置时才初始化
            var existingInviterReward = await invitationService.GetInvitationSettingAsync(InvitationSettings.Keys.DefaultInviterReward);
            if (string.IsNullOrEmpty(existingInviterReward))
            {
                await invitationService.SetInvitationSettingsAsync(InvitationSettings.Keys.DefaultInviterReward, inviterReward.ToString());
            }

            var existingInvitedReward = await invitationService.GetInvitationSettingAsync(InvitationSettings.Keys.DefaultInvitedReward);
            if (string.IsNullOrEmpty(existingInvitedReward))
            {
                await invitationService.SetInvitationSettingsAsync(InvitationSettings.Keys.DefaultInvitedReward, invitedReward.ToString());
            }

            var existingMaxInvitations = await invitationService.GetInvitationSettingAsync(InvitationSettings.Keys.DefaultMaxInvitations);
            if (string.IsNullOrEmpty(existingMaxInvitations))
            {
                await invitationService.SetInvitationSettingsAsync(InvitationSettings.Keys.DefaultMaxInvitations, maxInvitations.ToString());
            }

            var existingInvitationEnabled = await invitationService.GetInvitationSettingAsync(InvitationSettings.Keys.InvitationEnabled);
            if (string.IsNullOrEmpty(existingInvitationEnabled))
            {
                await invitationService.SetInvitationSettingsAsync(InvitationSettings.Keys.InvitationEnabled, "true");
            }

            Log.Information("邀请设置初始化完成: 邀请人奖励={InviterReward}, 被邀请人奖励={InvitedReward}, 最大邀请数={MaxInvitations}", 
                inviterReward, invitedReward, maxInvitations);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "初始化邀请设置失败");
            // 不抛出异常，允许应用程序继续启动
        }
    }

    private static void ConfigureSerilog(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog();

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        // 如果是Windows服务模式，添加文件日志
        if (WindowsServiceHelper.IsWindows())
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "claude-code-proxy-.log");
            loggerConfig.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
        }

        Log.Logger = loggerConfig.CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.IncludeFields = true;
            options.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy
                    .CamelCase));
            options.SerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddHttpClient()
            .ConfigureHttpClientDefaults((builder =>
            {
                builder.ConfigureHttpClient((client =>
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("ClaudeCodeProxy/1.0");
                }));
            }));

        services.AddSingleton<GlobalMiddleware>();
        services.AddMemoryCache();

        services.AddScoped<OAuthHelper>();
        services.AddScoped<OpenAiOAuthHelper>();
        services.AddSingleton<IOAuthSessionService, InMemoryOAuthSessionService>();

        services.AddScoped<SessionHelper>();

        if (configuration.GetConnectionString("Type").Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            
            services.AddEntityFrameworkCoreSqlite(configuration);
        
        }
        else if (configuration.GetConnectionString("Type").Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            services.AddEntityFrameworkCorePostgreSQL(configuration);
        }

        // 注册服务
        services.AddScoped<ApiKeyService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<UserAccountBindingService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<RoleService>();
        services.AddScoped<OAuthService>();
        services.AddScoped<WalletService>();
        services.AddScoped<RequestLogService>();
        services.AddScoped<RedeemCodeService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<DatabaseInitializationService>();
        services.AddScoped<AccountsService>();
        services.AddScoped<ClaudeProxyService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<VersionService>();
        services.AddSingleton<PricingService>(); // 价格服务使用单例，因为价格配置相对稳定
        services.AddScoped<ModelPricingInitService>(); // 模型定价初始化服务

        services.AddScoped<MessageService>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        services.AddCoreServices();
        
        services.AddMapster();

        services.AddResponseCompression();

        // 添加全局过滤器
        services.AddScoped<GlobalResponseFilter>();

        // OpenAPI支持
        services.AddOpenApi();

        // 配置JSON序列化选项
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy
                    .CamelCase));
        });

        // 添加授权和认证服务
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
        });
        services.AddJwtSupport(configuration);

        services.AddCors();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        // 开发环境配置
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/scalar");
        }

        app.UseMiddleware<GlobalMiddleware>();

        // 添加CORS支持（如果需要）
        app.UseCors(policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });

        app.UseStaticFiles();

        app.UseResponseCompression();

        // 认证和授权中间件
        app.UseJwtAuthentication();
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        // 添加全局过滤器到所有端点
        app.MapGroup("/")
            .AddEndpointFilter<GlobalResponseFilter>();

        // 配置API端点
        app.MapApiKeyEndpoints();
        app.MapAccountEndpoints();
        app.MapUserEndpoints();
        app.MapUserAccountBindingEndpoints();
        app.MapRoleEndpoints();
        app.MapOAuthEndpoints();
        app.MapWalletEndpoints();
        app.MapRequestLogEndpoints();
        app.MapProfileEndpoints();
        app.MapMessageEndpoints();
        app.MapAuthEndpoints();
        app.MapClaudeProxyEndpoints();
        app.MapRedeemCodeEndpoints();
        app.MapInvitationEndpoints();
        app.MapDashboardEndpoints();
        app.MapPricingEndpoints();
        app.MapVersionEndpoints();
        app.MapAnnouncementEndpoints();

        // 健康检查端点
        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.Now }))
            .WithName("HealthCheck")
            .WithSummary("健康检查")
            .WithTags("System");

        // 系统信息端点
        app.MapGet("/system-info", () =>
            {
                var info = new
                {
                    Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    Framework = RuntimeInformation.FrameworkDescription,
                    OS = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.OSArchitecture.ToString(),
                    IsWindows = WindowsServiceHelper.IsWindows(),
                    ProcessId = Environment.ProcessId,
                    WorkingDirectory = Environment.CurrentDirectory,
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    Timestamp = DateTime.Now
                };
                return Results.Ok(info);
            })
            .WithName("SystemInfo")
            .WithSummary("系统信息")
            .WithTags("System");

        // SPA fallback - 所有非API请求都返回index.html
        app.MapFallbackToFile("index.html");
    }
}