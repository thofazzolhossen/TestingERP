using System;
using System.Threading.Tasks;
using CRM.Messenger;
using Microsoft.AspNetCore.Authorization;
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

    // GET /Messenger?handler=Conversations
    public async Task<JsonResult> OnGetConversationsAsync()
        => new JsonResult(await _service.GetConversationsAsync());

    // GET /Messenger?handler=Messages&id=...
    public async Task<JsonResult> OnGetMessagesAsync(Guid id)
        => new JsonResult(await _service.GetMessagesAsync(id));

    // GET /Messenger?handler=Unread
    public async Task<JsonResult> OnGetUnreadAsync()
        => new JsonResult(new { count = await _service.GetUnreadCountAsync() });

    // POST /Messenger?handler=Send&id=...
    public async Task<JsonResult> OnPostSendAsync(Guid id, [FromBody] SendMessengerMessageInput input)
        => new JsonResult(await _service.SendAsync(id, input));

    // POST /Messenger?handler=Read&id=...
    public async Task<JsonResult> OnPostReadAsync(Guid id)
    {
        await _service.MarkAsReadAsync(id);
        return new JsonResult(new { success = true });
    }
}