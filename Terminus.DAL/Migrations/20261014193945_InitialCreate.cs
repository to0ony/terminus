using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "Europe/Zagreb"),
                    lang = table.Column<string>(type: "text", nullable: false, defaultValue: "hr"),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    short_name = table.Column<string>(type: "text", nullable: true),
                    long_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    route_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)3),
                    color = table.Column<string>(type: "char(6)", nullable: true),
                    text_color = table.Column<string>(type: "char(6)", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                    table.CheckConstraint("ck_routes_color", "color IS NULL OR color ~ '^[0-9A-Fa-f]{6}$'");
                    table.CheckConstraint("ck_routes_name", "short_name IS NOT NULL OR long_name IS NOT NULL");
                    table.CheckConstraint("ck_routes_text_color", "text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$'");
                    table.ForeignKey(
                        name: "fk_routes_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_calendars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    monday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tuesday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    wednesday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    thursday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    friday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    saturday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sunday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_calendars", x => x.id);
                    table.CheckConstraint("ck_service_calendars_dates", "end_date >= start_date");
                    table.ForeignKey(
                        name: "fk_service_calendars_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    location_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    parent_station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    platform_code = table.Column<string>(type: "text", nullable: true),
                    wheelchair_boarding = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stops", x => x.id);
                    table.CheckConstraint("ck_stops_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_stops_location_type", "location_type IN (0, 1)");
                    table.CheckConstraint("ck_stops_longitude", "longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_stops_not_own_parent", "parent_station_id IS NULL OR parent_station_id <> id");
                    table.CheckConstraint("ck_stops_wheelchair", "wheelchair_boarding IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "fk_stops_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stops_stops_parent_station_id",
                        column: x => x.parent_station_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "calendar_dates",
                columns: table => new
                {
                    service_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    exception_type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_dates", x => new { x.service_calendar_id, x.date });
                    table.CheckConstraint("ck_calendar_dates_type", "exception_type IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_calendar_dates_service_calendars_service_calendar_id",
                        column: x => x.service_calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    headsign = table.Column<string>(type: "text", nullable: true),
                    short_name = table.Column<string>(type: "text", nullable: true),
                    direction_id = table.Column<short>(type: "smallint", nullable: true),
                    block_id = table.Column<string>(type: "text", nullable: true),
                    wheelchair_accessible = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    bikes_allowed = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                    table.CheckConstraint("ck_trips_bikes", "bikes_allowed IN (0, 1, 2)");
                    table.CheckConstraint("ck_trips_direction", "direction_id IS NULL OR direction_id IN (0, 1)");
                    table.CheckConstraint("ck_trips_wheelchair", "wheelchair_accessible IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "fk_trips_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trips_service_calendars_service_calendar_id",
                        column: x => x.service_calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stop_times",
                columns: table => new
                {
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arrival_time = table.Column<int>(type: "integer", nullable: false),
                    departure_time = table.Column<int>(type: "integer", nullable: false),
                    stop_headsign = table.Column<string>(type: "text", nullable: true),
                    pickup_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    drop_off_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    timepoint = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_times", x => new { x.trip_id, x.stop_sequence });
                    table.CheckConstraint("ck_stop_times_arrival", "arrival_time BETWEEN 0 AND 172800");
                    table.CheckConstraint("ck_stop_times_departure", "departure_time BETWEEN 0 AND 172800");
                    table.CheckConstraint("ck_stop_times_dropoff", "drop_off_type IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_order", "departure_time >= arrival_time");
                    table.CheckConstraint("ck_stop_times_pickup", "pickup_type IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_sequence", "stop_sequence >= 0");
                    table.ForeignKey(
                        name: "fk_stop_times_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_stop_times_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_dates_date",
                table: "calendar_dates",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "uq_routes_agency_code",
                table: "routes",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_service_calendars_agency_code",
                table: "service_calendars",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stop_times_stop_departure",
                table: "stop_times",
                columns: new[] { "stop_id", "departure_time" });

            migrationBuilder.CreateIndex(
                name: "ix_stops_parent_station_id",
                table: "stops",
                column: "parent_station_id");

            migrationBuilder.CreateIndex(
                name: "uq_stops_agency_code",
                table: "stops",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_route_id",
                table: "trips",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_service_calendar_id",
                table: "trips",
                column: "service_calendar_id");

            migrationBuilder.CreateIndex(
                name: "uq_trips_agency_code",
                table: "trips",
                columns: new[] { "agency_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_dates");

            migrationBuilder.DropTable(
                name: "stop_times");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "service_calendars");

            migrationBuilder.DropTable(
                name: "agencies");
        }
    }
}
