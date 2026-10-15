using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace CRM.Messenger;

public class MessengerMessage : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public string? FacebookMessageId { get; set; }
    public string Text { get; set; } = default!;
    public bool IsFromCustomer { get; set; }
    public DateTime SentTime { get; set; }
    public Guid? SentByUserId { get; set; }

    // Attachment (null when the message is text only)
    public string? AttachmentType { get; set; }        // image | video | audio | file
    public string? AttachmentName { get; set; }
    public string? AttachmentMimeType { get; set; }
    public string? AttachmentStoredName { get; set; }  // file name on the server disk
    public long? AttachmentSize { get; set; }

    protected MessengerMessage() { }

    public MessengerMessage(Guid id, Guid conversationId, string? fbMid, string text,
        bool fromCustomer, DateTime sentTime, Guid? sentByUserId = null) : base(id)
    {
        ConversationId = conversationId;
        FacebookMessageId = fbMid;
        Text = text;
        IsFromCustomer = fromCustomer;
        SentTime = sentTime;
        SentByUserId = sentByUserId;
    }
}