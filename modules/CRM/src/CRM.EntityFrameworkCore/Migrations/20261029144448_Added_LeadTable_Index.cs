using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class Added_LeadTable_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_AssignedUserId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "AssignedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_CreationTime",
                table: "CrmLeads",
                columns: new[] { "TenantId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_CustomerTypeId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "CustomerTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_LeadSourceId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "LeadSourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_LeadStageId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "LeadStageId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_NextFollowUpDate",
                table: "CrmLeads",
                columns: new[] { "TenantId", "NextFollowUpDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_TenantId_ServiceId",
                table: "CrmLeads",
                columns: new[] { "TenantId", "ServiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_AssignedUserId",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_CreationTime",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_CustomerTypeId",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_LeadSourceId",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_LeadStageId",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_NextFollowUpDate",
                table: "CrmLeads");

            migrationBuilder.DropIndex(
                name: "IX_CrmLeads_TenantId_ServiceId",
                table: "CrmLeads");
        }
    }
}
