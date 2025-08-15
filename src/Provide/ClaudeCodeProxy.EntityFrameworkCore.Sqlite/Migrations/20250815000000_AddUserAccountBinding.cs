using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudeCodeProxy.EntityFrameworkCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccountBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加Accounts表的新列
            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentUsers",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // 添加ApiKeys表的新列
            migrationBuilder.AddColumn<string>(
                name: "DefaultAccountId",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountBindings",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            // 创建UserAccountBindings表
            migrationBuilder.CreateTable(
                name: "UserAccountBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    BindingType = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "private"),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 50),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccountBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccountBindings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccountBindings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 添加外键约束到Accounts表
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 添加UserAccountBindings的索引
            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_UserId",
                table: "UserAccountBindings",
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

            // 复合唯一索引确保一个用户不能重复绑定同一个账户
            migrationBuilder.CreateIndex(
                name: "IX_UserAccountBindings_User_Account_Unique",
                table: "UserAccountBindings",
                columns: new[] { "UserId", "AccountId" },
                unique: true);

            // 将现有的账户标记为全局账户（向后兼容）
            migrationBuilder.Sql("UPDATE Accounts SET IsGlobal = 1 WHERE IsEnabled = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除UserAccountBindings表
            migrationBuilder.DropTable(
                name: "UserAccountBindings");

            // 删除Accounts表的外键和索引
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts");

            // 删除新添加的列
            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentUsers",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DefaultAccountId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "AccountBindings",
                table: "ApiKeys");
        }
    }
}