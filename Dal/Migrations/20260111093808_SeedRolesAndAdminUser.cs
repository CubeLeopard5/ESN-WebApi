using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CanCreateEvents", "CanCreateUsers", "CanDeleteEvents", "CanDeleteUsers", "CanModifyEvents", "CanModifyUsers", "Name" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, true, "Admin" },
                    { 2, false, false, false, false, false, false, "User" },
                    { 3, true, false, false, false, true, true, "Moderator" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BirthDate", "CreatedAt", "Email", "ESNCardNumber", "FirstName", "LastLoginAt", "LastName", "PasswordHash", "PhoneNumber", "RoleId", "Status", "StudentType", "TransportPass", "UniversityName" },
                values: new object[] { 1, new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 11, 0, 0, 0, 0, DateTimeKind.Utc), "admin@esn.ch", null, "Admin", new DateTime(2026, 1, 11, 0, 0, 0, 0, DateTimeKind.Utc), "ESN", "AQAAAAIAAYagAAAAEHqO8hF7xJ0L3yKjMXH5ZF7wVvN0KQCqBXzP8x5MhGtY7bR3VjKqW8fT9nC2Lm1A==", "+41 00 000 00 00", 1, 1, "esn_member", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
