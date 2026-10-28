using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.Leads;

public interface ILeadAppService :
    ICrudAppService<
        LeadDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLeadDto>
{
    Task<int> ImportCsvAsync(string csvContent);
    Task<LeadDetailDto> GetDetailAsync(Guid id);
    Task<List<LeadStageHistoryDto>> GetStageHistoryAsync(Guid leadId);
    Task<List<LeadDto>> GetFollowUpListAsync(GetFollowUpListInput input);
    Task RescheduleFollowUpAsync(RescheduleFollowUpDto input);
}