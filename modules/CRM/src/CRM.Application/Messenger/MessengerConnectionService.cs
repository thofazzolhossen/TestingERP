using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;

namespace CRM.Messenger;

public class MessengerConnectionInfo
{
    public Guid? TenantId { get; set; }
    public string PageId { get; set; } = default!;
    public string Token { get; set; } = default!;
}

public class MessengerConnectionService : ITransientDependency
{
    private readonly IRepository<MessengerPageConnection, Guid> _repo;
    private readonly IStringEncryptionService _crypto;
    private readonly MessengerGraphClient _graph;
    private readonly IGuidGenerator _guid;
    private readonly ICurrentTenant _tenant;
    private readonly IDataFilter _dataFilter;

    public MessengerConnectionService(
        IRepository<MessengerPageConnection, Guid> repo,
        IStringEncryptionService crypto,
        MessengerGraphClient graph,
        IGuidGenerator guid,
        ICurrentTenant tenant,
        IDataFilter dataFilter)
    {
        _repo = repo;
        _crypto = crypto;
        _graph = graph;
        _guid = guid;
        _tenant = tenant;
        _dataFilter = dataFilter;
    }

    // Used ONLY by the webhook (nobody is logged in there): find which tenant owns this page.
    public async Task<MessengerConnectionInfo?> FindByPageIdAsync(string pageId)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var c = await _repo.FindAsync(x => x.PageId == pageId);
            if (c == null) return null;

            return new MessengerConnectionInfo
            {
                TenantId = c.TenantId,
                PageId = c.PageId,
                Token = _crypto.Decrypt(c.AccessToken)!
            };
        }
    }

    // Used when sending: current tenant's page token (tenant filter is automatic).
    public async Task<string> GetTokenAsync(string? pageId)
    {
        var q = await _repo.GetQueryableAsync();
        var c = string.IsNullOrEmpty(pageId)
            ? await q.FirstOrDefaultAsync()
            : await q.FirstOrDefaultAsync(x => x.PageId == pageId);

        if (c == null)
        {
            throw new UserFriendlyException("Messenger is not connected. Open Messenger Settings and connect your page.");
        }

        return _crypto.Decrypt(c.AccessToken)!;
    }

    public async Task<MessengerPageConnection?> GetCurrentAsync()
    {
        var q = await _repo.GetQueryableAsync();
        return await q.FirstOrDefaultAsync();
    }

    public async Task ConnectAsync(string token)
    {
        token = (token ?? "").Trim();
        if (token.Length == 0) throw new UserFriendlyException("Please paste the Page Access Token.");

        var (pageId, name) = await _graph.GetPageAsync(token);

        // A page can belong to only one tenant
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var other = await _repo.FindAsync(x => x.PageId == pageId);
            if (other != null && other.TenantId != _tenant.Id)
            {
                throw new UserFriendlyException("This page is already connected to another account.");
            }
        }

        await _graph.SubscribeAsync(pageId, token);

        var existing = await _repo.FindAsync(x => x.PageId == pageId);
        if (existing == null)
        {
            await _repo.InsertAsync(new MessengerPageConnection(
                _guid.Create(), _tenant.Id, pageId, name, _crypto.Encrypt(token)!));
        }
        else
        {
            existing.PageName = name;
            existing.AccessToken = _crypto.Encrypt(token)!;
            await _repo.UpdateAsync(existing);
        }
    }

    public async Task DisconnectAsync()
    {
        var c = await GetCurrentAsync();
        if (c != null) await _repo.DeleteAsync(c);
    }
}