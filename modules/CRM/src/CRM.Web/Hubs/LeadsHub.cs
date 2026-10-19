using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.SignalR;

namespace CRM.Web.Hubs;

[Authorize]
[HubRoute("/signalr-hubs/leads")]
public class LeadsHub : AbpHub
{
    public static string Group(Guid? tenantId) => tenantId?.ToString("N") ?? "host";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(CurrentTenant.Id));
        await base.OnConnectedAsync();
    }
}