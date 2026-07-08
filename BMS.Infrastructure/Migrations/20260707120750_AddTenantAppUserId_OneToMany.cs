using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAppUserId_OneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Tenants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AppUserId",
                table: "Tenants",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_AspNetUsers_AppUserId",
                table: "Tenants",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_AspNetUsers_AppUserId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_AppUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Tenants");
        }
    }
}
