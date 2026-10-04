using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace CRM.LeadSources;

public class LeadSource : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    protected LeadSource()
    {
    }

    public LeadSource(Guid id, string name, string? description = null, bool isActive = true)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
    }
}