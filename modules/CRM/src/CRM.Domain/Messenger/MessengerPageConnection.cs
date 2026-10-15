using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace CRM.Messenger;

public class MessengerPageConnection : AggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string PageId { get; set; } = default!;
    public string PageName { get; set; } = default!;
    public string AccessToken { get; set; } = default!;   // stored ENCRYPTED

    protected MessengerPageConnection() { }

    public MessengerPageConnection(Guid id, Guid? tenantId, string pageId, string pageName, string encryptedToken)
        : base(id)
    {
        TenantId = tenantId;
        PageId = pageId;
        PageName = pageName;
        AccessToken = encryptedToken;
    }
}