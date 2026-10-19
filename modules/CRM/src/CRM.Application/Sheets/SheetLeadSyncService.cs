using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CRM.CustomerTypes;
using CRM.Leads;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace CRM.Sheets;

public record NameId(Guid Id, string Name);

public class SheetRowDto
{
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public string? Remarks { get; set; }
    public string? CustomerType { get; set; }
    public string? LeadSource { get; set; }
    public string? LeadStage { get; set; }
    public string? Service { get; set; }
    public string? AssignedUser { get; set; }
}

public class SheetPayload
{
    public long Ts { get; set; }
    public List<SheetRowDto> Rows { get; set; } = new();
}

public class SheetLeadSyncService : ITransientDependency
{
    private readonly IRepository<Lead, Guid> _leads;
    private readonly IRepository<CustomerType, Guid> _ct;
    private readonly IRepository<LeadSource, Guid> _ls;
    private readonly IRepository<LeadStage, Guid> _st;
    private readonly IRepository<Service, Guid> _sv;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IGuidGenerator _guid;
    private readonly IObjectMapper _mapper;

    public SheetLeadSyncService(IRepository<Lead, Guid> leads, IRepository<CustomerType, Guid> ct,
        IRepository<LeadSource, Guid> ls, IRepository<LeadStage, Guid> st, IRepository<Service, Guid> sv,
        IRepository<IdentityUser, Guid> users, IGuidGenerator guid, IObjectMapper mapper)
    { _leads = leads; _ct = ct; _ls = ls; _st = st; _sv = sv; _users = users; _guid = guid; _mapper = mapper; }

    private static string K(string? s) => s?.Trim() ?? "";

    private static async Task<Dictionary<string, Guid>> Names<T>(IRepository<T, Guid> repo, Expression<Func<T, NameId>> sel)
        where T : class, IEntity<Guid>
    {
        var list = await (await repo.GetQueryableAsync()).AsNoTracking().Select(sel).ToListAsync();
        var d = new Dictionary<string, Guid>(list.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var x in list) d.TryAdd(x.Name.Trim(), x.Id);
        return d;
    }

    // Must run inside CurrentTenant.Change(...) -> tenant filter + TenantId are applied automatically.
    public async Task<(List<LeadDto> Changed, List<string> Errors)> SyncAsync(List<SheetRowDto> input)
    {
        var errors = new List<string>();
        var rows = input.Where(r => !string.IsNullOrWhiteSpace(r.ExternalId) && !string.IsNullOrWhiteSpace(r.Name))
                        .GroupBy(r => r.ExternalId).Select(g => g.Last()).ToList();   // dedupe, last edit wins
        if (rows.Count == 0) return (new(), errors);

        var ids = rows.Select(r => r.ExternalId).ToList();
        var existing = (await _leads.GetListAsync(x => ids.Contains(x.ExternalId!)))
                       .ToDictionary(x => x.ExternalId!);

        var ct = await Names(_ct, x => new NameId(x.Id, x.Name));
        var ls = await Names(_ls, x => new NameId(x.Id, x.Name));
        var st = await Names(_st, x => new NameId(x.Id, x.Name));
        var sv = await Names(_sv, x => new NameId(x.Id, x.Name));

        var userNames = rows.Select(r => K(r.AssignedUser)).Where(n => n.Length > 0).Distinct().ToList();
        var users = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (userNames.Count > 0)
        {
            var u = await (await _users.GetQueryableAsync()).AsNoTracking()
                .Where(x => userNames.Contains(x.UserName)).Select(x => new NameId(x.Id, x.UserName)).ToListAsync();
            foreach (var x in u) users.TryAdd(x.Name, x.Id);
        }

        var ins = new List<Lead>(); var upd = new List<Lead>(); var pairs = new List<(Lead, SheetRowDto)>(rows.Count);

        foreach (var r in rows)
        {
            if (!ct.TryGetValue(K(r.CustomerType), out var ctId) || !ls.TryGetValue(K(r.LeadSource), out var lsId) ||
                !st.TryGetValue(K(r.LeadStage), out var stId) || !sv.TryGetValue(K(r.Service), out var svId))
            {
                errors.Add($"{r.Name}: CustomerType/LeadSource/LeadStage/Service not found");
                continue;
            }
            Guid? uid = users.TryGetValue(K(r.AssignedUser), out var u1) ? u1 : null;

            if (existing.TryGetValue(r.ExternalId, out var l))
            {
                l.Name = K(r.Name); l.CompanyName = r.CompanyName ?? ""; l.Email = r.Email ?? ""; l.Phone = r.Phone ?? "";
                l.EstimatedBudget = r.EstimatedBudget ?? 0m; l.Remarks = r.Remarks;
                l.CustomerTypeId = ctId; l.LeadSourceId = lsId; l.LeadStageId = stId; l.ServiceId = svId; l.AssignedUserId = uid;
                upd.Add(l);
            }
            else
            {
                l = new Lead(_guid.Create(), K(r.Name), ctId, lsId, stId, svId, r.CompanyName ?? "", r.Email ?? "",
                    r.Phone ?? "", r.EstimatedBudget ?? 0m, r.Remarks, uid)
                { ExternalId = r.ExternalId };
                ins.Add(l);
            }
            pairs.Add((l, r));
        }

        if (ins.Count > 0) await _leads.InsertManyAsync(ins, autoSave: true);
        if (upd.Count > 0) await _leads.UpdateManyAsync(upd, autoSave: true);
        if (ins.Count + upd.Count > 0) await _leads.GetQueryableAsync(); // no-op; see note below

        var dtos = new List<LeadDto>(pairs.Count);
        foreach (var (l, r) in pairs)
        {
            var d = _mapper.Map<Lead, LeadDto>(l);
            d.CustomerTypeName = K(r.CustomerType); d.LeadSourceName = K(r.LeadSource);
            d.LeadStageName = K(r.LeadStage); d.ServiceName = K(r.Service);
            d.AssignedUserName = l.AssignedUserId.HasValue ? K(r.AssignedUser) : "Unassigned";
            dtos.Add(d);
        }
        return (dtos, errors);
    }
}