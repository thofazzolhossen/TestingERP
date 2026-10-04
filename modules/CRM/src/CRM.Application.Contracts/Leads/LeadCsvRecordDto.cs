namespace CRM.Leads;

public class LeadCsvRecordDto
{
    public string Name { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public string? Remarks { get; set; }

    // Lookups by Name / Username in CSV
    public string CustomerTypeName { get; set; } = default!;
    public string LeadSourceName { get; set; } = default!;
    public string LeadStageName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string? AssignedUserName { get; set; }
}