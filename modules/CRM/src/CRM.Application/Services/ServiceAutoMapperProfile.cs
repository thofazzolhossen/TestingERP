using AutoMapper;

namespace CRM.Services;

public class ServiceAutoMapperProfile : Profile
{
    public ServiceAutoMapperProfile()
    {
        CreateMap<Service, ServiceDto>();
        CreateMap<CreateUpdateServiceDto, Service>();
    }
}