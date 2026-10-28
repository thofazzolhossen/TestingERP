using System;

namespace CRM.Leads;

public class LeadStageHistoryDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid? OldStageId { get; set; }
    public string OldStageName { get; set; } = "Initial Creation";
    public Guid NewStageId { get; set; }
    public string NewStageName { get; set; } = default!;
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public string ChangedByUserName { get; set; } = "System";
    public string? Remarks { get; set; }
}