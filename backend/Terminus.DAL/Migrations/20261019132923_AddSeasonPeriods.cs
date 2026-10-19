using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_season_days_source",
                table: "season_days");

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
                    table.ForeignKey(
                        name: "fk_season_periods_service_calendars_calendar_id",
                        column: x => x.calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_season_periods_service_calendars_replaced_calendar_id",
                        column: x => x.replaced_calendar_id,
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_season_periods_timetable_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "timetable_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_season_days_source",
                table: "season_days",
                sql: "source IN (0, 1, 2)");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "season_periods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_season_days_source",
                table: "season_days");

            migrationBuilder.AddCheckConstraint(
                name: "ck_season_days_source",
                table: "season_days",
                sql: "source IN (0, 1)");
        }
    }
}
