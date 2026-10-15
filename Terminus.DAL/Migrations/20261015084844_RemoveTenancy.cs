using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_routes_agencies_agency_id",
                table: "routes");

            migrationBuilder.DropForeignKey(
                name: "fk_service_calendars_agencies_agency_id",
                table: "service_calendars");

            migrationBuilder.DropForeignKey(
                name: "fk_stops_agencies_agency_id",
                table: "stops");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_agencies_agency_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "uq_trips_agency_code",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "uq_stops_agency_code",
                table: "stops");

            migrationBuilder.DropIndex(
                name: "uq_service_calendars_agency_code",
                table: "service_calendars");

            migrationBuilder.DropIndex(
                name: "uq_routes_agency_code",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "agency_id",
                table: "service_calendars");

            migrationBuilder.CreateIndex(
                name: "uq_trips_code",
                table: "trips",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stops_code",
                table: "stops",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_service_calendars_code",
                table: "service_calendars",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routes_agency_id",
                table: "routes",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "uq_routes_code",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_routes_agencies_agency_id",
                table: "routes",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_routes_agencies_agency_id",
                table: "routes");

            migrationBuilder.DropIndex(
                name: "uq_trips_code",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "uq_stops_code",
                table: "stops");

            migrationBuilder.DropIndex(
                name: "uq_service_calendars_code",
                table: "service_calendars");

            migrationBuilder.DropIndex(
                name: "ix_routes_agency_id",
                table: "routes");

            migrationBuilder.DropIndex(
                name: "uq_routes_code",
                table: "routes");

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "trips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "stops",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "agency_id",
                table: "service_calendars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "uq_trips_agency_code",
                table: "trips",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stops_agency_code",
                table: "stops",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_service_calendars_agency_code",
                table: "service_calendars",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_routes_agency_code",
                table: "routes",
                columns: new[] { "agency_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_routes_agencies_agency_id",
                table: "routes",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_calendars_agencies_agency_id",
                table: "service_calendars",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stops_agencies_agency_id",
                table: "stops",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_agencies_agency_id",
                table: "trips",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
