using CRM.Messenger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Auditing;

namespace CRM.Web.Pages.Messenger;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[DisableAuditing]
public class WebhookModel : AbpPageModel
{
    private readonly IConfiguration _config;
    private readonly MessengerWebhookProcessor _processor;

    public WebhookModel(IConfiguration config, MessengerWebhookProcessor processor)
    {
        _config = config;
        _processor = processor;
    }

    // Facebook calls this once when you save the webhook in the Meta dashboard.
    public IActionResult OnGet(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {

        if (mode == "subscribe" && !string.IsNullOrEmpty(token) && token == _config["Messenger:VerifyToken"])
        {
            return Content(challenge ?? "", "text/plain");
        }

        return StatusCode(403);
    }

    // Facebook sends every new message here.
    public async Task<IActionResult> OnPostAsync()
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var raw = ms.ToArray();

        Logger.LogInformation("Messenger webhook received: {Bytes} bytes", raw.Length);

        var secret = _config["Messenger:AppSecret"];
        var header = Request.Headers["X-Hub-Signature-256"].ToString();

        if (string.IsNullOrEmpty(secret) || !IsValidSignature(raw, secret, header))
        {
            Logger.LogWarning("Messenger webhook REJECTED: invalid signature. Check Messenger:AppSecret.");
            return StatusCode(403);
        }

        try
        {
            var body = Encoding.UTF8.GetString(raw);
            Logger.LogInformation("Messenger payload: {Body}", body);
            await _processor.ProcessAsync(body);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Messenger webhook processing FAILED");
            throw;
        }

        return new OkResult();
    }

    private static bool IsValidSignature(byte[] body, string secret, string header)
    {
        if (!header.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(header.ToLowerInvariant()));
    }
}