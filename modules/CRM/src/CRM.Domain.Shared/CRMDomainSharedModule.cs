using Volo.Abp.Modularity;
using Volo.Abp.Validation;

namespace CRM.Domain.Shared;

[DependsOn(
    typeof(AbpValidationModule)
)]
public class CRMDomainSharedModule : AbpModule
{
}