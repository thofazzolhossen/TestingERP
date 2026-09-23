using TestingERP.Samples;
using Xunit;

namespace TestingERP.EntityFrameworkCore.Applications;

[Collection(TestingERPTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<TestingERPEntityFrameworkCoreTestModule>
{

}
