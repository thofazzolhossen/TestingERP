using TestingERP.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TestingERP.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(TestingERPEntityFrameworkCoreModule),
    typeof(TestingERPApplicationContractsModule)
)]
public class TestingERPDbMigratorModule : AbpModule
{
}
