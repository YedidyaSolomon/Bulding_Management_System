using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitReservedForTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedForTenantId",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_ReservedForTenantId",
                table: "Units",
                column: "ReservedForTenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Tenants_ReservedForTenantId",
                table: "Units",
                column: "ReservedForTenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Tenants_ReservedForTenantId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_ReservedForTenantId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ReservedForTenantId",
                table: "Units");
        }
    }
}
