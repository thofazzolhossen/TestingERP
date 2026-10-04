using System.ComponentModel.DataAnnotations;

namespace CRM.SalesPipelines;

public class CreateUpdateSalesPipelineDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = default!;

    [StringLength(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}