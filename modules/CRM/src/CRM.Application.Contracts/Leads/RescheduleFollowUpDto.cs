using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Leads;

public class RescheduleFollowUpDto
{
    [Required]
    public Guid LeadId { get; set; }

    public DateTime? NextFollowUpDate { get; set; }

    [Required]
    public Guid LeadStageId { get; set; }

    public Guid? AssignedUserId { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}