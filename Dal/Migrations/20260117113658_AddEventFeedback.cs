using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackDeadline",
                table: "Events",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackFormData",
                table: "Events",
                type: "varchar(max)",
                unicode: false,
                maxLength: 100000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackData = table.Column<string>(type: "varchar(max)", unicode: false, maxLength: 100000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EventFeedback__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventFeedback_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventFeedback_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventFeedbacks_EventId",
                table: "EventFeedbacks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "UQ_EventFeedback_User_Event",
                table: "EventFeedbacks",
                columns: new[] { "UserId", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventFeedbacks");

            migrationBuilder.DropColumn(
                name: "FeedbackDeadline",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "FeedbackFormData",
                table: "Events");
        }
    }
}
