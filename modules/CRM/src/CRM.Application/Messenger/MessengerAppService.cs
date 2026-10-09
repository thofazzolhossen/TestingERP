using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.Messenger;

[Authorize]
public class MessengerAppService : ApplicationService, IMessengerAppService
{
    private readonly IRepository<MessengerConversation, Guid> _conversations;
    private readonly IRepository<MessengerMessage, Guid> _messages;
    private readonly MessengerGraphClient _graph;

    public MessengerAppService(
        IRepository<MessengerConversation, Guid> conversations,
        IRepository<MessengerMessage, Guid> messages,
        MessengerGraphClient graph)
    {
        _conversations = conversations;
        _messages = messages;
        _graph = graph;
    }

    public async Task<List<MessengerConversationDto>> GetConversationsAsync()
    {
        var q = await _conversations.GetQueryableAsync();
        var list = await q.OrderByDescending(x => x.LastMessageTime).Take(100).ToListAsync();
        return list.Select(x => new MessengerConversationDto
        {
            Id = x.Id,
            CustomerName = x.CustomerName,
            ProfilePictureUrl = x.ProfilePictureUrl,
            LastMessageText = x.LastMessageText,
            LastMessageTime = x.LastMessageTime,
            UnreadCount = x.UnreadCount
        }).ToList();
    }

    public async Task<List<MessengerMessageDto>> GetMessagesAsync(Guid conversationId)
    {
        var q = await _messages.GetQueryableAsync();
        var list = await q.Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.SentTime).Take(200).ToListAsync();
        list.Reverse();
        return list.Select(ToDto).ToList();
    }

    public async Task<MessengerMessageDto> SendAsync(Guid conversationId, SendMessengerMessageInput input)
    {
        var conv = await _conversations.GetAsync(conversationId);
        var text = input.Text.Trim();

        var mid = await _graph.SendTextAsync(conv.PageScopedUserId, text);

        var msg = new MessengerMessage(GuidGenerator.Create(), conv.Id, mid, text, false,
            DateTime.UtcNow, CurrentUser.Id);
        await _messages.InsertAsync(msg);

        conv.LastMessageText = text.Length > 200 ? text[..200] : text;
        conv.LastMessageTime = msg.SentTime;
        await _conversations.UpdateAsync(conv, autoSave: true);

        return ToDto(msg);
    }

    public async Task MarkAsReadAsync(Guid conversationId)
    {
        var conv = await _conversations.GetAsync(conversationId);
        if (conv.UnreadCount == 0) return;
        conv.UnreadCount = 0;
        await _conversations.UpdateAsync(conv, autoSave: true);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var q = await _conversations.GetQueryableAsync();
        return await q.SumAsync(x => x.UnreadCount);
    }

    private static MessengerMessageDto ToDto(MessengerMessage m) => new()
    {
        Id = m.Id,
        Text = m.Text,
        IsFromCustomer = m.IsFromCustomer,
        SentTime = m.SentTime
    };
}