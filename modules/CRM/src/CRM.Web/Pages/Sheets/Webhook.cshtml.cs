using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CRM.Sheets;
using CRM.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Auditing;

namespace CRM.Web.Pages.Sheets;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[DisableAuditing]
public class WebhookModel : AbpPageModel
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    private readonly SheetConnectionService _conns;
    private readonly SheetLeadSyncService _sync;
    private readonly IHubContext<LeadsHub> _hub;

    public WebhookModel(SheetConnectionService conns, SheetLeadSyncService sync, IHubContext<LeadsHub> hub)
    { _conns = conns; _sync = sync; _hub = hub; }

    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        if (Request.ContentLength is null or > 1_000_000) return StatusCode(413);

        using var ms = new MemoryStream((int)Request.ContentLength.Value);
        await Request.Body.CopyToAsync(ms);
        var raw = ms.ToArray();

        var conn = await _conns.FindAsync(Request.Headers["X-Sheet-Key"].ToString());
        if (conn == null || !Valid(raw, conn.Value.Secret, Request.Headers["X-Sheet-Signature"].ToString()))
            return StatusCode(403);

        var payload = JsonSerializer.Deserialize<SheetPayload>(raw, Json);
        if (payload == null || payload.Rows.Count is 0 or > 200 ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - payload.Ts) > 300_000)   // replay guard
            return BadRequest();

        (System.Collections.Generic.List<CRM.Leads.LeadDto> changed, System.Collections.Generic.List<string> errors) result;
        using (CurrentTenant.Change(conn.Value.TenantId))
        {
            result = await _sync.SyncAsync(payload.Rows);
        }

        if (result.changed.Count > 0)
            await _hub.Clients.Group(LeadsHub.Group(conn.Value.TenantId)).SendAsync("LeadsUpserted", result.changed);

        return new JsonResult(new { updated = result.changed.Count, errors = result.errors });
    }

    private static bool Valid(byte[] body, string secret, string sig)
    {
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig.ToLowerInvariant()));
    }
}