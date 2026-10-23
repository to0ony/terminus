using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "Europe/Zagreb"),
                    lang = table.Column<string>(type: "text", nullable: false, defaultValue: "hr"),
                    phone = table.Column<string>(type: "text", nullable: true),
                    fare_url = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
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
                    setup_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    public_code = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    tts_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    zone_id = table.Column<string>(type: "text", nullable: true),
                    url = table.Column<string>(type: "text", nullable: true),
                    location_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    parent_station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    platform_code = table.Column<string>(type: "text", nullable: true),
                    wheelchair_boarding = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stops", x => x.id);
                    table.CheckConstraint("ck_stops_coordinates", "location_type IN (3, 4) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                    table.CheckConstraint("ck_stops_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("ck_stops_location_type", "location_type IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("ck_stops_longitude", "longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_stops_not_own_parent", "parent_station_id IS NULL OR parent_station_id <> id");
                    table.CheckConstraint("ck_stops_wheelchair", "wheelchair_boarding IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "fk_stops_stops_parent_station_id",
                        column: x => x.parent_station_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "admin_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_admin_user_logins_admin_users_user_id",
                        column: x => x.user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_admin_user_tokens_admin_users_user_id",
                        column: x => x.user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    url = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "char(6)", nullable: true),
                    text_color = table.Column<string>(type: "char(6)", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    continuous_pickup = table.Column<short>(type: "smallint", nullable: true),
                    continuous_drop_off = table.Column<short>(type: "smallint", nullable: true),
                    network_id = table.Column<string>(type: "text", nullable: true),
                    time_mode = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                    table.CheckConstraint("ck_routes_color", "color IS NULL OR color ~ '^[0-9A-Fa-f]{6}$'");
                    table.CheckConstraint("ck_routes_continuous_drop_off", "continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_routes_continuous_pickup", "continuous_pickup IS NULL OR continuous_pickup IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_routes_name", "short_name IS NOT NULL OR long_name IS NOT NULL");
                    table.CheckConstraint("ck_routes_text_color", "text_color IS NULL OR text_color ~ '^[0-9A-Fa-f]{6}$'");
                    table.CheckConstraint("ck_routes_time_mode", "time_mode IN (0, 1)");
                    table.ForeignKey(
                        name: "fk_routes_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                });

            migrationBuilder.CreateTable(
                name: "season_days",
                columns: table => new
                {
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<short>(type: "smallint", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_days", x => new { x.season_id, x.date });
                    table.CheckConstraint("ck_season_days_mode", "mode IN (0, 1, 2)");
                    table.CheckConstraint("ck_season_days_source", "source IN (0, 1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "season_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    replaced_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_periods", x => x.id);
                    table.CheckConstraint("ck_season_periods_dates", "end_date >= start_date");
                    table.CheckConstraint("ck_season_periods_mode", "mode IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "service_calendars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
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
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_calendars", x => x.id);
                    table.CheckConstraint("ck_service_calendars_dates", "end_date >= start_date");
                });

            migrationBuilder.CreateTable(
                name: "timetable_seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    holiday_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    holiday_mode = table.Column<short>(type: "smallint", nullable: false),
                    holiday_calendar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timetable_seasons", x => x.id);
                    table.CheckConstraint("ck_timetable_seasons_dates", "end_date >= start_date");
                    table.CheckConstraint("ck_timetable_seasons_holiday_mode", "holiday_mode IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "fk_timetable_seasons_service_calendars_holiday_calendar_id",
                        column: x => x.holiday_calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    headsign = table.Column<string>(type: "text", nullable: true),
                    short_name = table.Column<string>(type: "text", nullable: true),
                    direction_id = table.Column<short>(type: "smallint", nullable: true),
                    block_id = table.Column<string>(type: "text", nullable: true),
                    shape_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    departure_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "fk_trips_route_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "route_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    table.ForeignKey(
                        name: "fk_trips_shapes_shape_id",
                        column: x => x.shape_id,
                        principalTable: "shapes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    continuous_pickup = table.Column<short>(type: "smallint", nullable: true),
                    continuous_drop_off = table.Column<short>(type: "smallint", nullable: true),
                    shape_dist_traveled = table.Column<double>(type: "double precision", nullable: true),
                    timepoint = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_times", x => new { x.trip_id, x.stop_sequence });
                    table.CheckConstraint("ck_stop_times_arrival", "arrival_time BETWEEN 0 AND 172800");
                    table.CheckConstraint("ck_stop_times_continuous_drop_off", "continuous_drop_off IS NULL OR continuous_drop_off IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_continuous_pickup", "continuous_pickup IS NULL OR continuous_pickup IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_departure", "departure_time BETWEEN 0 AND 172800");
                    table.CheckConstraint("ck_stop_times_dropoff", "drop_off_type IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_order", "departure_time >= arrival_time");
                    table.CheckConstraint("ck_stop_times_pickup", "pickup_type IN (0, 1, 2, 3)");
                    table.CheckConstraint("ck_stop_times_sequence", "stop_sequence >= 0");
                    table.CheckConstraint("ck_stop_times_shape_dist", "shape_dist_traveled IS NULL OR shape_dist_traveled >= 0");
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
                name: "ix_admin_user_claims_user_id",
                table: "admin_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_user_logins_user_id",
                table: "admin_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_normalized_email",
                table: "admin_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "uq_admin_users_normalized_user_name",
                table: "admin_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_agencies_code",
                table: "agencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_dates_date",
                table: "calendar_dates",
                column: "date");

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

            migrationBuilder.CreateIndex(
                name: "ix_routes_agency_id",
                table: "routes",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "uq_routes_code",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_season_days_calendar_id",
                table: "season_days",
                column: "calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_periods_calendar_id",
                table: "season_periods",
                column: "calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_periods_replaced_calendar_id",
                table: "season_periods",
                column: "replaced_calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_periods_season_id",
                table: "season_periods",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_calendars_season_id",
                table: "service_calendars",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "uq_service_calendars_code",
                table: "service_calendars",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_shapes_code",
                table: "shapes",
                column: "code",
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
                name: "uq_stops_code",
                table: "stops",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_timetable_seasons_holiday_calendar_id",
                table: "timetable_seasons",
                column: "holiday_calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_departure_id",
                table: "trips",
                column: "departure_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_route_id",
                table: "trips",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_service_calendar_id",
                table: "trips",
                column: "service_calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_shape_id",
                table: "trips",
                column: "shape_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_variant_id",
                table: "trips",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_trips_code",
                table: "trips",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_calendar_dates_service_calendars_service_calendar_id",
                table: "calendar_dates",
                column: "service_calendar_id",
                principalTable: "service_calendars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_days_service_calendars_calendar_id",
                table: "season_days",
                column: "calendar_id",
                principalTable: "service_calendars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_days_timetable_seasons_season_id",
                table: "season_days",
                column: "season_id",
                principalTable: "timetable_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_periods_service_calendars_calendar_id",
                table: "season_periods",
                column: "calendar_id",
                principalTable: "service_calendars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_periods_service_calendars_replaced_calendar_id",
                table: "season_periods",
                column: "replaced_calendar_id",
                principalTable: "service_calendars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_season_periods_timetable_seasons_season_id",
                table: "season_periods",
                column: "season_id",
                principalTable: "timetable_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_calendars_timetable_seasons_season_id",
                table: "service_calendars",
                column: "season_id",
                principalTable: "timetable_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_timetable_seasons_service_calendars_holiday_calendar_id",
                table: "timetable_seasons");

            migrationBuilder.DropTable(
                name: "admin_user_claims");

            migrationBuilder.DropTable(
                name: "admin_user_logins");

            migrationBuilder.DropTable(
                name: "admin_user_tokens");

            migrationBuilder.DropTable(
                name: "calendar_dates");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "fare_rules");

            migrationBuilder.DropTable(
                name: "feed_info");

            migrationBuilder.DropTable(
                name: "route_variant_stops");

            migrationBuilder.DropTable(
                name: "season_days");

            migrationBuilder.DropTable(
                name: "season_periods");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "shape_points");

            migrationBuilder.DropTable(
                name: "stop_times");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "fares");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "route_variants");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "shapes");

            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "service_calendars");

            migrationBuilder.DropTable(
                name: "timetable_seasons");
        }
    }
}
