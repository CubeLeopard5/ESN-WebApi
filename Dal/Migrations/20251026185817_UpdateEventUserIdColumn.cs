using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Admins__3214EC07C94A3654", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CanCreateEvents = table.Column<bool>(type: "bit", nullable: false),
                    CanModifyEvents = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteEvents = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateUsers = table.Column<bool>(type: "bit", nullable: false),
                    CanModifyUsers = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteUsers = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ESNCardNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    UniversityName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    StudentType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TransportPass = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    RoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3214EC076FF3256C", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    EventfrogLink = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    SurveyJsData = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    AdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Events__3214EC079399E819", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Propositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    VotesUp = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VotesDown = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Propositions__3214EC07", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Propositions_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "registered")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EventReg__3214EC07FA5EB91B", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Registrations_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Admins__A9D105348FC7749F",
                table: "Admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId",
                table: "EventRegistrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "UQ_Registration",
                table: "EventRegistrations",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_AdminId",
                table: "Events",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserId",
                table: "Events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Propositions_UserId",
                table: "Propositions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D105340768CBC8",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrations");

            migrationBuilder.DropTable(
                name: "Propositions");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
