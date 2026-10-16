using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace CRM.Messenger;

public class MessengerConversation : AggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string PageScopedUserId { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? LastMessageText { get; set; }
    public string? PageId { get; set; }
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }

    protected MessengerConversation() { }

    public MessengerConversation(Guid id, string psid, string name, string? pic, string? pageId ) : base(id)
    {
        PageScopedUserId = psid;
        CustomerName = name;
        ProfilePictureUrl = pic;
        LastMessageTime = DateTime.UtcNow;
        PageId = pageId;
    }
}