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
    private static readonly HttpClient Http = new();
    private readonly IConfiguration _config;

    public MessengerGraphClient(IConfiguration config) => _config = config;

    private string Token => _config["Messenger:PageAccessToken"]
        ?? throw new UserFriendlyException("Messenger:PageAccessToken is not configured.");
    private string Version => _config["Messenger:GraphApiVersion"] ?? "v21.0";

    public async Task<(string Name, string? Picture)> GetProfileAsync(string psid)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://graph.facebook.com/{Version}/{psid}?fields=first_name,last_name,profile_pic");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
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

    public async Task<string?> SendTextAsync(string psid, string text)
    {
        var body = JsonSerializer.Serialize(new
        {
            recipient = new { id = psid },
            messaging_type = "RESPONSE",
            message = new { text }
        });

        var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/{Version}/me/messages")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

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