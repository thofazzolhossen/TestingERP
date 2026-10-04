using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.LeadSources;

public interface ILeadSourceAppService :
    ICrudAppService<
        LeadSourceDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLeadSourceDto>
{
}