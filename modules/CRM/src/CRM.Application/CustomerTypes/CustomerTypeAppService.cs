using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.CustomerTypes;

public class CustomerTypeAppService :
    CrudAppService<
        CustomerType,
        CustomerTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateCustomerTypeDto>,
    ICustomerTypeAppService
{
    public CustomerTypeAppService(IRepository<CustomerType, Guid> repository)
        : base(repository)
    {
    }
}