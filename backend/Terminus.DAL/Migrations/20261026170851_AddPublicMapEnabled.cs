using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicMapEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "public_map_enabled",
                table: "settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "public_map_enabled",
                table: "settings");
        }
    }
}
