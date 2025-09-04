using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClaudeCodeProxy.EntityFrameworkCore.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    BackgroundColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "bg-blue-50"),
                    TextColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "text-blue-800"),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputPrice = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    OutputPrice = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    CacheWritePrice = table.Column<decimal>(type: "numeric(18,9)", nullable: false, defaultValue: 0m),
                    CacheReadPrice = table.Column<decimal>(type: "numeric(18,9)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelPricings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Permissions = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvitationCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "shared"),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    ProjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RateLimitDuration = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    SupportedModels = table.Column<string>(type: "TEXT", nullable: true),
                    ClaudeAiOauth = table.Column<string>(type: "TEXT", nullable: true),
                    GeminiOauth = table.Column<string>(type: "TEXT", nullable: true),
                    OpenAiOauth = table.Column<string>(type: "TEXT", nullable: true),
                    Proxy = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RateLimitedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxConcurrentUsers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeyValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TokenLimit = table.Column<int>(type: "integer", nullable: true),
                    RateLimitWindow = table.Column<int>(type: "integer", nullable: true),
                    RateLimitRequests = table.Column<int>(type: "integer", nullable: true),
                    ConcurrencyLimit = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DailyCostLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    MonthlyCostLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCostLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    DailyCostUsed = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyCostUsed = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Permissions = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "all"),
                    ClaudeAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaudeConsoleAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GeminiAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnableModelRestriction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RestrictedModels = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnableClientRestriction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowedClients = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalUsageCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Service = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "claude"),
                    DefaultAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccountBindings = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "success"),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvitationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    InviterReward = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    InvitedReward = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RewardProcessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationRecords_Users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvitationRecords_Users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RedeemCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "balance"),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedeemCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedeemCodes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedeemCodes_Users_UsedByUserId",
                        column: x => x.UsedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FailureReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    TotalUsed = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    TotalRecharged = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRechargedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccountBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BindingType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "private"),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccountBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccountBindings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccountBindings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "success"),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CacheCreateTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CacheReadTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Cost = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    ClientIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsStreaming = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "claude"),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestHour = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestLogs_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatisticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotHour = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SuccessfulRequestCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FailedRequestCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CacheCreateTokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CacheReadTokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalTokens = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalCost = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    AverageResponseTime = table.Column<double>(type: "double precision", nullable: true),
                    MaxResponseTime = table.Column<long>(type: "bigint", nullable: true),
                    MinResponseTime = table.Column<long>(type: "bigint", nullable: true),
                    ActiveApiKeyCount = table.Column<int>(type: "integer", nullable: true),
                    ActiveAccountCount = table.Column<int>(type: "integer", nullable: true),
                    RateLimitedAccountCount = table.Column<int>(type: "integer", nullable: true),
                    UniqueUserCount = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatisticsSnapshots_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StatisticsSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalletId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "completed"),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_RequestLogs_RequestLogId",
                        column: x => x.RequestLogId,
                        principalTable: "RequestLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountType",
                table: "Accounts",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatedAt",
                table: "Accounts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Global_Enabled_Status",
                table: "Accounts",
                columns: new[] { "IsGlobal", "IsEnabled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsEnabled",
                table: "Accounts",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsGlobal",
                table: "Accounts",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Name",
                table: "Accounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Platform",
                table: "Accounts",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Platform_IsEnabled_Status",
                table: "Accounts",
                columns: new[] { "Platform", "IsEnabled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Priority",
                table: "Accounts",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Status",
                table: "Accounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CreatedAt",
                table: "Announcements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_EndTime",
                table: "Announcements",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsVisible",
                table: "Announcements",
                column: "IsVisible");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsVisible_Priority_CreatedAt",
                table: "Announcements",
                columns: new[] { "IsVisible", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Priority",
                table: "Announcements",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_StartTime",
                table: "Announcements",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedAt",
                table: "ApiKeys",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_DefaultAccountId",
                table: "ApiKeys",
                column: "DefaultAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsEnabled",
                table: "ApiKeys",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Name",
                table: "ApiKeys",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Service",
                table: "ApiKeys",
                column: "Service");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "Action", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceId",
                table: "AuditLogs",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceType",
                table: "AuditLogs",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceType_ResourceId",
                table: "AuditLogs",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Result",
                table: "AuditLogs",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_InvitationCode",
                table: "InvitationRecords",
                column: "InvitationCode");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_InvitedAt",
                table: "InvitationRecords",
                column: "InvitedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_InvitedUserId",
                table: "InvitationRecords",
                column: "InvitedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_InviterUserId",
                table: "InvitationRecords",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_InviterUserId_InvitedAt",
                table: "InvitationRecords",
                columns: new[] { "InviterUserId", "InvitedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationRecords_RewardProcessed",
                table: "InvitationRecords",
                column: "RewardProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationSettings_Key",
                table: "InvitationSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelPricings_Currency",
                table: "ModelPricings",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPricings_IsEnabled",
                table: "ModelPricings",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPricings_Model",
                table: "ModelPricings",
                column: "Model",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_Code",
                table: "RedeemCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_CreatedByUserId",
                table: "RedeemCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_ExpiresAt",
                table: "RedeemCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_IsEnabled",
                table: "RedeemCodes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_IsUsed",
                table: "RedeemCodes",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_IsUsed_IsEnabled_ExpiresAt",
                table: "RedeemCodes",
                columns: new[] { "IsUsed", "IsEnabled", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_Type",
                table: "RedeemCodes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemCodes_UsedByUserId",
                table: "RedeemCodes",
                column: "UsedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_ApiKeyId",
                table: "RequestLogs",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Model",
                table: "RequestLogs",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Platform",
                table: "RequestLogs",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestDate",
                table: "RequestLogs",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestDate_ApiKeyId",
                table: "RequestLogs",
                columns: new[] { "RequestDate", "ApiKeyId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestDate_Model",
                table: "RequestLogs",
                columns: new[] { "RequestDate", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestDate_UserId",
                table: "RequestLogs",
                columns: new[] { "RequestDate", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestStartTime",
                table: "RequestLogs",
                column: "RequestStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestStartTime_RequestHour",
                table: "RequestLogs",
                columns: new[] { "RequestStartTime", "RequestHour" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Status",
                table: "RequestLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_UserId",
                table: "RequestLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsSystem",
                table: "Roles",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_ApiKeyId",
                table: "StatisticsSnapshots",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_Model",
                table: "StatisticsSnapshots",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotDate",
                table: "StatisticsSnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType",
                table: "StatisticsSnapshots",
                column: "SnapshotType");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType_SnapshotDate",
                table: "StatisticsSnapshots",
                columns: new[] { "SnapshotType", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType_SnapshotDate_ApiKeyId",
                table: "StatisticsSnapshots",
                columns: new[] { "SnapshotType", "SnapshotDate", "ApiKeyId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType_SnapshotDate_Model",
                table: "StatisticsSnapshots",
                columns: new[] { "SnapshotType", "SnapshotDate", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType_SnapshotDate_SnapshotHour",
                table: "StatisticsSnapshots",
                columns: new[] { "SnapshotType", "SnapshotDate", "SnapshotHour" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_SnapshotType_SnapshotDate_UserId",
                table: "StatisticsSnapshots",
                columns: new[] { "SnapshotType", "SnapshotDate", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSnapshots_UserId",
                table: "StatisticsSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_AccountId",
                table: "UserAccountBindings",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_BindingType",
                table: "UserAccountBindings",
                column: "BindingType");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_Priority",
                table: "UserAccountBindings",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_User_Priority_Active",
                table: "UserAccountBindings",
                columns: new[] { "UserId", "Priority", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_UserId",
                table: "UserAccountBindings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserAccountBindings_User_Account",
                table: "UserAccountBindings",
                columns: new[] { "UserId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_CreatedAt",
                table: "UserLoginHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_UserId",
                table: "UserLoginHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_UserId_CreatedAt",
                table: "UserLoginHistories",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_InvitationCode",
                table: "Users",
                column: "InvitationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_InvitedByUserId",
                table: "Users",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Provider_ProviderId",
                table: "Users",
                columns: new[] { "Provider", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_LastUsedAt",
                table: "Wallets",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Status",
                table: "Wallets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RequestLogId",
                table: "WalletTransactions",
                column: "RequestLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Status",
                table: "WalletTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_TransactionType",
                table: "WalletTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId",
                table: "WalletTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "WalletId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "InvitationRecords");

            migrationBuilder.DropTable(
                name: "InvitationSettings");

            migrationBuilder.DropTable(
                name: "ModelPricings");

            migrationBuilder.DropTable(
                name: "RedeemCodes");

            migrationBuilder.DropTable(
                name: "StatisticsSnapshots");

            migrationBuilder.DropTable(
                name: "UserAccountBindings");

            migrationBuilder.DropTable(
                name: "UserLoginHistories");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "RequestLogs");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
