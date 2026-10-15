using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace CRM.Messenger;

[Authorize]
public class MessengerAppService : ApplicationService, IMessengerAppService
{
    private readonly IRepository<MessengerConversation, Guid> _conversations;
    private readonly IRepository<MessengerMessage, Guid> _messages;
    private readonly MessengerGraphClient _graph;
    private readonly MessengerFileStore _files;

    public MessengerAppService(
        IRepository<MessengerConversation, Guid> conversations,
        IRepository<MessengerMessage, Guid> messages,
        MessengerGraphClient graph,
        MessengerFileStore files)
    {
        _conversations = conversations;
        _messages = messages;
        _graph = graph;
        _files = files;
    }

    // The conversation decides which tenant its messages belong to.
    private async Task<MessengerConversation> GetConversationAsync(Guid id)
    {
        using (DataFilter.Disable<IMultiTenant>())
        {
            return await _conversations.GetAsync(id);
        }
    }

    public async Task<List<MessengerConversationDto>> GetConversationsAsync()
    {
        var q = await _conversations.GetQueryableAsync();   // tenant filter ON
        return await q.OrderByDescending(x => x.LastMessageTime)
            .Take(100)
            .Select(x => new MessengerConversationDto
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                ProfilePictureUrl = x.ProfilePictureUrl,
                LastMessageText = x.LastMessageText,
                LastMessageTime = x.LastMessageTime,
                UnreadCount = x.UnreadCount
            }).ToListAsync();
    }

    public async Task<List<MessengerMessageDto>> GetMessagesAsync(Guid conversationId)
    {
        using (DataFilter.Disable<IMultiTenant>())
        {
            var q = await _messages.GetQueryableAsync();
            var list = await q.Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.SentTime).Take(200).ToListAsync();
            list.Reverse();
            return list.Select(ToDto).ToList();
        }
    }

    public async Task<MessengerMessageDto> SendAsync(Guid conversationId, SendMessengerMessageInput input)
    {
        var conv = await GetConversationAsync(conversationId);
        var text = input.Text.Trim();

        var mid = await _graph.SendTextAsync(conv.PageScopedUserId, text);

        using (CurrentTenant.Change(conv.TenantId))
        {
            var msg = new MessengerMessage(GuidGenerator.Create(), conv.Id, mid, text, false,
                DateTime.UtcNow, CurrentUser.Id);
            await _messages.InsertAsync(msg);

            conv.LastMessageText = text.Length > 200 ? text[..200] : text;
            conv.LastMessageTime = msg.SentTime;
            await _conversations.UpdateAsync(conv, autoSave: true);

            return ToDto(msg);
        }
    }

    public async Task<MessengerMessageDto> SendFileAsync(
        Guid conversationId, string fileName, string contentType, byte[] content)
    {
        if (content == null || content.Length == 0)
            throw new UserFriendlyException("The file is empty.");
        if (content.Length > MessengerGraphClient.MaxFileBytes)
            throw new UserFriendlyException("File is too large. Maximum size is 25 MB.");

        var conv = await GetConversationAsync(conversationId);

        var safeName = Path.GetFileName(fileName ?? "");
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";
        if (string.IsNullOrWhiteSpace(contentType)) contentType = "application/octet-stream";

        var type = MessengerMime.TypeFromMime(contentType);
        var mid = await _graph.SendAttachmentAsync(conv.PageScopedUserId, type, safeName, contentType, content);
        var stored = await _files.SaveAsync(content, safeName);

        using (CurrentTenant.Change(conv.TenantId))
        {
            var msg = new MessengerMessage(GuidGenerator.Create(), conv.Id,
                mid == null ? null : mid + ":0", "", false, DateTime.UtcNow, CurrentUser.Id)
            {
                AttachmentType = type,
                AttachmentName = safeName,
                AttachmentMimeType = contentType,
                AttachmentStoredName = stored,
                AttachmentSize = content.Length
            };
            await _messages.InsertAsync(msg);

            var preview = MessengerMime.Preview(type, safeName, null);
            conv.LastMessageText = preview.Length > 200 ? preview[..200] : preview;
            conv.LastMessageTime = msg.SentTime;
            await _conversations.UpdateAsync(conv, autoSave: true);

            return ToDto(msg);
        }
    }

    public async Task<MessengerAttachmentDto> GetAttachmentAsync(Guid messageId)
    {
        MessengerMessage msg;
        using (DataFilter.Disable<IMultiTenant>())
        {
            msg = await _messages.GetAsync(messageId);
        }

        if (string.IsNullOrEmpty(msg.AttachmentStoredName))
            throw new EntityNotFoundException(typeof(MessengerMessage), messageId);

        var content = await _files.ReadAsync(msg.AttachmentStoredName);
        if (content == null)
            throw new EntityNotFoundException(typeof(MessengerMessage), messageId);

        return new MessengerAttachmentDto
        {
            FileName = msg.AttachmentName ?? "file",
            ContentType = msg.AttachmentMimeType ?? "application/octet-stream",
            Content = content
        };
    }

    public async Task MarkAsReadAsync(Guid conversationId)
    {
        var conv = await GetConversationAsync(conversationId);
        if (conv.UnreadCount == 0) return;
        conv.UnreadCount = 0;
        await _conversations.UpdateAsync(conv, autoSave: true);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        using (DataFilter.Disable<IMultiTenant>())
        {
            var q = await _conversations.GetQueryableAsync();
            return await q.SumAsync(x => x.UnreadCount);
        }
    }

    private static MessengerMessageDto ToDto(MessengerMessage m) => new()
    {
        Id = m.Id,
        Text = m.Text,
        IsFromCustomer = m.IsFromCustomer,
        SentTime = m.SentTime,
        AttachmentType = m.AttachmentType,
        AttachmentName = m.AttachmentName,
        AttachmentMimeType = m.AttachmentMimeType,
        AttachmentSize = m.AttachmentSize
    };
}