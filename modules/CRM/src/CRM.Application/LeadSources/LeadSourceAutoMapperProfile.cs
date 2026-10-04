using AutoMapper;

namespace CRM.LeadSources;

public class LeadSourceAutoMapperProfile : Profile
{
    public LeadSourceAutoMapperProfile()
    {
        CreateMap<LeadSource, LeadSourceDto>();

        CreateMap<CreateUpdateLeadSourceDto, LeadSource>();
    }
}