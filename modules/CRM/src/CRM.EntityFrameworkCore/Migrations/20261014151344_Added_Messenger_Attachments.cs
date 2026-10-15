using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_Messenger_Attachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentMimeType",
                table: "CrmMessengerMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "CrmMessengerMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AttachmentSize",
                table: "CrmMessengerMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentStoredName",
                table: "CrmMessengerMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "CrmMessengerMessages",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentMimeType",
                table: "CrmMessengerMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "CrmMessengerMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentSize",
                table: "CrmMessengerMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentStoredName",
                table: "CrmMessengerMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "CrmMessengerMessages");
        }
    }
}
