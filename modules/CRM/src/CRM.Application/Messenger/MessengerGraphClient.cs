using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CRM.Messenger;

public class MessengerGraphClient : ITransientDependency
{
    public const int MaxFileBytes = 25 * 1024 * 1024;

    private static readonly HttpClient Http = new();
    private readonly IConfiguration _config;

    public MessengerGraphClient(IConfiguration config) => _config = config;

    private string Version => _config["Messenger:GraphApiVersion"] ?? "v21.0";

    private static HttpRequestMessage NewRequest(HttpMethod method, string url, string token)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // Checks the token and returns the page it belongs to
    
    public async Task<(string Id, string Name)> GetPageAsync(string token)
    {
        var req = NewRequest(HttpMethod.Get, $"https://graph.facebook.com/{Version}/me?fields=id,name", token);
        var res = await Http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        if (!res.IsSuccessStatusCode || !doc.RootElement.TryGetProperty("id", out var id))
        {
            throw new UserFriendlyException("Facebook says: " + ReadError(doc.RootElement));
        }

        var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
        return (id.GetString()!, string.IsNullOrWhiteSpace(name) ? "Facebook Page" : name!);
    }

    // Tells Facebook to send this page's messages to our webhook
    public async Task SubscribeAsync(string pageId, string token)
    {
        var req = NewRequest(HttpMethod.Post,
            $"https://graph.facebook.com/{Version}/{pageId}/subscribed_apps?subscribed_fields=messages,message_echoes",
            token);
        var res = await Http.SendAsync(req);

        if (!res.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            throw new UserFriendlyException("Facebook says: " + ReadError(doc.RootElement));
        }
    }

    private static string ReadError(JsonElement root)
    {
        if (root.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m))
        {
            var code = e.TryGetProperty("code", out var c) ? c.ToString() : "";
            return $"{m.GetString()} (code {code})";
        }
        return "Unknown error.";
    }

    public async Task<(string Name, string? Picture)> GetProfileAsync(string psid, string token)
    {
        try
        {
            var req = NewRequest(HttpMethod.Get,
                $"https://graph.facebook.com/{Version}/{psid}?fields=first_name,last_name,profile_pic", token);
            var res = await Http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return ("Facebook User", null);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var r = doc.RootElement;
            var first = r.TryGetProperty("first_name", out var f) ? f.GetString() : "";
            var last = r.TryGetProperty("last_name", out var l) ? l.GetString() : "";
            var pic = r.TryGetProperty("profile_pic", out var p) ? p.GetString() : null;
            var name = $"{first} {last}".Trim();
            return (string.IsNullOrWhiteSpace(name) ? "Facebook User" : name, pic);
        }
        catch
        {
            return ("Facebook User", null);
        }
    }

    public async Task<string?> SendTextAsync(string token, string psid, string text)
    {
        var body = JsonSerializer.Serialize(new
        {
            recipient = new { id = psid },
            messaging_type = "RESPONSE",
            message = new { text }
        });

        var req = NewRequest(HttpMethod.Post, $"https://graph.facebook.com/{Version}/me/messages", token);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        return await SendAndReadMessageIdAsync(req);
    }

    public async Task<string?> SendAttachmentAsync(
        string token, string psid, string type, string fileName, string contentType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(JsonSerializer.Serialize(new { id = psid })), "recipient");
        form.Add(new StringContent("RESPONSE"), "messaging_type");
        form.Add(new StringContent(JsonSerializer.Serialize(new
        {
            attachment = new { type, payload = new { is_reusable = true } }
        })), "message");

        var file = new ByteArrayContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var ct)
            ? ct
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "filedata", fileName);

        var req = NewRequest(HttpMethod.Post, $"https://graph.facebook.com/{Version}/me/messages", token);
        req.Content = form;

        return await SendAndReadMessageIdAsync(req);
    }

    public async Task<(byte[] Content, string ContentType)?> DownloadAsync(string url)
    {
        try
        {
            using var res = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!res.IsSuccessStatusCode) return null;
            if (res.Content.Headers.ContentLength > MaxFileBytes) return null;

            var bytes = await res.Content.ReadAsByteArrayAsync();
            if (bytes.Length > MaxFileBytes) return null;

            return (bytes, res.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> SendAndReadMessageIdAsync(HttpRequestMessage req)
    {
        var res = await Http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        if (!res.IsSuccessStatusCode)
        {
            var msg = doc.RootElement.TryGetProperty("error", out var e) &&
                      e.TryGetProperty("message", out var m) ? m.GetString() : "Facebook rejected the message.";
            throw new UserFriendlyException("Facebook: " + msg);
        }

        return doc.RootElement.TryGetProperty("message_id", out var mid) ? mid.GetString() : null;
    }
}