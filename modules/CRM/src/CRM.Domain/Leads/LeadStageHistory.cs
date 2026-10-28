using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace CRM.Leads;

public class LeadStageHistory : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid LeadId { get; set; }
    public Guid? OldStageId { get; set; }
    public Guid NewStageId { get; set; }
    public string? Remarks { get; set; }

    protected LeadStageHistory() { }

    public LeadStageHistory(
        Guid id,
        Guid leadId,
        Guid? oldStageId,
        Guid newStageId,
        Guid? tenantId = null,
        string? remarks = null) : base(id)
    {
        LeadId = leadId;
        OldStageId = oldStageId;
        NewStageId = newStageId;
        TenantId = tenantId;
        Remarks = remarks;
    }
}