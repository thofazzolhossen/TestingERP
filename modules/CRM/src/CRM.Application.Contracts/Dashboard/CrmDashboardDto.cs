using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CRM.Dashboard;

public class ChartItemDto
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class TrendPointDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class FollowUpDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public DateTime Date { get; set; }
    public decimal Budget { get; set; }
}

public class CrmDashboardDto
{
    public int TotalLeads { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal AvgBudget { get; set; }
    public int NewThisMonth { get; set; }
    public int NewLastMonth { get; set; }
    public int OverdueFollowUps { get; set; }
    public int DueToday { get; set; }
    public int Unassigned { get; set; }

    public List<TrendPointDto> Trend { get; set; } = new();
    public List<ChartItemDto> ByStage { get; set; } = new();
    public List<ChartItemDto> BySource { get; set; } = new();
    public List<ChartItemDto> ByService { get; set; } = new();
    public List<ChartItemDto> ByCustomerType { get; set; } = new();
    public List<ChartItemDto> TopAssignees { get; set; } = new();
    public List<FollowUpDto> Upcoming { get; set; } = new();
}

public interface ICrmDashboardAppService : IApplicationService
{
    Task<CrmDashboardDto> GetAsync();
}