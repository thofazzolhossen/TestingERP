using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.Services;

public class ServiceAppService :
    CrudAppService<
        Service,
        ServiceDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateServiceDto>,
    IServiceAppService
{
    public ServiceAppService(IRepository<Service, Guid> repository)
        : base(repository)
    {
    }
}