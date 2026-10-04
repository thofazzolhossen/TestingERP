using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using CRM;

namespace CRM.EntityFrameworkCore;

[DependsOn(
    typeof(CRMDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class CRMEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<CRMDbContext>(options =>
        {
            options.AddDefaultRepositories(includeAllEntities: true);
        });
    }
}