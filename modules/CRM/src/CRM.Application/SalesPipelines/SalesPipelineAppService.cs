using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace CRM.SalesPipelines;

public class SalesPipelineAppService :
    CrudAppService<
        SalesPipeline,
        SalesPipelineDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSalesPipelineDto>,
    ISalesPipelineAppService
{
    public SalesPipelineAppService(IRepository<SalesPipeline, Guid> repository)
        : base(repository)
    {
    }
}