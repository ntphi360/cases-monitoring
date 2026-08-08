using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoSoMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProcedureFieldAndOrganizationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "Cases",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserProcedureFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProcedureFieldId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProcedureFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProcedureFields_ProcedureFields_ProcedureFieldId",
                        column: x => x.ProcedureFieldId,
                        principalTable: "ProcedureFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProcedureFields_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProcedureFields_ProcedureFieldId",
                table: "UserProcedureFields",
                column: "ProcedureFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProcedureFields_UserId_ProcedureFieldId",
                table: "UserProcedureFields",
                columns: new[] { "UserId", "ProcedureFieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProcedureFields");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "Cases");
        }
    }
}
