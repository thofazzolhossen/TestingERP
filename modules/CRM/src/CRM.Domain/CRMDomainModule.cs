using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace CRM;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(CRM.Domain.Shared.CRMDomainSharedModule)
)]
public class CRMDomainModule : AbpModule
{
}