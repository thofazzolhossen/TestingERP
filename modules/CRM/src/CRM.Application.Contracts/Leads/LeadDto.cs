using System;
using Volo.Abp.Application.Dtos;

namespace CRM.Leads;

public class LeadDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public decimal EstimatedBudget { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public string? Remarks { get; set; }

    // Dropdown Display Values
    public Guid CustomerTypeId { get; set; }
    public string CustomerTypeName { get; set; } = default!;

    public Guid LeadSourceId { get; set; }
    public string LeadSourceName { get; set; } = default!;

    public Guid LeadStageId { get; set; }
    public string LeadStageName { get; set; } = default!;

    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;

    // Assigned User Details
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
}