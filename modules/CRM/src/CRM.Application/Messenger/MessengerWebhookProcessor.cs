using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;

namespace CRM.Messenger;

public class MessengerWebhookProcessor : ITransientDependency
{
    private readonly IRepository<MessengerConversation, Guid> _conversations;
    private readonly IRepository<MessengerMessage, Guid> _messages;
    private readonly IGuidGenerator _guid;
    private readonly ICurrentTenant _currentTenant;
    private readonly IConfiguration _config;
    private readonly MessengerGraphClient _graph;

    public MessengerWebhookProcessor(
        IRepository<MessengerConversation, Guid> conversations,
        IRepository<MessengerMessage, Guid> messages,
        IGuidGenerator guid,
        ICurrentTenant currentTenant,
        IConfiguration config,
        MessengerGraphClient graph)
    {
        _conversations = conversations;
        _messages = messages;
        _guid = guid;
        _currentTenant = currentTenant;
        _config = config;
        _graph = graph;
    }

    public async Task ProcessAsync(string json)
    {
        Guid? tenantId = Guid.TryParse(_config["Messenger:TenantId"], out var t) ? t : null;

        using (_currentTenant.Change(tenantId))
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("object", out var obj) || obj.GetString() != "page") return;
            if (!root.TryGetProperty("entry", out var entries)) return;

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("messaging", out var events)) continue;

                foreach (var evt in events.EnumerateArray())
                {
                    if (!evt.TryGetProperty("message", out var msg)) continue;

                    var isEcho = msg.TryGetProperty("is_echo", out var e) && e.GetBoolean();
                    var senderId = evt.GetProperty("sender").GetProperty("id").GetString()!;
                    var recipientId = evt.GetProperty("recipient").GetProperty("id").GetString()!;
                    var psid = isEcho ? recipientId : senderId;

                    var mid = msg.TryGetProperty("mid", out var m) ? m.GetString() : null;
                    if (mid != null && await _messages.FindAsync(x => x.FacebookMessageId == mid) != null)
                    {
                        continue;
                    }

                    var text = msg.TryGetProperty("text", out var tx) ? tx.GetString() ?? "" : "[Attachment]";
                    var time = evt.TryGetProperty("timestamp", out var ts)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(ts.GetInt64()).UtcDateTime
                        : DateTime.UtcNow;

                    var conv = await _conversations.FindAsync(x => x.PageScopedUserId == psid);
                    if (conv == null)
                    {
                        var (name, pic) = await _graph.GetProfileAsync(psid);
                        conv = new MessengerConversation(_guid.Create(), psid, name, pic);
                        await _conversations.InsertAsync(conv, autoSave: true);
                    }

                    await _messages.InsertAsync(
                        new MessengerMessage(_guid.Create(), conv.Id, mid, text, !isEcho, time));

                    conv.LastMessageText = text.Length > 200 ? text[..200] : text;
                    conv.LastMessageTime = time;
                    if (!isEcho) conv.UnreadCount++;
                    await _conversations.UpdateAsync(conv, autoSave: true);
                }
            }
        }
    }
}