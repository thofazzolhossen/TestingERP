using System;
using Volo.Abp.Application.Dtos;

namespace CRM.Leads;

public class LeadDetailDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public string? ExternalId { get; set; }
    public string? ConcurrencyStamp { get; set; }

    // Contact Information
    public string Name { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    // Business Metadata & Deal Info
    public decimal EstimatedBudget { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public string? Remarks { get; set; }

    // Lookups
    public Guid CustomerTypeId { get; set; }
    public string CustomerTypeName { get; set; } = default!;

    public Guid LeadSourceId { get; set; }
    public string LeadSourceName { get; set; } = default!;

    public Guid LeadStageId { get; set; }
    public string LeadStageName { get; set; } = default!;

    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;

    public Guid? AssignedUserId { get; set; }
    public string AssignedUserName { get; set; } = default!;

    // Resolved Audit User Names
    public string CreatorUserName { get; set; } = "System";
    public string LastModifierUserName { get; set; } = "-";

    public bool IsConvertedToDeal { get; set; }
    public DateTime? ConvertedToDealTime { get; set; }
}