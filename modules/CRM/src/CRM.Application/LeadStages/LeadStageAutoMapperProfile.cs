using AutoMapper;

namespace CRM.LeadStages;

public class LeadStageAutoMapperProfile : Profile
{
    public LeadStageAutoMapperProfile()
    {
        CreateMap<LeadStage, LeadStageDto>();
        CreateMap<CreateUpdateLeadStageDto, LeadStage>();
    }
}