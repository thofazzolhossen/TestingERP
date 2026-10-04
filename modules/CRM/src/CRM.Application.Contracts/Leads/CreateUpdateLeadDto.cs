using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Leads;

public class CreateUpdateLeadDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = default!;

    [StringLength(128)]
    public string CompanyName { get; set; } = default!;

    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Phone]
    [StringLength(32)]
    public string Phone { get; set; } = default!;

    public decimal EstimatedBudget { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    public Guid CustomerTypeId { get; set; }

    [Required]
    public Guid LeadSourceId { get; set; }

    [Required]
    public Guid LeadStageId { get; set; }

    [Required]
    public Guid ServiceId { get; set; }

    // Assigned User Selection
    public Guid? AssignedUserId { get; set; }
}