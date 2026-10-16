using System.Threading.Tasks;
using CRM.Messenger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Messenger;

[Authorize]
public class SettingsModel : AbpPageModel
{
    private readonly MessengerConnectionService _connections;

    public SettingsModel(MessengerConnectionService connections)
    {
        _connections = connections;
    }

    public string? PageName { get; set; }
    public string? Error { get; set; }

    [BindProperty]
    public string Token { get; set; } = "";

    public async Task OnGetAsync()
    {
        PageName = (await _connections.GetCurrentAsync())?.PageName;
    }

    public async Task<IActionResult> OnPostConnectAsync()
    {
        try
        {
            await _connections.ConnectAsync(Token);
            return RedirectToPage();
        }
        catch (UserFriendlyException ex)
        {
            Error = ex.Message;
            PageName = (await _connections.GetCurrentAsync())?.PageName;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        await _connections.DisconnectAsync();
        return RedirectToPage();
    }
}