using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActivityDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "activities",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "activities",
                type: "timestamp with time zone",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "activities",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true
            );

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "activities"
            );
        }
    }
}
