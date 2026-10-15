using System;
using System.IO;
using System.Threading.Tasks;
using CRM.Messenger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Messenger;

[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly IMessengerAppService _service;

    public IndexModel(IMessengerAppService service)
    {
        _service = service;
    }

    public IActionResult OnGet() => Redirect("/");

    public async Task<JsonResult> OnGetConversationsAsync()
        => new JsonResult(await _service.GetConversationsAsync());

    public async Task<JsonResult> OnGetMessagesAsync(Guid id)
        => new JsonResult(await _service.GetMessagesAsync(id));

    public async Task<JsonResult> OnGetUnreadAsync()
        => new JsonResult(new { count = await _service.GetUnreadCountAsync() });

    public async Task<JsonResult> OnPostSendAsync(Guid id, [FromBody] SendMessengerMessageInput input)
        => new JsonResult(await _service.SendAsync(id, input));

    // POST /Messenger?handler=SendFile&id=...   (multipart, field name "file")
    public async Task<JsonResult> OnPostSendFileAsync(Guid id, IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return new JsonResult(new { success = false, message = "No file selected." });
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return new JsonResult(await _service.SendFileAsync(id, file.FileName, file.ContentType, ms.ToArray()));
    }

    // GET /Messenger?handler=File&id=<messageId>&download=true
    public async Task<IActionResult> OnGetFileAsync(Guid id, bool download = false)
    {
        var f = await _service.GetAttachmentAsync(id);
        var type = (f.ContentType ?? "").ToLowerInvariant();

        // Only safe media types are shown inline. Everything else is downloaded,
        // so an uploaded .html/.svg can never run inside your app.
        var inlineOk = !download &&
            ((type.StartsWith("image/") && !type.Contains("svg")) ||
             type.StartsWith("video/") || type.StartsWith("audio/"));

        if (inlineOk)
        {
            return new FileContentResult(f.Content, f.ContentType!)
            {
                EnableRangeProcessing = true
            };
        }

        return File(f.Content, "application/octet-stream", f.FileName);
    }

    public async Task<JsonResult> OnPostReadAsync(Guid id)
    {
        await _service.MarkAsReadAsync(id);
        return new JsonResult(new { success = true });
    }
}