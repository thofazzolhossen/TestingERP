using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.LeadStages;

public interface ILeadStageAppService :
    ICrudAppService<
        LeadStageDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLeadStageDto>
{
}