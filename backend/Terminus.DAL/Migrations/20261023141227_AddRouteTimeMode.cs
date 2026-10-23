using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteTimeMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "time_mode",
                table: "routes",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_routes_time_mode",
                table: "routes",
                sql: "time_mode IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_routes_time_mode",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "time_mode",
                table: "routes");
        }
    }
}
