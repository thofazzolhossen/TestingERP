using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.MultiTenancy;

namespace CRM.CustomerTypes;

public class CustomerTypeDto : FullAuditedEntityDto<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}