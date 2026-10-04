using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Identity;

namespace CRM;

[DependsOn(
    typeof(CRM.Application.Contracts.CRMApplicationContractsModule),
    typeof(CRMDomainModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpIdentityDomainModule)
)]
public class CRMApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<CRMApplicationModule>();
        });
    }
}