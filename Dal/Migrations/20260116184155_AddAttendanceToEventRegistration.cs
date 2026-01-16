using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceToEventRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceStatus",
                table: "EventRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceValidatedAt",
                table: "EventRegistrations",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceValidatedById",
                table: "EventRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_AttendanceValidatedById",
                table: "EventRegistrations",
                column: "AttendanceValidatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AttendanceValidator",
                table: "EventRegistrations",
                column: "AttendanceValidatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AttendanceValidator",
                table: "EventRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_AttendanceValidatedById",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "AttendanceValidatedAt",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "AttendanceValidatedById",
                table: "EventRegistrations");
        }
    }
}
