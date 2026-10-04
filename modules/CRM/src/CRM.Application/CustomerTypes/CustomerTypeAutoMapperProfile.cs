using AutoMapper;

namespace CRM.CustomerTypes;

public class CustomerTypeAutoMapperProfile : Profile
{
    public CustomerTypeAutoMapperProfile()
    {
        CreateMap<CustomerType, CustomerTypeDto>();
        CreateMap<CreateUpdateCustomerTypeDto, CustomerType>();
    }
}