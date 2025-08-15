using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudeCodeProxy.EntityFrameworkCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountBindings",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultAccountId",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentUsers",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "success"),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
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
                name: "UserAccountBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BindingType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "private"),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 50),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_DefaultAccountId",
                table: "ApiKeys",
                column: "DefaultAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Global_Enabled_Status",
                table: "Accounts",
                columns: new[] { "IsGlobal", "IsEnabled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsGlobal",
                table: "Accounts",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "UserAccountBindings");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_DefaultAccountId",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Global_Enabled_Status",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_IsGlobal",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountBindings",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "DefaultAccountId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentUsers",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Accounts");
        }
    }
}
