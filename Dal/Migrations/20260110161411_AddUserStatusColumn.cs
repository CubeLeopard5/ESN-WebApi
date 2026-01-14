using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ajouter la colonne Status avec valeur par défaut 0 (Pending)
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Mettre tous les utilisateurs existants en statut Approved (1)
            // Car ils étaient déjà actifs avant cette feature
            migrationBuilder.Sql(
                @"UPDATE Users
                  SET Status = 1
                  WHERE Status = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");
        }
    }
}
