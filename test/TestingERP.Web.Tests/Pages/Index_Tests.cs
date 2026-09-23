using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TestingERP.Pages;

[Collection(TestingERPTestConsts.CollectionDefinitionName)]
public class Index_Tests : TestingERPWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
