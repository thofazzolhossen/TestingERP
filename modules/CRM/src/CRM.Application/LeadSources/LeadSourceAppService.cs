using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.LeadSources;

public class LeadSourceAppService :
    CrudAppService<
        LeadSource,
        LeadSourceDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLeadSourceDto>,
    ILeadSourceAppService
{
    public LeadSourceAppService(IRepository<LeadSource, Guid> repository)
        : base(repository)
    {
    }
}