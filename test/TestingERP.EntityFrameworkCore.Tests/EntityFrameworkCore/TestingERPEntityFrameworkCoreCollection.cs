using Xunit;

namespace TestingERP.EntityFrameworkCore;

[CollectionDefinition(TestingERPTestConsts.CollectionDefinitionName)]
public class TestingERPEntityFrameworkCoreCollection : ICollectionFixture<TestingERPEntityFrameworkCoreFixture>
{

}
