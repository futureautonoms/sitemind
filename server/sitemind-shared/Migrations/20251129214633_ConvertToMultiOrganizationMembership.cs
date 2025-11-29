using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sitemind_shared.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToMultiOrganizationMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create UserOrganizationMemberships table first
            migrationBuilder.CreateTable(
                name: "UserOrganizationMemberships",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizationMemberships", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_UserOrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Migrate existing User-Organization relationships to UserOrganizationMemberships
            migrationBuilder.Sql(@"
                INSERT INTO ""UserOrganizationMemberships"" (""UserId"", ""OrganizationId"", ""Role"", ""JoinedAt"")
                SELECT ""Id"", ""OrganizationId"", 'admin', ""CreatedAt""
                FROM ""Users""
                WHERE ""OrganizationId"" IS NOT NULL;
            ");

            // Step 3: Drop foreign key and indexes
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username_OrganizationId",
                table: "Users");

            // Step 4: Remove OrganizationId column from Users
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");

            // Step 5: Create new global unique indexes for Email and Username
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // Step 6: Create indexes for UserOrganizationMemberships
            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_OrganizationId",
                table: "UserOrganizationMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_UserId",
                table: "UserOrganizationMemberships",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_OrganizationId",
                table: "Users",
                columns: new[] { "Email", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username_OrganizationId",
                table: "Users",
                columns: new[] { "Username", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
