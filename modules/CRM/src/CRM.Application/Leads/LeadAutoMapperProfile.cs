using AutoMapper;
using CRM.Leads;

namespace CRM.Leads;

public class LeadAutoMapperProfile : Profile
{
    public LeadAutoMapperProfile()
    {
        // Entity to DTO
        CreateMap<Lead, LeadDto>();

        // DTO to Entity (Fixes the exception in OnPostCreateAsync)
        CreateMap<CreateUpdateLeadDto, Lead>();
    }
}