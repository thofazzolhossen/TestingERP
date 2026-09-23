using Volo.Abp.Modularity;

namespace TestingERP;

[DependsOn(
    typeof(TestingERPDomainModule),
    typeof(TestingERPTestBaseModule)
)]
public class TestingERPDomainTestModule : AbpModule
{

}
