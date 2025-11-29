using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sitemind_shared.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            // Insert existing organization IDs from Users and Websites tables
            migrationBuilder.Sql(@"
                INSERT INTO ""Organizations"" (""Id"", ""Name"", ""CreatedAt"")
                SELECT DISTINCT ""OrganizationId"", 'Organization ' || ""OrganizationId""::text, NOW()
                FROM ""Users""
                WHERE ""OrganizationId"" NOT IN (SELECT ""Id"" FROM ""Organizations"")
                UNION
                SELECT DISTINCT ""OrganizationId"", 'Organization ' || ""OrganizationId""::text, NOW()
                FROM ""Websites""
                WHERE ""OrganizationId"" NOT IN (SELECT ""Id"" FROM ""Organizations"");
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Websites_OrganizationId",
                table: "Websites",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Websites_Organizations_OrganizationId",
                table: "Websites",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Websites_Organizations_OrganizationId",
                table: "Websites");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Websites_OrganizationId",
                table: "Websites");
        }
    }
}
