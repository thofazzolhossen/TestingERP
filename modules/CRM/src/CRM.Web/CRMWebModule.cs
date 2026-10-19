using CRM.Web.Menus;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Identity.Web;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Volo.Abp.AspNetCore.SignalR;

namespace CRM.Web;

[DependsOn(
    typeof(CRMApplicationModule),
    typeof(AbpIdentityWebModule),
    typeof(AbpAspNetCoreSignalRModule)
)]
public class CRMWebModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IMenuContributor, CRMMenuContributor>();
    }
}