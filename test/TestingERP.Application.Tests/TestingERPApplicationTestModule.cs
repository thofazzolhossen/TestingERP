using Volo.Abp.Modularity;

namespace TestingERP;

[DependsOn(
    typeof(TestingERPApplicationModule),
    typeof(TestingERPDomainTestModule)
)]
public class TestingERPApplicationTestModule : AbpModule
{

}
