using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Messenger;

public class MessengerConversationDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}

public class MessengerMessageDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = default!;
    public bool IsFromCustomer { get; set; }
    public DateTime SentTime { get; set; }
}

public class SendMessengerMessageInput
{
    [Required]
    [StringLength(2000)]
    public string Text { get; set; } = default!;
}