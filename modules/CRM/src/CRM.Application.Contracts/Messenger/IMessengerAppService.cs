using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CRM.Messenger;

public interface IMessengerAppService : IApplicationService
{
    Task<List<MessengerConversationDto>> GetConversationsAsync();
    Task<List<MessengerMessageDto>> GetMessagesAsync(Guid conversationId);
    Task<MessengerMessageDto> SendAsync(Guid conversationId, SendMessengerMessageInput input);
    Task<MessengerMessageDto> SendFileAsync(Guid conversationId, string fileName, string contentType, byte[] content);
    Task<MessengerAttachmentDto> GetAttachmentAsync(Guid messageId);
    Task MarkAsReadAsync(Guid conversationId);
    Task<int> GetUnreadCountAsync();

}