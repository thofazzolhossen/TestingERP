using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_Messenger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmMessengerConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PageScopedUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LastMessageText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastMessageTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnreadCount = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMessengerConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmMessengerMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacebookMessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsFromCustomer = table.Column<bool>(type: "bit", nullable: false),
                    SentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMessengerMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMessengerMessages_CrmMessengerConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CrmMessengerConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMessengerConversations_TenantId_PageScopedUserId",
                table: "CrmMessengerConversations",
                columns: new[] { "TenantId", "PageScopedUserId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMessengerMessages_ConversationId_SentTime",
                table: "CrmMessengerMessages",
                columns: new[] { "ConversationId", "SentTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMessengerMessages_FacebookMessageId",
                table: "CrmMessengerMessages",
                column: "FacebookMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmMessengerMessages");

            migrationBuilder.DropTable(
                name: "CrmMessengerConversations");
        }
    }
}
