using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StationaryManagement1.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsToRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportsToRoleId",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ReportsToRoleId",
                table: "Roles",
                column: "ReportsToRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Roles_ReportsToRoleId",
                table: "Roles",
                column: "ReportsToRoleId",
                principalTable: "Roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Roles_ReportsToRoleId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_ReportsToRoleId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ReportsToRoleId",
                table: "Roles");
        }
    }
}

