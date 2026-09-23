using Volo.Abp.Modularity;

namespace TestingERP;

public abstract class TestingERPApplicationTestBase<TStartupModule> : TestingERPTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
