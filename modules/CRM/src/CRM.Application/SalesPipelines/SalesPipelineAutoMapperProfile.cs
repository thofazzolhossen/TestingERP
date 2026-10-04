using AutoMapper;

namespace CRM.SalesPipelines;

public class SalesPipelineAutoMapperProfile : Profile
{
    public SalesPipelineAutoMapperProfile()
    {
        CreateMap<SalesPipeline, SalesPipelineDto>();
        CreateMap<CreateUpdateSalesPipelineDto, SalesPipeline>();
    }
}