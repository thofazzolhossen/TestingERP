using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    private readonly MessengerFileStore _files;

    public MessengerWebhookProcessor(
        IRepository<MessengerConversation, Guid> conversations,
        IRepository<MessengerMessage, Guid> messages,
        IGuidGenerator guid,
        ICurrentTenant currentTenant,
        IConfiguration config,
        MessengerGraphClient graph,
        MessengerFileStore files)
    {
        _conversations = conversations;
        _messages = messages;
        _guid = guid;
        _currentTenant = currentTenant;
        _config = config;
        _graph = graph;
        _files = files;
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
                    var time = evt.TryGetProperty("timestamp", out var ts)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(ts.GetInt64()).UtcDateTime
                        : DateTime.UtcNow;
                    var text = msg.TryGetProperty("text", out var tx) ? tx.GetString() ?? "" : "";

                    var items = new List<MessengerMessage>();

                    // 1) The text part (emoji arrive here as normal unicode text)
                    if (text.Length > 0 && !await ExistsAsync(mid))
                    {
                        items.Add(new MessengerMessage(_guid.Create(), Guid.Empty, mid,
                            Trunc(text), !isEcho, time));
                    }

                    // 2) Every attachment becomes its own message ("mid:0", "mid:1", ...)
                    if (msg.TryGetProperty("attachments", out var atts) && atts.ValueKind == JsonValueKind.Array)
                    {
                        var i = 0;
                        foreach (var a in atts.EnumerateArray())
                        {
                            var key = mid == null ? null : $"{mid}:{i}";
                            i++;
                            if (await ExistsAsync(key)) continue;

                            var type = a.TryGetProperty("type", out var tp) ? tp.GetString() : null;
                            var hasPayload = a.TryGetProperty("payload", out var pl) && pl.ValueKind == JsonValueKind.Object;
                            var url = hasPayload && pl.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String
                                ? u.GetString() : null;
                            var sentTime = time.AddMilliseconds(i);

                            MessengerMessage? item = null;

                            if ((type is "image" or "video" or "audio" or "file") && url != null)
                            {
                                var dl = await _graph.DownloadAsync(url);
                                if (dl != null)
                                {
                                    var (bytes, mime) = dl.Value;
                                    var name = FileNameFrom(url, type!, mime);
                                    var stored = await _files.SaveAsync(bytes, name);

                                    item = new MessengerMessage(_guid.Create(), Guid.Empty, key, "", !isEcho, sentTime)
                                    {
                                        AttachmentType = type,
                                        AttachmentName = name,
                                        AttachmentMimeType = mime,
                                        AttachmentStoredName = stored,
                                        AttachmentSize = bytes.Length
                                    };
                                }
                                else
                                {
                                    item = new MessengerMessage(_guid.Create(), Guid.Empty, key,
                                        $"[{type} unavailable]", !isEcho, sentTime);
                                }
                            }
                            else if (type == "location" && hasPayload &&
                                     pl.TryGetProperty("coordinates", out var cc) &&
                                     cc.TryGetProperty("lat", out var latEl) &&
                                     cc.TryGetProperty("long", out var lngEl))
                            {
                                var lat = latEl.GetDouble().ToString(CultureInfo.InvariantCulture);
                                var lng = lngEl.GetDouble().ToString(CultureInfo.InvariantCulture);
                                item = new MessengerMessage(_guid.Create(), Guid.Empty, key,
                                    $"📍 https://www.google.com/maps?q={lat},{lng}", !isEcho, sentTime);
                            }
                            else if (url != null)
                            {
                                var title = a.TryGetProperty("title", out var ttl) ? ttl.GetString() : null;
                                item = new MessengerMessage(_guid.Create(), Guid.Empty, key,
                                    Trunc(string.IsNullOrWhiteSpace(title) ? url : title + "\n" + url), !isEcho, sentTime);
                            }
                            else
                            {
                                item = new MessengerMessage(_guid.Create(), Guid.Empty, key,
                                    "[Attachment]", !isEcho, sentTime);
                            }

                            items.Add(item);
                        }
                    }

                    if (items.Count == 0 && text.Length == 0 && !await ExistsAsync(mid))
                    {
                        items.Add(new MessengerMessage(_guid.Create(), Guid.Empty, mid,
                            "[Unsupported message]", !isEcho, time));
                    }

                    if (items.Count == 0) continue;

                    var conv = await _conversations.FindAsync(x => x.PageScopedUserId == psid);
                    if (conv == null)
                    {
                        var (name, pic) = await _graph.GetProfileAsync(psid);
                        conv = new MessengerConversation(_guid.Create(), psid, name, pic);
                        await _conversations.InsertAsync(conv, autoSave: true);
                    }

                    foreach (var item in items)
                    {
                        item.ConversationId = conv.Id;
                        await _messages.InsertAsync(item);
                        if (!isEcho) conv.UnreadCount++;
                    }

                    var last = items[^1];
                    var preview = MessengerMime.Preview(last.AttachmentType, last.AttachmentName, last.Text);
                    conv.LastMessageText = preview.Length > 200 ? preview[..200] : preview;
                    conv.LastMessageTime = time;
                    await _conversations.UpdateAsync(conv, autoSave: true);
                }
            }
        }
    }

    private async Task<bool> ExistsAsync(string? key)
        => key != null && await _messages.FindAsync(x => x.FacebookMessageId == key) != null;

    private static string Trunc(string s) => s.Length > 2000 ? s[..2000] : s;

    private static string FileNameFrom(string url, string type, string mime)
    {
        var name = "";
        try { name = Path.GetFileName(new Uri(url).AbsolutePath); } catch { }

        if (string.IsNullOrWhiteSpace(name) || !name.Contains('.'))
        {
            name = type + MessengerMime.ExtFromMime(mime);
        }

        return name.Length > 200 ? name[^200..] : name;
    }
}