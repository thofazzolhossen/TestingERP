using System.ComponentModel.DataAnnotations;

namespace CRM.LeadStages;

public class CreateUpdateLeadStageDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = default!;

    [StringLength(512)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}