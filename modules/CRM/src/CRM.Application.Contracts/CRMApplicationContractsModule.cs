using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace CRM.Application.Contracts;

[DependsOn(
    typeof(CRM.Domain.Shared.CRMDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
)]
public class CRMApplicationContractsModule : AbpModule
{
}