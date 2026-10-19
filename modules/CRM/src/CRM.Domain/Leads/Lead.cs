using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace CRM.Leads;

public class Lead : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

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

    // Dropdown Foreign Keys
    public Guid CustomerTypeId { get; set; }
    public Guid LeadSourceId { get; set; }
    public Guid LeadStageId { get; set; }
    public Guid ServiceId { get; set; }

    // Assigned User (ABP Identity User ID)
    public Guid? AssignedUserId { get; set; }
    public string? ExternalId { get; set; }   // sheet row key

    // Parameterless constructor required by ORM / ABP
    protected Lead()
    {
    }

    // Parameterized constructor including Id and all Lead attributes
    public Lead(
        Guid id,
        string name,
        Guid customerTypeId,
        Guid leadSourceId,
        Guid leadStageId,
        Guid serviceId,
        string companyName = "",
        string email = "",
        string phone = "",
        decimal estimatedBudget = 0m,
        string? remarks = null,
        Guid? assignedUserId = null,
        DateTime? expectedCloseDate = null,
        DateTime? nextFollowUpDate = null,
        string? externalId = null)
        : base(id)
    {
        Name = name;
        CustomerTypeId = customerTypeId;
        LeadSourceId = leadSourceId;
        LeadStageId = leadStageId;
        ServiceId = serviceId;
        CompanyName = companyName;
        Email = email;
        Phone = phone;
        EstimatedBudget = estimatedBudget;
        Remarks = remarks;
        AssignedUserId = assignedUserId;
        ExpectedCloseDate = expectedCloseDate;
        NextFollowUpDate = nextFollowUpDate;
        ExternalId = externalId;
    }
}