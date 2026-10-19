using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace CRM.Sheets;

public class SheetConnection : AggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string ConnectionKey { get; set; } = default!;   // public id, unique
    public string Secret { get; set; } = default!;          // stored ENCRYPTED

    protected SheetConnection() { }
    public SheetConnection(Guid id, Guid? tenantId, string key, string encSecret) : base(id)
    {
        TenantId = tenantId; ConnectionKey = key; Secret = encSecret;
    }
}