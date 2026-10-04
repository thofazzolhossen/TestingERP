using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.LeadStages;

public class LeadStageAppService :
    CrudAppService<
        LeadStage,
        LeadStageDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLeadStageDto>,
    ILeadStageAppService
{
    public LeadStageAppService(IRepository<LeadStage, Guid> repository)
        : base(repository)
    {
    }
}