using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintCalendarEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Calendars_EventId_Unique",
                table: "Calendars",
                column: "EventId",
                unique: true,
                filter: "[EventId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Calendars_EventId_Unique",
                table: "Calendars");
        }
    }
}
