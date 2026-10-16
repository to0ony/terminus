using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    primary_color = table.Column<string>(type: "text", nullable: true),
                    primary_text_color = table.Column<string>(type: "text", nullable: true),
                    public_url = table.Column<string>(type: "text", nullable: true),
                    public_gtfs_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    logo = table.Column<byte[]>(type: "bytea", nullable: true),
                    logo_content_type = table.Column<string>(type: "text", nullable: true),
                    logo_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                    table.CheckConstraint("ck_settings_logo", "(logo IS NULL) = (logo_content_type IS NULL) AND (logo IS NULL) = (logo_updated_at IS NULL)");
                    table.CheckConstraint("ck_settings_primary_color", "primary_color ~ '^[0-9A-F]{6}$'");
                    table.CheckConstraint("ck_settings_primary_text_color", "primary_text_color ~ '^[0-9A-F]{6}$'");
                    table.CheckConstraint("ck_settings_single_row", "id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings");
        }
    }
}
