using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoSoMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCaseAndProcedureSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Procedures",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Procedures])
                   AND NOT EXISTS (SELECT 1 FROM [Departments])
                BEGIN
                    THROW 51000, 'Cannot assign Procedure.DepartmentId because no Department exists.', 1;
                END;

                UPDATE [Procedures]
                SET [DepartmentId] = (
                    SELECT TOP (1) [Id]
                    FROM [Departments]
                    ORDER BY CASE WHEN [Code] = 'ROOT' THEN 0 ELSE 1 END, [Id]);
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Procedures",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantName",
                table: "Cases",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingDays",
                table: "Cases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procedures_DepartmentId",
                table: "Procedures",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procedures_Departments_DepartmentId",
                table: "Procedures",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procedures_Departments_DepartmentId",
                table: "Procedures");

            migrationBuilder.DropIndex(
                name: "IX_Procedures_DepartmentId",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "ApplicantName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AppointmentDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ProcessingDays",
                table: "Cases");
        }
    }
}
