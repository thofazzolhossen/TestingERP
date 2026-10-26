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
using CRM.Sheets;                       // reuses NameId record
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace CRM.Dashboard;

[Authorize]
public class CrmDashboardAppService : ApplicationService, ICrmDashboardAppService
{
    private readonly IRepository<Lead, Guid> _leads;
    private readonly IRepository<LeadStage, Guid> _stages;
    private readonly IRepository<LeadSource, Guid> _sources;
    private readonly IRepository<Service, Guid> _services;
    private readonly IRepository<CustomerType, Guid> _types;
    private readonly IRepository<IdentityUser, Guid> _users;

    public CrmDashboardAppService(
        IRepository<Lead, Guid> leads, IRepository<LeadStage, Guid> stages,
        IRepository<LeadSource, Guid> sources, IRepository<Service, Guid> services,
        IRepository<CustomerType, Guid> types, IRepository<IdentityUser, Guid> users)
    {
        _leads = leads; _stages = stages; _sources = sources;
        _services = services; _types = types; _users = users;
    }

    public async Task<CrmDashboardDto> GetAsync()
    {
        var today = Clock.Now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var prevStart = monthStart.AddMonths(-1);
        var trendFrom = today.AddDays(-29);

        var q = (await _leads.GetQueryableAsync()).AsNoTracking();
        var dto = new CrmDashboardDto();

        // 1) All KPIs in ONE round trip (single SQL row)
        var s = await q.GroupBy(_ => 1).Select(g => new
        {
            Total = g.Count(),
            Budget = g.Sum(x => x.EstimatedBudget),
            ThisMonth = g.Count(x => x.CreationTime >= monthStart),
            LastMonth = g.Count(x => x.CreationTime >= prevStart && x.CreationTime < monthStart),
            Overdue = g.Count(x => x.NextFollowUpDate < today),
            Today = g.Count(x => x.NextFollowUpDate >= today && x.NextFollowUpDate < tomorrow),
            Unassigned = g.Count(x => x.AssignedUserId == null)
        }).FirstOrDefaultAsync();

        if (s != null)
        {
            dto.TotalLeads = s.Total;
            dto.TotalBudget = s.Budget;
            dto.AvgBudget = s.Total == 0 ? 0 : Math.Round(s.Budget / s.Total, 2);
            dto.NewThisMonth = s.ThisMonth;
            dto.NewLastMonth = s.LastMonth;
            dto.OverdueFollowUps = s.Overdue;
            dto.DueToday = s.Today;
            dto.Unassigned = s.Unassigned;
        }

        // 2) 30-day trend (grouped by day in SQL, gaps filled in memory)
        var days = await q.Where(x => x.CreationTime >= trendFrom)
            .GroupBy(x => x.CreationTime.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();
        var map = days.ToDictionary(x => x.Day, x => x.Count);
        for (var d = trendFrom; d <= today; d = d.AddDays(1))
            dto.Trend.Add(new TrendPointDto { Date = d, Count = map.GetValueOrDefault(d) });

        // 3) Breakdowns (sequential: one DbContext)
        dto.ByStage = await GroupAsync(q, x => x.LeadStageId, _stages, x => new NameId(x.Id, x.Name));
        dto.BySource = await GroupAsync(q, x => x.LeadSourceId, _sources, x => new NameId(x.Id, x.Name));
        dto.ByService = await GroupAsync(q, x => x.ServiceId, _services, x => new NameId(x.Id, x.Name));
        dto.ByCustomerType = await GroupAsync(q, x => x.CustomerTypeId, _types, x => new NameId(x.Id, x.Name));

        // 4) Top 5 assignees
        var top = await q.Where(x => x.AssignedUserId != null)
            .GroupBy(x => x.AssignedUserId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count(), Value = g.Sum(x => x.EstimatedBudget) })
            .OrderByDescending(x => x.Count).Take(5).ToListAsync();
        if (top.Count > 0)
        {
            var ids = top.Select(x => x.Id).ToList();
            var names = await (await _users.GetQueryableAsync()).AsNoTracking()
                .Where(u => ids.Contains(u.Id)).Select(u => new NameId(u.Id, u.UserName)).ToListAsync();
            var nm = names.ToDictionary(x => x.Id, x => x.Name);
            dto.TopAssignees = top.Select(x => new ChartItemDto
            { Name = nm.GetValueOrDefault(x.Id, "Unknown"), Count = x.Count, Value = x.Value }).ToList();
        }

        // 5) Next 8 follow-ups (overdue first)
        dto.Upcoming = await q.Where(x => x.NextFollowUpDate != null)
            .OrderBy(x => x.NextFollowUpDate).Take(8)
            .Select(x => new FollowUpDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Date = x.NextFollowUpDate!.Value,
                Budget = x.EstimatedBudget
            }).ToListAsync();

        return dto;
    }

    // Group leads by FK (tiny result), then attach names from the small lookup table.
    private static async Task<List<ChartItemDto>> GroupAsync<T>(
        IQueryable<Lead> q, Expression<Func<Lead, Guid>> fk,
        IRepository<T, Guid> lookup, Expression<Func<T, NameId>> sel)
        where T : class, IEntity<Guid>
    {
        var rows = await q.GroupBy(fk)
            .Select(g => new { Id = g.Key, Count = g.Count(), Value = g.Sum(x => x.EstimatedBudget) })
            .ToListAsync();
        if (rows.Count == 0) return new();

        var names = (await (await lookup.GetQueryableAsync()).AsNoTracking().Select(sel).ToListAsync())
            .ToDictionary(x => x.Id, x => x.Name);

        return rows.OrderByDescending(r => r.Count)
            .Select(r => new ChartItemDto { Name = names.GetValueOrDefault(r.Id, "Unknown"), Count = r.Count, Value = r.Value })
            .ToList();
    }
}