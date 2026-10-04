using System;
using Volo.Abp.Application.Dtos;

namespace CRM.LeadSources;

public class LeadSourceDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}