using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_Messenger_Attachmentsforpertanent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "CrmMessengerConversations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CrmMessengerPageConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PageName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMessengerPageConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMessengerPageConnections_PageId",
                table: "CrmMessengerPageConnections",
                column: "PageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmMessengerPageConnections");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "CrmMessengerConversations");
        }
    }
}
