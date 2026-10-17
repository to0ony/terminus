using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "route_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    direction_id = table.Column<short>(type: "smallint", nullable: true),
                    headsign = table.Column<string>(type: "text", nullable: true),
                    shape_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_variants", x => x.id);
                    table.CheckConstraint("ck_route_variants_direction", "direction_id IS NULL OR direction_id IN (0, 1)");
                    table.CheckConstraint("ck_route_variants_source", "source IN (0, 1)");
                    table.ForeignKey(
                        name: "fk_route_variants_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_route_variants_shapes_shape_id",
                        column: x => x.shape_id,
                        principalTable: "shapes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "route_variant_stops",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    travel_time = table.Column<int>(type: "integer", nullable: false),
                    dwell_time = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_variant_stops", x => new { x.variant_id, x.sequence });
                    table.CheckConstraint("ck_route_variant_stops_times", "travel_time >= 0 AND dwell_time >= 0");
                    table.ForeignKey(
                        name: "fk_route_variant_stops_route_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "route_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_route_variant_stops_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_trips_variant_id",
                table: "trips",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_variant_stops_stop_id",
                table: "route_variant_stops",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_variants_route_id",
                table: "route_variants",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_variants_shape_id",
                table: "route_variants",
                column: "shape_id");

            migrationBuilder.AddForeignKey(
                name: "fk_trips_route_variants_variant_id",
                table: "trips",
                column: "variant_id",
                principalTable: "route_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_route_variants_variant_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "route_variant_stops");

            migrationBuilder.DropTable(
                name: "route_variants");

            migrationBuilder.DropIndex(
                name: "ix_trips_variant_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "trips");
        }
    }
}
