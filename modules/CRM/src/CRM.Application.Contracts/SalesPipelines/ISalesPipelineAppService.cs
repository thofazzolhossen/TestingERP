using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace CRM.SalesPipelines;

public interface ISalesPipelineAppService :
    ICrudAppService<
        SalesPipelineDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSalesPipelineDto>
{
}