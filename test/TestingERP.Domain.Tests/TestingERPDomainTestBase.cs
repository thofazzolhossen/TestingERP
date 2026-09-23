using Volo.Abp.Modularity;

namespace TestingERP;

/* Inherit from this class for your domain layer tests. */
public abstract class TestingERPDomainTestBase<TStartupModule> : TestingERPTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
