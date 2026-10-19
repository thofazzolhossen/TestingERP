using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;

namespace CRM.Sheets;

public class SheetConnectionService : ITransientDependency
{
    private readonly IRepository<SheetConnection, Guid> _repo;
    private readonly IStringEncryptionService _crypto;
    private readonly IGuidGenerator _guid;
    private readonly ICurrentTenant _tenant;
    private readonly IDataFilter _filter;

    public SheetConnectionService(IRepository<SheetConnection, Guid> repo, IStringEncryptionService crypto,
        IGuidGenerator guid, ICurrentTenant tenant, IDataFilter filter)
    { _repo = repo; _crypto = crypto; _guid = guid; _tenant = tenant; _filter = filter; }

    private static string Rand(int bytes) => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();

    // Current tenant's key/secret (tenant filter is automatic). Creates on first use.
    public async Task<(string Key, string Secret)> GetOrCreateAsync(bool regenerate = false)
    {
        var c = await (await _repo.GetQueryableAsync()).FirstOrDefaultAsync();
        if (c == null)
        {
            var s = Rand(32);
            c = new SheetConnection(_guid.Create(), _tenant.Id, Rand(16), _crypto.Encrypt(s)!);
            await _repo.InsertAsync(c, autoSave: true);
            return (c.ConnectionKey, s);
        }
        if (regenerate)
        {
            c.Secret = _crypto.Encrypt(Rand(32))!;
            await _repo.UpdateAsync(c, autoSave: true);
        }
        return (c.ConnectionKey, _crypto.Decrypt(c.Secret)!);
    }

    // Webhook only (nobody logged in): find the owning tenant by key (unique index).
    public async Task<(Guid? TenantId, string Secret)?> FindAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > 64) return null;
        using (_filter.Disable<IMultiTenant>())
        {
            var c = await _repo.FindAsync(x => x.ConnectionKey == key);
            return c == null ? null : (c.TenantId, _crypto.Decrypt(c.Secret)!);
        }
    }
}