using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.Services;

public interface IServiceAppService :
    ICrudAppService<
        ServiceDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateServiceDto>
{
}