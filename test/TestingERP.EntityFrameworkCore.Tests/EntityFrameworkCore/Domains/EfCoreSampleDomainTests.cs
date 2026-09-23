using TestingERP.Samples;
using Xunit;

namespace TestingERP.EntityFrameworkCore.Domains;

[Collection(TestingERPTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<TestingERPEntityFrameworkCoreTestModule>
{

}
