using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.CustomerTypes;

public interface ICustomerTypeAppService :
    ICrudAppService<
        CustomerTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateCustomerTypeDto>
{
}