using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTripDepartures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "departure_id",
                table: "trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_departure_id",
                table: "trips",
                column: "departure_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_trips_departure_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "departure_id",
                table: "trips");
        }
    }
}
