using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetableSeasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "season_id",
                table: "service_calendars",
                type: "uuid",
                nullable: true);

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
                    table.CheckConstraint("ck_season_days_source", "source IN (0, 1)");
                    table.ForeignKey(
                        name: "fk_season_days_service_calendars_calendar_id",
                        column: x => x.calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_season_days_timetable_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "timetable_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_calendars_season_id",
                table: "service_calendars",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_days_calendar_id",
                table: "season_days",
                column: "calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_timetable_seasons_holiday_calendar_id",
                table: "timetable_seasons",
                column: "holiday_calendar_id");

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
                name: "fk_service_calendars_timetable_seasons_season_id",
                table: "service_calendars");

            migrationBuilder.DropTable(
                name: "season_days");

            migrationBuilder.DropTable(
                name: "timetable_seasons");

            migrationBuilder.DropIndex(
                name: "ix_service_calendars_season_id",
                table: "service_calendars");

            migrationBuilder.DropColumn(
                name: "season_id",
                table: "service_calendars");
        }
    }
}
