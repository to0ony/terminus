using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ExtendGtfsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stops_location_type",
                table: "stops");

            migrationBuilder.AddColumn<Guid>(
                name: "shape_id",
                table: "trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "longitude",
                table: "stops",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                table: "stops",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "public_code",
                table: "stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tts_name",
                table: "stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                table: "stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zone_id",
                table: "stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "continuous_drop_off",
                table: "stop_times",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "continuous_pickup",
                table: "stop_times",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "shape_dist_traveled",
                table: "stop_times",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "continuous_drop_off",
                table: "routes",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "continuous_pickup",
                table: "routes",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "network_id",
                table: "routes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                table: "routes",
                type: "text",
                nullable: true);

            // Postojeći prijevoznici dobivaju kod jednak Id-u: to je agency_id koji je
            // izvoz dosad pisao, pa se feed ne mijenja.
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "agencies",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE agencies SET code = id::text;");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "agencies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fare_url",
                table: "agencies",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    currency_type = table.Column<string>(type: "char(3)", nullable: false),
                    payment_method = table.Column<short>(type: "smallint", nullable: false),
                    transfers = table.Column<short>(type: "smallint", nullable: true),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_duration = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fares", x => x.id);
                    table.CheckConstraint("ck_fares_currency", "currency_type ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_fares_payment_method", "payment_method IN (0, 1)");
                    table.CheckConstraint("ck_fares_price", "price >= 0");
                    table.CheckConstraint("ck_fares_transfer_duration", "transfer_duration IS NULL OR transfer_duration >= 0");
                    table.CheckConstraint("ck_fares_transfers", "transfers IS NULL OR transfers IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "fk_fares_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feed_info",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    publisher_name = table.Column<string>(type: "text", nullable: false),
                    publisher_url = table.Column<string>(type: "text", nullable: false),
                    lang = table.Column<string>(type: "text", nullable: false),
                    default_lang = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_info", x => x.id);
                    table.CheckConstraint("ck_feed_info_single_row", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "shapes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shapes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fare_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    fare_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_id = table.Column<string>(type: "text", nullable: true),
                    destination_id = table.Column<string>(type: "text", nullable: true),
                    contains_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fare_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_fare_rules_fares_fare_id",
                        column: x => x.fare_id,
                        principalTable: "fares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fare_rules_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shape_points",
                columns: table => new
                {
                    shape_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    dist_traveled = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shape_points", x => new { x.shape_id, x.sequence });
                    table.CheckConstraint("ck_shape_points_dist", "dist_traveled IS NULL OR dist_traveled >= 0");
                    table.CheckConstraint("ck_shape_points_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_shape_points_longitude", "longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_shape_points_sequence", "sequence >= 0");
                    table.ForeignKey(
                        name: "fk_shape_points_shapes_shape_id",
                        column: x => x.shape_id,
                        principalTable: "shapes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trips_shape_id",
                table: "trips",
                column: "shape_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stops_coordinates",
                table: "stops",
                sql: "location_type IN (3, 4) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stops_location_type",
                table: "stops",
                sql: "location_type IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stop_times_continuous_drop_off",
                table: "stop_times",
                sql: "continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stop_times_continuous_pickup",
                table: "stop_times",
                sql: "continuous_pickup IS NULL OR continuous_pickup IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stop_times_shape_dist",
                table: "stop_times",
                sql: "shape_dist_traveled IS NULL OR shape_dist_traveled >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_routes_continuous_drop_off",
                table: "routes",
                sql: "continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_routes_continuous_pickup",
                table: "routes",
                sql: "continuous_pickup IS NULL OR continuous_pickup IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "uq_agencies_code",
                table: "agencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fare_rules_route_id",
                table: "fare_rules",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "uq_fare_rules_fare_sequence",
                table: "fare_rules",
                columns: new[] { "fare_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fares_agency_id",
                table: "fares",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "uq_fares_code",
                table: "fares",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_shapes_code",
                table: "shapes",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_shapes_shape_id",
                table: "trips",
                column: "shape_id",
                principalTable: "shapes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_shapes_shape_id",
                table: "trips");

            migrationBuilder.DropTable(
                name: "fare_rules");

            migrationBuilder.DropTable(
                name: "feed_info");

            migrationBuilder.DropTable(
                name: "shape_points");

            migrationBuilder.DropTable(
                name: "fares");

            migrationBuilder.DropTable(
                name: "shapes");

            migrationBuilder.DropIndex(
                name: "ix_trips_shape_id",
                table: "trips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stops_coordinates",
                table: "stops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stops_location_type",
                table: "stops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stop_times_continuous_drop_off",
                table: "stop_times");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stop_times_continuous_pickup",
                table: "stop_times");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stop_times_shape_dist",
                table: "stop_times");

            migrationBuilder.DropCheckConstraint(
                name: "ck_routes_continuous_drop_off",
                table: "routes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_routes_continuous_pickup",
                table: "routes");

            migrationBuilder.DropIndex(
                name: "uq_agencies_code",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "shape_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "public_code",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "tts_name",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "url",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "zone_id",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "continuous_drop_off",
                table: "stop_times");

            migrationBuilder.DropColumn(
                name: "continuous_pickup",
                table: "stop_times");

            migrationBuilder.DropColumn(
                name: "shape_dist_traveled",
                table: "stop_times");

            migrationBuilder.DropColumn(
                name: "continuous_drop_off",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "continuous_pickup",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "network_id",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "url",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "code",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "fare_url",
                table: "agencies");

            migrationBuilder.AlterColumn<double>(
                name: "longitude",
                table: "stops",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                table: "stops",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stops_location_type",
                table: "stops",
                sql: "location_type IN (0, 1)");
        }
    }
}
