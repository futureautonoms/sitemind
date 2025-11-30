using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sitemind_shared.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStartedAtToWebsite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "Websites",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "Websites");
        }
    }
}
