using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_Sheet_Sync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "CrmLeads",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CrmSheetConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConnectionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSheetConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_ExternalId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmSheetConnections_ConnectionKey",
                table: "CrmSheetConnections",
                column: "ConnectionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmSheetConnections");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_ExternalId",
                table: "CrmLeads");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "CrmLeads");
        }
    }
}
