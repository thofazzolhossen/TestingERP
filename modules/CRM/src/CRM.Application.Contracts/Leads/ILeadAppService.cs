using System;
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
}