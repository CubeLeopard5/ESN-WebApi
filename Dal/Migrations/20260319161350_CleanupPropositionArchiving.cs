using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class CleanupPropositionArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Hard delete old soft-deleted propositions (IsDeleted=true was old soft-delete)
            migrationBuilder.Sql("DELETE FROM Propositions WHERE IsDeleted = 1");

            // 2. Transfer archived state: IsArchived=true → IsDeleted=true
            migrationBuilder.Sql("UPDATE Propositions SET IsDeleted = 1, DeletedAt = ArchivedAt WHERE IsArchived = 1");

            // 3. Drop the now-unused columns
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Propositions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Propositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Propositions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Propositions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
