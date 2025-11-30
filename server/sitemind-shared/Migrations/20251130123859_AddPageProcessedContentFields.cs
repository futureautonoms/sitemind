using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sitemind_shared.Migrations
{
    /// <inheritdoc />
    public partial class AddPageProcessedContentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeywordsJson",
                table: "Pages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarkdownContent",
                table: "Pages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Pages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeywordsJson",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "MarkdownContent",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Pages");
        }
    }
}
