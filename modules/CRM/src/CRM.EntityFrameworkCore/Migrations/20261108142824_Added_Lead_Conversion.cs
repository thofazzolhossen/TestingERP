using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_Lead_Conversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedToDealTime",
                table: "CrmLeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConvertedToDeal",
                table: "CrmLeads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_IsConvertedToDeal",
                table: "CrmLeads",
                columns: new[] { "TenantId", "IsConvertedToDeal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_IsConvertedToDeal",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "ConvertedToDealTime",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "IsConvertedToDeal",
                table: "CrmLeads");
        }
    }
}
